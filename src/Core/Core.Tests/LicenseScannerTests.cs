using System;
using System.Linq;
using Xunit;

namespace LicenseInspector.Core.Tests {
    public class LicenseScannerTests {
        [Fact]
        public void GetLicenseUrl_GitProtocol_CorrectUrl() {
            TestGetLicenseUrl("git://github.com/zertosh/x-x.git",
                "https://raw.githubusercontent.com/zertosh/x-x/master/LICENSE");
        }

        [Fact]
        public void GetLicenseUrl_GitPlus_CorrectUrl() {
            TestGetLicenseUrl("git+https://github.com/x/fbjs.git",
                "https://raw.githubusercontent.com/x/fbjs/master/LICENSE");
        }

        [Fact]
        public void GetLicenseUrl_HttpsProtocol_CorrectUrl() {
            TestGetLicenseUrl("https://github.com/x/promise.git",
                "https://raw.githubusercontent.com/x/promise/master/LICENSE");
        }

        [Fact]
        public void GetLicenseUrl_WithDot_CorrectUrl() {
            TestGetLicenseUrl("git://github.com/x/spin.js.git",
                "https://raw.githubusercontent.com/x/spin.js/master/LICENSE");
        }

        private void TestGetLicenseUrl(string url, string expectedResult) {
            var projectUrl = new Uri(url);
            var expected = new Uri(expectedResult);
            var result = LicenseScanner.GetPotentialLicenseUrls(projectUrl).First();
            Assert.Equal(expected, result);
        }
    }
}
