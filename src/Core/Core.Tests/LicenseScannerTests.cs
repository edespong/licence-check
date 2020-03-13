using System;
using System.Linq;
using Xunit;

namespace LicenseInspector.Tests {
    public class LicenseScannerTests {
        [Fact]
        public void GetLicenseUrl_GitProtocol_CorrectUrl() {
            TestGetLicenseUrl("git://github.com/zertosh/x-x.git",
                "https://github.com/zertosh/x-x/blob/master/LICENSE");
        }

        [Fact]
        public void GetLicenseUrl_GitPlus_CorrectUrl() {
            TestGetLicenseUrl("git+https://github.com/x/fbjs.git",
                "https://github.com/x/fbjs/blob/master/LICENSE");
        }

        [Fact]
        public void GetLicenseUrl_HttpsProtocol_CorrectUrl() {
            TestGetLicenseUrl("https://github.com/x/promise.git",
                "https://github.com/x/promise/blob/master/LICENSE");
        }

        [Fact]
        public void GetLicenseUrl_WithDot_CorrectUrl() {
            TestGetLicenseUrl("git://github.com/x/spin.js.git",
                "https://github.com/x/spin.js/blob/master/LICENSE");
        }

        private void TestGetLicenseUrl(string url, string expectedResult) {
            var projectUrl = new Uri(url);
            var expected = new Uri(expectedResult);
            var result = LicenseScanner.GetPotentialLicenseUrls(projectUrl).First();
            Assert.Equal(expected, result);
        }
    }
}
