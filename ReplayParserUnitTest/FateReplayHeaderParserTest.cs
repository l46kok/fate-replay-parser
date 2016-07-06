using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ReplayParser.Data;
using ReplayParser.Parser;
using ReplayParser.Samples;

namespace ReplayParserUnitTest
{
    [TestFixture]
    public class FateReplayHeaderParserTest
    {
        private FateReplayHeaderParser parser;
        private byte[] sampleBytes;
        [SetUp]
        public void Init()
        {
            parser = new FateReplayHeaderParser();
            sampleBytes = File.ReadAllBytes(@"Samples\Sample1.w3g");
        }

        [Test]
        public void SampleTest()
        {
            int currIndex = 0;
            ReplayHeader replayHeader = parser.ParseReplayHeader(sampleBytes, out currIndex);
            Assert.Greater(replayHeader.CompressedFileSize,0);
        }
    }
}
