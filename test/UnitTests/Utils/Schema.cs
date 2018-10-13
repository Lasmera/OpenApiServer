using Newtonsoft.Json.Schema;

namespace UnitTests.Utils
{
    public static class Schema
    {
        public static JSchema Any() => new JSchema();

        public static JSchema Int() => new JSchema {Type = JSchemaType.Integer};
        public static JSchema Number() => new JSchema {Type = JSchemaType.Number};
        public static JSchema String(string format = null) => new JSchema {Type = JSchemaType.String, Format = format};
        public static JSchema Boolean() => new JSchema {Type = JSchemaType.Boolean};

        public static JSchema DateTime() => String("date-time");
        public static JSchema Base64() => String("base64");

        public static JSchema Enum(params string[] values)
        {
            var schema = String();
            foreach (string value in values)
            {
                schema.Enum.Add(value);
            }

            return schema;
        }

        public static JSchema Object() => new JSchema {Type = JSchemaType.Object};

        public static JSchema Array(JSchema item, int? minItems = null) =>
                new JSchema
                {
                        Type = JSchemaType.Array,
                        Items = {item},
                        MinimumItems = minItems
                };
    }
}