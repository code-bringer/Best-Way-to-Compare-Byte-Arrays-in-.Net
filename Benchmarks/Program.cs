using System;
using BenchmarkDotNet;

namespace Benchmarks
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            int sizeOfComparingArrays = 1024 * 1024;
            if (args != null && args.Length > 0)
            {
                Int32.TryParse(args[0], out sizeOfComparingArrays);
            }

            BenchmarkSettings.Instance.DefaultWarmUpIterationCount = 3;
            BenchmarkSettings.Instance.DefaultResultIterationCount = 10;
            BenchmarkSettings.Instance.DetailedMode = false;

            var competition = new ByteArrayComparsionBenchmarkCompetition(sizeOfComparingArrays);
            competition.Run();
        }
    }
}
