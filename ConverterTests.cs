using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Virinco.WATS.Interface;
using System.IO;

namespace XMLConverter
{
    [TestClass]
    public class ConverterTests : TDM
    {
        // disable this test if you don't want to run it, or if you don't have a WATS server to connect to
        [Ignore]
        [TestMethod]
        public void SetupClient()
        {
            SetupAPI(null, "", "Test", true);
            RegisterClient("https://trimble.wats.com", "username", "password");
            InitializeAPI(true);
        }

        [TestMethod]
        public void TestXMLConverter()
        {
            InitializeAPI(true);
            var arguments = new Dictionary<string, string> { { "sequenceFile", "sequence.seq" } };
            var converter = new XMLConverter(arguments);
            using (var file = new FileStream(@"Examples\XMLReport.xml", FileMode.Open))
            {
                var report = converter.ImportReport(this, file);
                Assert.IsNotNull(report);
                var uut = report as UUTReport;
                Assert.IsNotNull(uut);
                Assert.AreEqual("12345", uut.SerialNumber);
                Assert.AreEqual("Part1", uut.PartNumber);
            }            
        }
    }
}
