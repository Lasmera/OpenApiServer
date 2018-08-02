using System;

using ITExpert.OpenApi.Server.Core.MockServer.Generation.Internals;

using Microsoft.OpenApi.Writers;

using Newtonsoft.Json.Schema;

namespace ITExpert.OpenApi.Server.Core.MockServer.Generation.Generators
{
    public class DateTimeGenerator : IOpenApiExampleProvider
    {
        public bool TryWriteValue(IOpenApiWriter writer, JSchema schema)
        {
            var isDateTime = schema.IsFormattedString("date-time");
            if (!isDateTime)
            {
                return false;
            }

            var value = DateTime.UtcNow.ToString("O");
            writer.WriteValue(value);

            return true;
        }
    }
}