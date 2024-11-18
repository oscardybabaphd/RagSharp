using EventBroadcasting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RagSharpLib.EventHandler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RagSharpLib.Common
{
    public class RagSharpZodSchemaGenerator
    {
        private bool _additionalProperties;
        private bool _strict;
        public RagSharpZodSchemaGenerator(bool strict, bool additionalProperties)
        {
            _additionalProperties = additionalProperties;
            _strict = strict;
        }
        public JObject GenerateZodJsonSchema<T>()
        {
            var type = typeof(T);
            var schema = new JObject
            {
                ["type"] = "json_schema",
                ["json_schema"] = new JObject
                {
                    ["name"] = type.Name.ToLower(),
                    ["schema"] = GetSchemaForType(type),
                    ["strict"] = _strict
                }
            };

            return schema;
        }

        private JObject GetSchemaForType(Type type)
        {
            var schema = new JObject
            {
                ["type"] = "object",
                ["properties"] = GetPropertiesSchema(type),
                ["required"] = GetRequiredProperties(type),
                ["additionalProperties"] = _additionalProperties
            };

            return schema;
        }

        private JObject GetPropertiesSchema(Type type)
        {
            var properties = new JObject();
            foreach (var property in type.GetProperties())
            {
                var propertyType = property.PropertyType;
                var propertySchema = new JObject
                {
                    ["type"] = GetJsonType(propertyType)
                };

                if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var itemType = propertyType.GetGenericArguments()[0];
                    propertySchema["type"] = "array";
                    propertySchema["items"] = propertyType == typeof(string)
                        ? new JObject { ["type"] = "string" }
                        : GetSchemaForType(itemType);
                }
                else if (!propertyType.IsValueType && propertyType != typeof(string))
                {
                    propertySchema = GetSchemaForType(propertyType);
                }

                properties[property.Name.ToLower()] = propertySchema;
            }
            return properties;
        }

        private JArray GetRequiredProperties(Type type)
        {
            var requiredProperties = type.GetProperties()
                //.Where(p => !IsNullable(p))
                .Select(p => p.Name.ToLower())
                .ToList();

            return new JArray(requiredProperties);
        }

        private bool IsNullable(PropertyInfo property)
        {
            if (!property.PropertyType.IsValueType) return true;
            if (Nullable.GetUnderlyingType(property.PropertyType) != null) return true;
            return false;
        }

        private string GetJsonType(Type type)
        {
            if (type == typeof(string)) return "string";
            if (type == typeof(int) || type == typeof(long) || type == typeof(short)) return "integer";
            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal)) return "number";
            if (type == typeof(bool)) return "boolean";
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) return "array";
            if (type.IsEnum) return "integer";
            if (type == typeof(DateTime)) return "string";
            return "object"; // Default to object for other types
        }
    }
}
