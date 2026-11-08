// This source code is dual-licensed under the Apache License, version
// 2.0, and the Mozilla Public License, version 2.0.
// Copyright (c) 2017-2023 Broadcom. All Rights Reserved. The term "Broadcom" refers to Broadcom Inc. and/or its subsidiaries.

using BenchmarkDotNet.Running;
using RabbitMQ.Stream.Client.Benchmarks.Cases;

namespace RabbitMQ.Stream.Client.Benchmarks
{
    internal class Program
    {
        static void Main()
        {
            BenchmarkRunner.Run<MetricsAffectedCase>();
        }
    }
}
