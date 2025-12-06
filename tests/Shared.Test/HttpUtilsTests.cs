using System.Collections.Specialized;
using Shared.Http;
using Xunit;

namespace Shared.Tests.Http
{
    public class HttpUtilsTests
    {
        [Fact]
        public void ParseUrl_SplitsIntoExpectedComponents()
        {
            string url = "https://john:abc123@site.com:8080/api/v1/users/3?q=0&active=true#bio";

            NameValueCollection parts = HttpUtils.ParseUrl(url);

            Assert.Equal("https", parts["scheme"]);
            Assert.Equal("john:abc123@site.com:8080", parts["auth"]);
            Assert.Equal("john", parts["user"]);
            Assert.Equal("abc123", parts["pass"]);
            Assert.Equal("site.com", parts["host"]);
            Assert.Equal("8080", parts["port"]);
            Assert.Equal("/api/v1/users/3", parts["path"]);
            Assert.Equal("q=0&active=true", parts["query"]);
            Assert.Equal("bio", parts["fragment"]);
        }

        [Fact]
        public void ParseQueryString_HandlesLeadingQuestionMarkAndDuplicates()
        {
            string query = "?key1=value1&key2=value2&key1=value3";

            var nvc = HttpUtils.ParseQueryString(query);

            Assert.Equal("value1,value3", nvc["key1"]);
            Assert.Equal("value2", nvc["key2"]);
        }

        [Fact]
        public void ParseFormData_ParsesEncodedPairs()
        {
            string text = "name=Sergio+Velez&city=San%20Juan";

            var nvc = HttpUtils.ParseFormData(text);

            Assert.Equal("Sergio Velez", nvc["name"]);
            Assert.Equal("San Juan", nvc["city"]);
        }

        [Theory]
        [InlineData(" { \"a\": 1 } ", "application/json")]
        [InlineData("<html><body></body></html>", "text/html")]
        [InlineData("<root><a /></root>", "application/xml")]
        [InlineData("plain text", "text/plain")]
        public void DetectContentType_InfersFromPayload(string content, string expectedPrefix)
        {
            string detected = HttpUtils.DetectContentType(content);

            Assert.StartsWith(expectedPrefix, detected);
        }
    }
}
