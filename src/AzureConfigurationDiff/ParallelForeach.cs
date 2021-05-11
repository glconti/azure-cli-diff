using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureConfigurationDiff
{
    public static class ParallelForeach
    {
        /// <summary>
        /// Processes a sequence of async Tasks in parallel.
        /// </summary>
        /// <remarks>Code inspired by this StackOverflow answer https://stackoverflow.com/a/52973907/6709779</remarks>
        public static Task ParallelForEachAsync<T>(
            this IEnumerable<T> source,
            Func<T, Task> body,
            int maxDegreeOfParallelism = -1)
        {
            if (maxDegreeOfParallelism < 1) maxDegreeOfParallelism = Environment.ProcessorCount;

            return Task.WhenAll(
                Partitioner.Create(source)
                    .GetPartitions(maxDegreeOfParallelism)
                    .AsParallel()
                    .Select(async partition =>
                    {
                        using (partition)
                        {
                            while (partition.MoveNext())
                            {
                                await Task.Yield(); // prevents a sync/hot thread hangup
                                await body(partition.Current);
                            }
                        }
                    }));
        }
    }
}