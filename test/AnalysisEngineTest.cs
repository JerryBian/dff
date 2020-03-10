using System.Diagnostics;
using System.Threading.Tasks;
using DuplicateFileFinder.Core;
using Xunit;
using Xunit.Abstractions;

namespace DuplicateFileFinder.Test
{
    public class AnalysisEngineTest
    {
        private readonly ITestOutputHelper _output;

        public AnalysisEngineTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(8)]
        [InlineData(32)]
        [InlineData(64)]
        [InlineData(128)]
        [InlineData(256)]
        [InlineData(512)]
        [InlineData(1024)]
        public async Task TestSameFileComparePerformance(int maxBytesScan)
        {
            var file1 = "./Asset/Autoruns.zip";
            var file2 = "./Asset/Autoruns2.zip";

            const int iterations = 100;
            var engine = new AnalysisEngine(null, null) {MaxBytesScan = maxBytesScan};
            var total = 0L;
            for (var i = 0; i < iterations; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                var result = await engine.AreFilesEqualAsync(file1, file2);
                stopwatch.Stop();

                total += stopwatch.ElapsedMilliseconds;
                Assert.True(result);
            }

            _output.WriteLine($"MaxBytesScan={maxBytesScan}: in average it took {total / iterations}ms.");
        }

        [Theory]
        [InlineData(8)]
        [InlineData(32)]
        [InlineData(64)]
        [InlineData(128)]
        [InlineData(256)]
        [InlineData(512)]
        [InlineData(1024)]
        public async Task TestDiffFileComparePerformance(int maxBytesScan)
        {
            var file1 = "./Asset/Autoruns.zip";
            var file2 = "./Asset/VMMap.zip";

            const int iterations = 100;
            var engine = new AnalysisEngine(null, null) {MaxBytesScan = maxBytesScan};
            var total = 0L;
            for (var i = 0; i < iterations; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                var result = await engine.AreFilesEqualAsync(file1, file2);
                stopwatch.Stop();

                total += stopwatch.ElapsedMilliseconds;
                Assert.False(result);
            }

            _output.WriteLine($"MaxBytesScan={maxBytesScan}: in average it took {total / iterations}ms.");
        }
    }
}