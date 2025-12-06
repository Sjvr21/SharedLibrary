using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Shared.Http;
using Xunit;

namespace Shared.Tests.Http
{
    public class JsonUtilsTests
    {
        // Helper to obtain a real HttpListenerContext so we can access Request/Response
        private static HttpListenerContext CreateContext(string path)
        {
            string prefix = "http://localhost:54321/";
            var listener = new HttpListener();
            listener.Prefixes.Clear();
            listener.Prefixes.Add(prefix);
            listener.Start();

            var contextTask = listener.GetContextAsync();

            using (var client = new HttpClient())
            {
                // Fire-and-forget; we only need the server-side context.
                var _ = client.GetAsync(prefix + path);
                var ctx = contextTask.Result;
                listener.Stop();
                return ctx;
            }
        }

        [Fact]
        public void GetPaginatedResponse_BuildsCorrectMetadataAndLinks()
        {
            var ctx = CreateContext("items");
            var req = ctx.Request;
            var res = ctx.Response;

            var values = Enumerable.Range(1, 5).ToList();
            var pagedResult = new PagedResult<int>(totalCount: 23, values: values);
            var props = new Hashtable();

            int page = 2;
            int size = 5;

            var payload = JsonUtils.GetPaginatedResponse(req, res, props, pagedResult, page, size);

            var payloadType = payload.GetType();

            var dataProp = payloadType.GetProperty("data");
            var metaProp = payloadType.GetProperty("meta");
            var linksProp = payloadType.GetProperty("links");

            var data = (IEnumerable<int>)dataProp!.GetValue(payload)!;
            var meta = metaProp!.GetValue(payload)!;
            var links = linksProp!.GetValue(payload)!;

            Assert.Equal(values, data);

            var totalCountProp = meta.GetType().GetProperty("TotalCount")!;
            var pageProp = meta.GetType().GetProperty("page")!;
            var sizeProp = meta.GetType().GetProperty("size")!;
            var totalPagesProp = meta.GetType().GetProperty("totalPages")!;

            Assert.Equal(23, (int)totalCountProp.GetValue(meta)!);
            Assert.Equal(page, (int)pageProp.GetValue(meta)!);
            Assert.Equal(size, (int)sizeProp.GetValue(meta)!);
            Assert.Equal(5, (int)totalPagesProp.GetValue(meta)!); // ceil(23/5) = 5

            string baseUrl = $"{req.Url!.Scheme}://{req.Url!.Authority}{req.Url!.AbsolutePath}";

            var selfProp = links.GetType().GetProperty("self")!;
            var firstProp = links.GetType().GetProperty("first")!;
            var prevProp = links.GetType().GetProperty("prev")!;
            var nextProp = links.GetType().GetProperty("next")!;
            var lastProp = links.GetType().GetProperty("last")!;

            Assert.Equal($"{baseUrl}?page=2&size=5", (string)selfProp.GetValue(links)!);
            Assert.Equal($"{baseUrl}?page=1&size=5", (string)firstProp.GetValue(links)!);
            Assert.Equal($"{baseUrl}?page=1&size=5", (string)prevProp.GetValue(links)!);
            Assert.Equal($"{baseUrl}?page=3&size=5", (string)nextProp.GetValue(links)!);
            Assert.Equal($"{baseUrl}?page=5&size=5", (string)lastProp.GetValue(links)!);
        }
    }
}
