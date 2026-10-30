// This source code is dual-licensed under the Apache License, version
// 2.0, and the Mozilla Public License, version 2.0.
// Copyright (c) 2017-2023 Broadcom. All Rights Reserved. The term "Broadcom" refers to Broadcom Inc. and/or its subsidiaries.

using System.Threading.Tasks;
using Benchmarks.Helpers;
using Testcontainers.RabbitMq;

namespace Benchmarks
{
    public abstract class RabbitMQStreamBenchmark
    {
        protected RabbitMqContainer _container;

        protected async Task OneTimeSetUp()
        {
            _container = new RabbitMqBuilder("rabbitmq:3.12.14-management-alpine")
                .WithUsername("guest")
                .WithPassword("guest")
                .WithEnvironment("RABBITMQ_SERVER_ADDITIONAL_PLUGINS", "rabbitmq_stream")
                .WithCommand("sh", "-c", "rabbitmq-plugins enable rabbitmq_stream && rabbitmq-server")
                .WithPortBinding(5552, true)
                .WithAutoRemove(true)
                .Build();

            await _container.StartAsync().ConfigureAwait(false);
            await _container.WaitContainerStateRunningAsync(10_000).ConfigureAwait(false);
            await _container.WaitResponseAsync(10_000).ConfigureAwait(false);
        }

        protected async Task OneTimeTearDown()
        {
            if (_container != null)
            {
                try
                {
                    await _container.DisposeAsync().ConfigureAwait(false);
                }
                catch
                {
                    // ignore
                }
            }
        }
    }
}
