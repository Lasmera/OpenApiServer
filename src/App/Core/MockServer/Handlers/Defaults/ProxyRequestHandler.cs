using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

using OpenApiServer.Core.MockServer.Context.Types;
using OpenApiServer.Core.MockServer.Exceptions;
using OpenApiServer.Utils;

using HttpMethod = System.Net.Http.HttpMethod;

namespace OpenApiServer.Core.MockServer.Handlers.Defaults
{
    [RequestHandler("proxy")]
    public class ProxyRequestHandler : IRequestHandler
    {
        public class Options
        {
            public string Host { get; set; }
        }

        private static readonly Guid Id = Guid.NewGuid();
        private string ProxyInstanceId { get; }

        private const string ForwarderFromHeader = "X-Forwarded-From";
        private const string ForwaredHostHeader = "X-Forwarded-Host";

        private IHttpClientFactory ClientFactory { get; }
        private Options Config { get; }

        public ProxyRequestHandler(IHttpClientFactory clientFactory, IConfiguration config)
        {
            ClientFactory = clientFactory;
            ProxyInstanceId = Id.ToString();
            
            var options = new Options();
            config.Bind(options);
            Config = options;
        }

        public Task<ResponseContext> HandleAsync(RequestContext context)
        {
            var host = GetHostFromHeader(context.Request.Headers) ??
                       Config.Host ??
                       GetHostFromOperation(context.Spec);
            if (host == null)
            {
                throw new MockServerConfigurationException("Unable to find host to proxy the request.");
            }

            var hasHeader = context.Request.Headers.TryGetValue(ForwarderFromHeader, out var mockServerHeader);
            if (hasHeader)
            {
                if (mockServerHeader == ProxyInstanceId)
                {
                    throw new MockServerConfigurationException(
                            "This MockServer instance is configured to proxy requests to itself, " +
                            "which will result in an infinite loop. Update the host via config or " +
                            "'X-Forwarded-Host' header");
                }
            }

            return Proxy(context, host);
        }

        private async Task<ResponseContext> Proxy(RequestContext ctx, string host)
        {
            var client = ClientFactory.CreateClient();
            var request = CreateRequest(ctx, host);

            var response = await client.SendAsync(request).ConfigureAwait(false);

            return await CreateResponseAsync(response).ConfigureAwait(false);
        }

        private HttpRequestMessage CreateRequest(RequestContext ctx, string host)
        {
            var targetRequest = new HttpRequestMessage
                                {
                                        RequestUri = new Uri($"{host}{ctx.Request.PathAndQuery}"),
                                        Method = new HttpMethod(ctx.Request.Method.ToString().ToUpperInvariant()),
                                        Content = new StringContent(ctx.Request.Body.ToString(),
                                                                    Encoding.UTF8,
                                                                    ctx.Request.ContentType)
                                };

            foreach (var (k, v) in ctx.Request.Headers)
            {
                // Skip Content-Length to avoid mismatch with actual content-length
                // which may change when request is recreated.
                if (k == "Content-Length") continue;

                targetRequest.Content.Headers.TryAddWithoutValidation(k, v.ToArray());
            }

            targetRequest.Content.Headers.Add(ForwarderFromHeader, ProxyInstanceId);

            return targetRequest;
        }

        private static Task<ResponseContext> CreateResponseAsync(HttpResponseMessage sourceResponse)
        {
            return sourceResponse.Content.ReadAsStringAsync().ContinueWith(CreateContext);

            ResponseContext CreateContext(Task<string> body) =>
                    new ResponseContext
                    {
                            ContentType = sourceResponse.Content.Headers.ContentType?.ToString(),
                            StatusCode = sourceResponse.StatusCode,
                            Body = body.Result,
                            Headers = GetHeaders()
                    };

            IDictionary<string, StringValues> GetHeaders()
            {
                // Setting {Transer-Encoding = chunked} on response results in invalid response.
                var items = sourceResponse.Headers
                                          .Where(x => x.Key != "Transfer-Encoding")
                                          .Concat(sourceResponse.Content.Headers)
                                          .ToDictionary(x => x.Key, x => new StringValues(x.Value.ToArray()));
                return new Dictionary<string, StringValues>(items);
            }
        }

        private static string GetHostFromHeader(IHeaderDictionary headers)
        {
            var hasProxyHeader = headers.TryGetValue(ForwaredHostHeader, out var header);
            if (hasProxyHeader)
            {
                return UrlHelper.GetHost(header.ToString());
            }
            else
            {
                return null;
            }
        }

        private static string GetHostFromOperation(RequestContextSpec spec)
        {
            var specUrl = spec.Servers.FirstOrDefault();
            return specUrl == null ? null : UrlHelper.GetHost(specUrl);
        }
    }
}