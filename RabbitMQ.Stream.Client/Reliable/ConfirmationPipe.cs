﻿// This source code is dual-licensed under the Apache License, version
// 2.0, and the Mozilla Public License, version 2.0.
// Copyright (c) 2017-2023 Broadcom. All Rights Reserved. The term "Broadcom" refers to Broadcom Inc. and/or its subsidiaries.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Timers;
using Timer = System.Timers.Timer;

namespace RabbitMQ.Stream.Client.Reliable;

/// <summary>
/// ConfirmationStatus can be:
/// </summary>
public enum ConfirmationStatus : ushort
{
    WaitForConfirmation = 0,

    /// <summary>
    /// Message was confirmed to be received and stored by server.
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// Client gave up on waiting for this publishing id.
    /// </summary>
    ClientTimeoutError = 2,

    /// <summary>
    /// Stream is not available anymore (it was deleted).
    /// </summary>
    StreamNotAvailable = 6,
    InternalError = 15,

    /// <summary>
    /// Signals either bad credentials, or insufficient permissions.
    /// </summary>
    AccessRefused = 16,
    PreconditionFailed = 17,
    PublisherDoesNotExist = 18,
    UndefinedError = 200,
}

/// <summary>
/// MessagesConfirmation is a wrapper around the message/s
/// This class is returned to the user to understand
/// the message status. 
/// </summary>
public class MessagesConfirmation
{
    public ulong PublishingId { get; internal set; }
    public List<Message> Messages { get; internal init; }
    public DateTime InsertDateTime { get; init; }
    public ConfirmationStatus Status { get; internal set; }

    public string Stream { get; internal set; }
}

/// <summary>
/// ConfirmationPipe maintains the status for the sent and received messages.
/// TPL Action block sends the confirmation to the user in async way
/// So the send/1 is not blocking.
/// </summary>
public class ConfirmationPipe
{
    private ActionBlock<(ConfirmationStatus, ulong, string)> _waitForConfirmationActionBlock;
    private readonly ConcurrentDictionary<ulong, MessagesConfirmation> _waitForConfirmation = new();
    private readonly Timer _invalidateTimer = new();
    private Func<MessagesConfirmation, Task> ConfirmHandler { get; }
    private readonly TimeSpan _messageTimeout;
    private readonly int _maxInFlightMessages;

    internal ConfirmationPipe(Func<MessagesConfirmation, Task> confirmHandler,
        TimeSpan messageTimeout, int maxInFlightMessages)
    {
        ConfirmHandler = confirmHandler;
        _messageTimeout = messageTimeout;
        _maxInFlightMessages = maxInFlightMessages;
    }

    internal void Start()
    {
        _waitForConfirmationActionBlock = new ActionBlock<(ConfirmationStatus, ulong, string)>(
            request =>
            {
                var (confirmationStatus, publishingId, stream) = request;

                _waitForConfirmation.TryRemove(publishingId, out var message);
                if (message == null)
                {
                    return;
                }

                message.Status = confirmationStatus;
                message.Stream = stream;
                ConfirmHandler?.Invoke(message);
            }, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 1,
                // We set the BoundedCapacity to the double of the maxInFlightMessages
                // because we want a cache of messages to speedup performance.
                BoundedCapacity = _maxInFlightMessages * 2
            });

        _invalidateTimer.Elapsed += OnTimedEvent;
        _invalidateTimer.Interval = _messageTimeout.TotalMilliseconds;
        _invalidateTimer.Enabled = true;
    }

    internal void Stop()
    {
        FlushPendingMessages();
        _invalidateTimer.Enabled = false;
        _waitForConfirmationActionBlock.Complete();
    }

    private async void OnTimedEvent(object sender, ElapsedEventArgs e)
    {
        var timedOutMessages = _waitForConfirmation.Where(pair =>
            (DateTime.Now - pair.Value.InsertDateTime).TotalSeconds > _messageTimeout.TotalSeconds);

        foreach (var pair in timedOutMessages)
        {
            await RemoveUnConfirmedMessage(ConfirmationStatus.ClientTimeoutError, pair.Value.PublishingId, pair.Value.Stream)
                .ConfigureAwait(false);
        }
    }

    private async void FlushPendingMessages()
    {
        foreach (var pair in _waitForConfirmation)
        {
            await RemoveUnConfirmedMessage(ConfirmationStatus.ClientTimeoutError, pair.Value.PublishingId, null)
                .ConfigureAwait(false);
        }
    }

    internal void AddUnConfirmedMessage(ulong publishingId, Message message)
    {
        AddUnConfirmedMessage(publishingId, new List<Message> { message });
    }

    internal void AddUnConfirmedMessage(ulong publishingId, List<Message> messages)
    {
        var messagesConfirmation = new MessagesConfirmation
        {
            // We need to copy the messages because the user can reuse the same message or deleted them.
            Messages = new List<Message>(messages),
            PublishingId = publishingId,
            InsertDateTime = DateTime.Now
        };

        if (!_waitForConfirmation.TryAdd(publishingId, messagesConfirmation))
        {
            foreach (var message in messages)
            {
                message.Dispose();
            }
        }
    }

    internal async Task RemoveUnConfirmedMessage(ConfirmationStatus confirmationStatus, ulong publishingId,
        string stream)
    {
        if (!await _waitForConfirmationActionBlock.SendAsync((confirmationStatus, publishingId, stream))
                .ConfigureAwait(false))
        {
            await _waitForConfirmationActionBlock.Completion.ConfigureAwait(false);
        }
    }
}
