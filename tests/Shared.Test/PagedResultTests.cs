using System.Collections.Generic;
using Shared.Http;
using Xunit;

namespace Shared.Tests.Http
{
    public class PagedResultTests
    {
        [Fact]
        public void Constructor_SetsProperties()
        {
            var values = new List<int> { 1, 2, 3 };

            var result = new PagedResult<int>(10, values);

            Assert.Equal(10, result.TotalCount);
            Assert.Same(values, result.Values);
        }
    }
}
