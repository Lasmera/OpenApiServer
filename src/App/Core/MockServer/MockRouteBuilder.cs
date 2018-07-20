using System;
using System.Collections.Generic;
using System.Linq;

using ITExpert.OpenApi.Server.Core.MockServer.Validation;
using ITExpert.OpenApi.Utils;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

namespace ITExpert.OpenApi.Server.Core.MockServer
{
    public class MockRouteBuilder
    {
        private IEnumerable<OpenApiDocument> Specs { get; }
        private RouteBuilder RouteBuilder { get; }

        private string HostUrl { get; }
        private Func<OpenApiDocument, string> GetRoutePrefix { get; }

        public MockRouteBuilder(IApplicationBuilder app, IEnumerable<OpenApiDocument> specs)
        {
            var options = app.ApplicationServices.GetService<IOptions<MockServerOptions>>();
            GetRoutePrefix = options.Value.GetRoutePrefix ?? GetDefaultRoutePrefix;
            HostUrl = options.Value.Host;

            RouteBuilder = new RouteBuilder(app);
            Specs = specs;
        }

        public IRouter Build()
        {
            foreach (var spec in Specs)
            {
                AddMockServer(spec);
                MapSpec(spec);
            }

            return RouteBuilder.Build();
        }

        private void AddMockServer(OpenApiDocument spec)
        {
            if (string.IsNullOrEmpty(HostUrl))
            {
                return;
            }

            var mockServerUrl = $"{HostUrl}{GetRoutePrefix(spec)}";
            var mockServer = new OpenApiServer
                             {
                                     Url = mockServerUrl,
                                     Description = "Mock server"
                             };
            spec.Servers.Add(mockServer);
        }

        private void MapSpec(OpenApiDocument spec)
        {
            foreach (var route in GetRoutes(spec))
            {
                var template = GetRouteTemplate(route.Path, spec);
                var handler = new MockRouteHandler(route.Operation, route.Validator, route.Generator);
                RouteBuilder.MapVerb(route.OperationType.ToString(), template, handler.InvokeAsync);
            }
        }

        private static string GetDefaultRoutePrefix(OpenApiDocument doc)
        {
            var title = doc.Info.Title.Replace(" ", "");
            var version = doc.Info.GetMajorVersion();
            return $"/api/{title}/v{version}";
        }

        private string GetRouteTemplate(string openApiRoute, OpenApiDocument spec)
        {
            var prefix = GetRoutePrefix(spec);
            if (prefix.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                prefix = prefix.Substring(1);
            }

            return $"{prefix}{openApiRoute}";
        }

        private static IEnumerable<MockServerRouteContext> GetRoutes(OpenApiDocument doc)
        {
            var validator = new NullRequestValidator();
            var generator = new MockResponseGenerator();

            return doc.Paths.SelectMany(
                    path => path.Value.Operations.Select(
                            verb => new MockServerRouteContext
                                    {
                                            Path = path.Key,
                                            Operation = verb.Value,
                                            OperationType = verb.Key,
                                            Validator = validator,
                                            Generator = generator
                                    }));
        }

        private class MockServerRouteContext
        {
            public string Path { get; set; }
            public OperationType OperationType { get; set; }
            public OpenApiOperation Operation { get; set; }
            public IMockServerRequestValidator Validator { get; set; }
            public MockResponseGenerator Generator { get; set; }
        }
    }
}