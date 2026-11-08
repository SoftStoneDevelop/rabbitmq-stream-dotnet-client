// This source code is dual-licensed under the Apache License, version
// 2.0, and the Mozilla Public License, version 2.0.
// Copyright (c) 2017-2023 Broadcom. All Rights Reserved. The term "Broadcom" refers to Broadcom Inc. and/or its subsidiaries.

using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using RabbitMQ.Stream.Client.Reliable;

namespace RabbitMQ.Stream.Client.Benchmarks.Cases
{
    [MemoryDiagnoser]
    [ThreadingDiagnoser]
    [SimpleJob(RuntimeMoniker.Net80)]
    public class MetricsAffectedCase : BenchmarkBase
    {
        [Params(10, 100, 1_000, 10_000, 100_000)]
        public int MessagesCount;

        private Producer _producerWithMetrics;
        private Producer _producerWithoutMetrics;
        private static readonly byte[] s_messageData = new byte[10_000];

        [GlobalSetup]
        public async Task GlobalSetup()
        {
            await OneTimeSetUp().ConfigureAwait(false);

            var configCollectMetrics = new StreamSystemConfig()
            {
                Endpoints = new List<EndPoint> { new IPEndPoint(IPAddress.Loopback, _container.GetMappedPublicPort(5552)) },
                CollectMetrics = true
            };
            var system = await StreamSystem.Create(configCollectMetrics).ConfigureAwait(false);
            const string StreamName = "array-pool-benchmark-stream";
            await system.CreateStream(new StreamSpec(StreamName)).ConfigureAwait(false);

            var configNotCollectMetrics = new StreamSystemConfig()
            {
                Endpoints = new List<EndPoint> { new IPEndPoint(IPAddress.Loopback, _container.GetMappedPublicPort(5552)) },
                CollectMetrics = false
            };
            var system2 = await StreamSystem.Create(configNotCollectMetrics).ConfigureAwait(false);

            var configWithMetrics = new ProducerConfig(system2, StreamName);
            _producerWithMetrics = await Producer.Create(configWithMetrics).ConfigureAwait(false);

            var configWithoutMetrics = new ProducerConfig(system2, StreamName);
            _producerWithoutMetrics = await Producer.Create(configWithoutMetrics).ConfigureAwait(false);
        }

        [GlobalCleanup]
        public async Task GlobalCleanup()
        {
            await OneTimeTearDown().ConfigureAwait(false);
        }

        [Benchmark(Baseline = true, Description = "CollectMetrics = True")]
        public async Task CollectMetricsTrue()
        {
            for (var i = 0; i < MessagesCount; i++)
            {
                await _producerWithMetrics.Send(new Message(s_messageData)).ConfigureAwait(false);
            }
        }

        [Benchmark(Baseline = false, Description = "CollectMetrics = False")]
        public async Task CollectMetricsFalse()
        {
            for (var i = 0; i < MessagesCount; i++)
            {
                await _producerWithoutMetrics.Send(new Message(s_messageData)).ConfigureAwait(false);
            }
        }
    }
}
