using System.Collections.Generic;

using Microsoft.OpenApi.Writers;

using Newtonsoft.Json.Schema;

using OpenApiServer.Core.MockServer.MockDataProviders.Internals;

namespace OpenApiServer.Core.MockServer.MockDataProviders.Providers
{
    //TODO: minProperties, maxProperties
    //TODO: patternProperties
    //TODO: Property dependencies
    public class ObjectProvider : IMockDataProvider
    {
        private IReadOnlyCollection<IMockDataProvider> Providers { get; }
        private ObjectDepthCounter DepthCounter { get; }

        private static readonly string[] AdditionalPropertiesExampleNames =
        {
                "dynamicProp1",
                "dynamicProp2",
                "dynamicProp3"
        };

        public ObjectProvider(IReadOnlyCollection<IMockDataProvider> providers, ObjectDepthCounter counter)
        {
            Providers = providers;
            DepthCounter = counter;
        }

        public bool TryWriteValue(IOpenApiWriter writer, JSchema schema)
        {
            if (!schema.IsObject())
            {
                return false;
            }

            writer.WriteStartObject();

            using (DepthCounter.Enter())
            {
                if (DepthCounter.CanEnter)
                {
                    WriteProperties(writer, schema);
                }
            }

            writer.WriteEndObject();

            return true;
        }

        private void WriteProperties(IOpenApiWriter writer, JSchema schema)
        {
            if (schema.Properties != null)
            {
                foreach (var property in schema.Properties)
                {
                    writer.WritePropertyName(property.Key);
                    Providers.WriteValueOrThrow(writer, property.Value);
                }
            }

            if (schema.AdditionalProperties != null)
            {
                foreach (string property in AdditionalPropertiesExampleNames)
                {
                    writer.WritePropertyName(property);
                    Providers.WriteValueOrThrow(writer, schema.AdditionalProperties);
                }
            }
        }
    }
}