using System;
using System.Net;
using Shared.Http;
using Xunit;

namespace Shared.Tests.Http
{
    public class ResultTests
    {
        [Fact]
        public void SuccessConstructor_SetsPayloadAndStatusCode()
        {
            var payload = "ok";

            var result = new Result<string>(payload, (int)HttpStatusCode.Created);

            Assert.False(result.IsError);
            Assert.Null(result.Error);
            Assert.Equal(payload, result.Payload);
            Assert.Equal((int)HttpStatusCode.Created, result.StatusCode);
        }

        [Fact]
        public void ErrorConstructor_SetsErrorAndStatusCode()
        {
            var ex = new InvalidOperationException("boom");

            var result = new Result<string>(ex, (int)HttpStatusCode.BadRequest);

            Assert.True(result.IsError);
            Assert.Same(ex, result.Error);
            Assert.Null(result.Payload);
            Assert.Equal((int)HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public void Constructors_UseDefaultStatusCodes()
        {
            var ex = new Exception("error");
            var errorResult = new Result<string>(ex);
            var okResult = new Result<string>("ok");

            Assert.Equal((int)HttpStatusCode.InternalServerError, errorResult.StatusCode);
            Assert.Equal((int)HttpStatusCode.OK, okResult.StatusCode);
        }
    }
}
