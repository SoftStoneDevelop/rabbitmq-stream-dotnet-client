// This source code is dual-licensed under the Apache License, version
// 2.0, and the Mozilla Public License, version 2.0.
// Copyright (c) 2017-2023 Broadcom. All Rights Reserved. The term "Broadcom" refers to Broadcom Inc. and/or its subsidiaries.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;

namespace Benchmarks.Helpers
{
    public static class RabbitMqContainerHelper
    {
        public static async ValueTask WaitResponseAsync(this RabbitMqContainer container, long timeoutMs)
        {
            var start = Stopwatch.GetTimestamp();
            while (true)
            {
                if (Stopwatch.GetElapsedTime(start).TotalMilliseconds >= timeoutMs)
                {
                    break;
                }

                var connectionString = container.GetConnectionString();
                try
                {
                    var factory = new ConnectionFactory
                    {
                        Uri = new Uri(connectionString)
                    };

                    using (var conn = factory.CreateConnection())
                    {
                        if (conn.IsOpen)
                        {
                            break;
                        }
                    }
                }
                catch
                {
                    // ignore
                }

                await Task.Delay(100).ConfigureAwait(false);
            }
        }
    }
}
