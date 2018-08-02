using System.Collections.Generic;
using System.Net;

using Microsoft.Extensions.Primitives;

namespace ITExpert.OpenApi.Server.Core.MockServer.Context.Types
{
    public class MockServerResponseContext
    {
        public HttpStatusCode StatusCode { get; set; }

        public string Body { get; set; }

        public string ContentType { get; set; }

        public IDictionary<string, StringValues> Headers { get; set; } = new Dictionary<string, StringValues>();
    }
}