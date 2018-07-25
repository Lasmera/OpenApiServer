using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Extensions.Primitives;

namespace ITExpert.OpenApi.Server.Core.MockServer
{
    public class ProxyRequestHandler : IMockServerRequestHandler
    {
        private const string ProxyHeaderName = "X-Forwarded-Host";

        private IHttpClientFactory ClientFactory { get; }

        public ProxyRequestHandler(IHttpClientFactory clientFactory)
        {
            ClientFactory = clientFactory;
        }

        public Task<IMockServerResponseContext> HandleAsync(IMockServerRequestContext context)
        {
            var foundHost = TryGetProxyHeader(context, out var host);
            if (!foundHost)
            {
                throw new Exception("Unable to find host to proxy the request.");
            }

            return Proxy(context, host);
        }

        private Task<IMockServerResponseContext> Proxy(IMockServerRequestContext ctx, string host)
        {
            var client = ClientFactory.CreateClient();
            var request = CreateRequest(ctx, host);
            var response = client.SendAsync(request);

            return response.ContinueWith(x => CreateResponse(x.Result));
        }

        private static HttpRequestMessage CreateRequest(IMockServerRequestContext ctx, string forwardHost)
        {
            var targetRequest = new HttpRequestMessage
                                {
                                        RequestUri = GetUri(),
                                        Method = new HttpMethod(ctx.Method.ToString()),
                                        Content = GetContent()
                                };
            CopyHeaders(targetRequest);

            return targetRequest;

            Uri GetUri() => new Uri($"{forwardHost}/{ctx.PathAndQuery}");

            HttpContent GetContent()
            {
                return new StringContent(ctx.Body);
            }

            void CopyHeaders(HttpRequestMessage x)
            {
                foreach (var (k, v) in ctx.Headers)
                {
                    x.Headers.TryAddWithoutValidation(k, v.ToArray());
                }
            }
        }

        private static IMockServerResponseContext CreateResponse(HttpResponseMessage sourceResponse)
        {
            var body = new MemoryStream();
            sourceResponse.Content.CopyToAsync(body);

            return new MockServerResponseContext
                   {
                           ContentType = sourceResponse.Content.Headers.ContentType.ToString(),
                           StatusCode = sourceResponse.StatusCode,
                           Body = body,
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

        private static bool TryGetProxyHeader(IMockServerRequestContext context, out string result)
        {
            var hasProxyHeader = context.Headers.TryGetValue(ProxyHeaderName, out var header);
            result = header;
            return hasProxyHeader && header.Count > 0;
        }
    }
}