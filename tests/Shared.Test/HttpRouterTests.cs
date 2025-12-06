using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Shared.Http;
using Xunit;

namespace Shared.Tests.Http
{
    public class HttpRouterTests
    {
        private static List<HttpMiddleware> GetMiddlewares(HttpRouter router)
        {
            var field = typeof(HttpRouter).GetField("middlewares",
                BindingFlags.NonPublic | BindingFlags.Instance);
            return (List<HttpMiddleware>)field!.GetValue(router)!;
        }

        private static List<(string Method, string Path, HttpMiddleware[] Middlewares)> GetRoutes(HttpRouter router)
        {
            var field = typeof(HttpRouter).GetField("routes",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var value = (List<(string, string, HttpMiddleware[])>)field!.GetValue(router)!;
            return value.Select(tuple => (tuple.Item1, tuple.Item2, tuple.Item3)).ToList();
        }

        private static string GetBasePath(HttpRouter router)
        {
            var field = typeof(HttpRouter).GetField("basePath",
                BindingFlags.NonPublic | BindingFlags.Instance);
            return (string)field!.GetValue(router)!;
        }

        [Fact]
        public void Use_AddsMiddlewaresInOrder()
        {
            var router = new HttpRouter();
            HttpMiddleware m1 = (req, res, props, next) => Task.CompletedTask;
            HttpMiddleware m2 = (req, res, props, next) => Task.CompletedTask;

            router.Use(m1, m2);

            var middlewares = GetMiddlewares(router);
            Assert.Equal(2, middlewares.Count);
            Assert.Same(m1, middlewares[0]);
            Assert.Same(m2, middlewares[1]);
        }

        [Fact]
        public void Map_AddsRouteWithUppercaseMethod()
        {
            var router = new HttpRouter();
            HttpMiddleware handler = (req, res, props, next) => Task.CompletedTask;

            router.Map("get", "/test", handler);

            var routes = GetRoutes(router);
            Assert.Single(routes);
            Assert.Equal("GET", routes[0].Method);
            Assert.Equal("/test", routes[0].Path);
            Assert.Single(routes[0].Middlewares);
        }

        [Fact]
        public void ConvenienceMap_Methods_AddRoutes()
        {
            var router = new HttpRouter();
            HttpMiddleware handler = (req, res, props, next) => Task.CompletedTask;

            router.MapGet("/g", handler)
                  .MapPost("/p", handler)
                  .MapPut("/u", handler)
                  .MapDelete("/d", handler);

            var routes = GetRoutes(router);
            Assert.Equal(4, routes.Count);

            Assert.Contains(routes, r => r.Method == "GET" && r.Path == "/g");
            Assert.Contains(routes, r => r.Method == "POST" && r.Path == "/p");
            Assert.Contains(routes, r => r.Method == "PUT" && r.Path == "/u");
            Assert.Contains(routes, r => r.Method == "DELETE" && r.Path == "/d");
        }

        [Fact]
        public void UseRouter_ComposesBasePath()
        {
            var root = new HttpRouter();
            var child = new HttpRouter();

            root.UseRouter("/api", child);

            Assert.Equal(string.Empty, GetBasePath(root));
            Assert.Equal("/api", GetBasePath(child));
        }

        [Theory]
        [InlineData("/users/123", "/users/:id", "id", "123")]
        [InlineData("/api/v1/users/john%20doe", "/api/v1/users/:username", "username", "john doe")]
        public void ParseUrlParams_MatchesParameterizedRoute(
            string urlPath,
            string routePath,
            string paramName,
            string expectedValue)
        {
            NameValueCollection? result = HttpRouter.ParseUrlParams(urlPath, routePath);

            Assert.NotNull(result);
            Assert.Equal(expectedValue, result![paramName]);
        }

        [Theory]
        [InlineData("/users/123", "/users")]
        [InlineData("/users/123/extra", "/users/:id")]
        [InlineData("/users/123", "/orders/:id")]
        public void ParseUrlParams_ReturnsNullOnMismatch(string urlPath, string routePath)
        {
            var result = HttpRouter.ParseUrlParams(urlPath, routePath);

            Assert.Null(result);
        }
    }
}
