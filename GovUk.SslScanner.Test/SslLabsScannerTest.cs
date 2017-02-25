using System.Collections.Generic;
using GovUk.SslScanner.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GovUk.SslScanner.Test
{
    [TestClass]
    public class SslLabsScannerTest
    {
        [TestMethod]
        public void ExmoornationalparkTest()
        {
            var scanner = new SslLabsScanner(new HashSet<string>
            {
                "www.exmoor-nationalpark.gov.uk"
            });

            var results = scanner.Run();

            Assert.IsNotNull(results);

            var govDomain = results[0];

            Assert.IsNotNull(govDomain);
            Assert.IsNotNull(govDomain.grade);

            Assert.AreNotEqual(Grade.Aplus, govDomain.grade);
        }
    }
}