using Xunit;

namespace LicenseInspector.JavaScript.Tests
{
    public class NpmPackageSerializationTests
    {
        [Fact]
        public void StringLicense_CorrectResult()
        {
            const string expected = "TestLicense";
            string json = $"{{ \"license\": \"{expected}\" }}";
            var npmPackage = NpmPackage.Deserialize(json);
            Assert.Equal(expected, npmPackage.License);
        }

        [Fact]
        public void TypeLicense_CorrectResult()
        {
            const string expected = "TestLicense";
            string json = $"{{ \"name\": \"test-name\", \"version\": \"1.0.0\", \"license\": {{ \"type\": \"{expected}\", \"url\": \"https://test\" }} }}";
            var npmPackage = NpmPackage.Deserialize(json);
            Assert.Equal(expected, npmPackage.License);
        }

        [Fact]
        public void TypeLicense_InvalidRepositoryFormat_CorrectResult()
        {
            const string expected = "TestLicense";
            const string json = "{\"name\":\"pinkie\",\"version\":\"2.0.0\",\"description\":\"Itty\",\"license\":\"TestLicense\",\"repository\":\"x/y\",\"author\":{\"name\":\"My Name\",\"email\":\"xxx@gmail.com\",\"url\":\"github.com/xx\"},\"engines\":{\"node\":\">=0.10.0\"},\"scripts\":{\"test\":\"xo && nyc mocha\",\"coverage\":\"nyc report --reporter=text-lcov | coveralls\"},\"files\":[\"index.js\"],\"keywords\":[\"promise\",\"promises\",\"es2015\",\"es6\"],\"devDependencies\":{\"coveralls\":\"^2.11.4\",\"mocha\":\"*\",\"nyc\":\"^3.2.2\",\"promises-aplus-tests\":\"*\",\"xo\":\"^0.10.1\"},\"_id\":\"pinkie@2.0.0\",\"_npmVersion\":\"0.0.0-fake\",\"_nodeVersion\":\"0.0.0-fake\",\"_shasum\":\"11737918d16ab5859a90a5a031b6f7e0d6f245cc\",\"_npmUser\":{\"name\":\"npm\",\"email\":\"support@npmjs.com\"},\"_from\":\".\",\"dist\":{\"shasum\":\"11737918d16ab5859a90a5a031b6f7e0d6f245cc\",\"tarball\":\"https://registry.npmjs.org/xx\"},\"maintainers\":[{\"name\":\"bb\",\"email\":\"mm@gmail.com\"}],\"directories\":{}}"; ;
            var npmPackage = NpmPackage.Deserialize(json);
            Assert.Equal(expected, npmPackage.License);
        }
    }
}