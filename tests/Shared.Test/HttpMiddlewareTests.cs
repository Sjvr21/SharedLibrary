using System;
using System.Collections;
using System.Net;
using System.Threading.Tasks;
using Shared.Http;
using Xunit;

namespace Shared.Test.Http
{
    public class HttpMiddlewareTests
    {
        [Fact]
        public async Task Middleware_InvokesNextDelegate()
        {
            bool nextCalled = false;
            HttpMiddleware middleware = async (req, res, props, next) =>
            {
                props["value"] = 1;
                await next();
            };

            Func<Task> next = () =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            await middleware(null!, null!, new Hashtable(), next);

            Assert.True(nextCalled);
        }

        [Fact]
        public async Task Middleware_Pipeline_ExecutesInOrder()
        {
            var props = new Hashtable();

            HttpMiddleware first = async (req, res, p, next) =>
            {
                p["order"] = "first";
                await next();
            };

            HttpMiddleware second = async (req, res, p, next) =>
            {
                p["order"] = p["order"] + ",second";
                await next();
            };

            Func<Task> pipeline = () =>
                first(null!, null!, props, () =>
                    second(null!, null!, props, () => Task.CompletedTask));

            await pipeline();

            Assert.Equal("first,second", props["order"]);
        }

        [Fact]
        public async Task Middleware_CanShortCircuitPipeline()
        {
            var props = new Hashtable { ["count"] = 0 };

            // NOTE: no 'async' here, so no CS1998 warning
            HttpMiddleware first = (req, res, p, next) =>
            {
                // Safely read and increment count without unboxing-null warning
                var current = p["count"] as int? ?? 0;
                p["count"] = current + 1;

                // Short-circuit: do not call next()
                return Task.CompletedTask;
            };

            HttpMiddleware second = async (req, res, p, next) =>
            {
                var current = p["count"] as int? ?? 0;
                p["count"] = current + 1;
                await next();
            };

            Func<Task> pipeline = () =>
                first(null!, null!, props, () =>
                    second(null!, null!, props, () => Task.CompletedTask));

            await pipeline();

            Assert.Equal(1, props["count"]);
        }
    }
}
