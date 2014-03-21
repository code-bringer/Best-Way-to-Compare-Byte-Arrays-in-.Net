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

            BenchmarkSettings.Instance.DefaultWarmUpIterationCount = 10;
            BenchmarkSettings.Instance.DefaultResultIterationCount = 100;
            BenchmarkSettings.Instance.DetailedMode = true;

            var competition = new ByteArrayComparsionBenchmarkCompetition(sizeOfComparingArrays);
            competition.Run();
        }
    }
}
