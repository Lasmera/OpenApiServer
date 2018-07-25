using System.Collections.Generic;
using System.IO;
using System.Net;

using Microsoft.Extensions.Primitives;

namespace ITExpert.OpenApi.Server.Core.MockServer
{
    public class MockServerResponseContext : IMockServerResponseContext
    {
        public HttpStatusCode StatusCode { get; set; }

        public Stream Body { get; set; }

        public string ContentType { get; set; }

        public IDictionary<string, StringValues> Headers { get; set; }
    }
}