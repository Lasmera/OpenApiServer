using System.Collections.Generic;
using System.Net.Http;

using Microsoft.OpenApi.Models;

using OpenApiServer.Utils;

namespace OpenApiServer.DocumentProviders
{
    public class WebOpenApiDocumentProvider : IOpenApiDocumentProvider
    {
        private IHttpClientFactory ClientFactory { get; }
        private string Uri { get; }

        public WebOpenApiDocumentProvider(IHttpClientFactory clientFactory, string uri)
        {
            ClientFactory = clientFactory;
            Uri = uri;
        }

        public IEnumerable<OpenApiDocument> GetDocuments()
        {
            var client = ClientFactory.CreateClient();
            var spec = client.GetStringAsync(Uri).Result;
            yield return OpenApiDocumentUtils.ReadSpec(spec);
        }
    }
}