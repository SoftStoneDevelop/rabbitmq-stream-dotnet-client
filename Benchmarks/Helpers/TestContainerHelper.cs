// This source code is dual-licensed under the Apache License, version
// 2.0, and the Mozilla Public License, version 2.0.
// Copyright (c) 2017-2023 Broadcom. All Rights Reserved. The term "Broadcom" refers to Broadcom Inc. and/or its subsidiaries.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DotNet.Testcontainers.Containers;

namespace Benchmarks.Helpers
{
    public static class TestContainerHelper
    {
        public static async ValueTask WaitContainerStateRunningAsync(this DockerContainer dockerContainer, long timeoutMs)
        {
            var start = Stopwatch.GetTimestamp();
            while (true)
            {
                if (dockerContainer.State == TestcontainersStates.Running)
                {
                    break;
                }

                if (Stopwatch.GetElapsedTime(start).TotalMilliseconds >= timeoutMs)
                {
                    throw new Exception("");
                }

                await Task.Delay(100).ConfigureAwait(false);
            }
        }
    }
}
