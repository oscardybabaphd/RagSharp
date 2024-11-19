using EventBroadcasting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RagSharpLib.EventHandler;
using RagSharpLib.OpenAI;
using System.Reflection;

namespace RagSharpLib.Attributes
{
    internal class AttributeParser
    {
        public (List<ToolFunctionSchema> ToolFunctions, string ToolChoice) GenerateToolSchema(List<ClassType> classList, IServiceProvider serviceProvider)
        {
            var toolFunctions = new List<ToolFunctionSchema>();
            string choice = null;

            foreach (var obj in classList)
            {
                Type type = GetClassType(obj, serviceProvider);

                var methods = type.GetMethods()
                    .Where(method => method.IsDefined(typeof(RagSharpToolAttribute), false))
                    .ToArray();

                var toolChoices = type.GetMethods()
                    .Where(method => method.IsDefined(typeof(RagSharpChoiceAttribute), false))
                    .ToArray();

                if (toolChoices.Length > 1)
                {
                    throw new ArgumentException("Only one RagSharpChoice attribute is allowed per tool call.");
                }
                else if (toolChoices.Length == 1)
                {
                    choice = toolChoices[0].Name;
                }

                foreach (var method in methods)
                {
                    var methodAttribute = method.GetCustomAttribute<RagSharpToolAttribute>();
                    var methodParams = GetMethodParameter(method);
                    var attributeValues = GetRagSharpMethodAttributeValue(methodAttribute);

                    var toolFunctionSchema = new ToolFunctionSchema
                    {
                        Type = "function",
                        Function = new FunctionDefinition
                        {
                            Name = method.Name,
                            Strict = attributeValues.Strict,
                            Description = attributeValues.Description,
                            Parameters = new Parameters
                            {
                                Type = "object",
                                Properties = methodParams.Properties ?? new Dictionary<string, PropertyDefinition>(),
                                Required = methodParams.RequiredPropNames,
                                AdditionalProperties = attributeValues.AdditionalProperties
                            }
                        }
                    };
                    toolFunctions.Add(toolFunctionSchema);
                }
            }

            return (toolFunctions, choice);
        }

        private Type GetClassType(ClassType classType, IServiceProvider serviceProvider)
        {
            try
            {
                if (!classType.IsStatic)
                {
                    object instance = null;
                    var constructor = classType.Type.GetConstructors().FirstOrDefault();
                    if (constructor != null && constructor.GetParameters().Length == 0)
                    {
                        // If there is a parameterless constructor, create an instance directly
                        instance = Activator.CreateInstance(classType.Type);
                    }
                    else
                    {
                        if (serviceProvider == null)
                            throw new InvalidOperationException("Service provider cannot be null for for an instance type with one or constructor ");
                        // Use the existing service provider to resolve dependencies if registered
                        var service = serviceProvider?.GetService(classType.Type);
                        if (service == null)
                            throw new InvalidOperationException($"Unable to locate service container of type '{classType.Type.Name}'");
                        instance = service;
                    }
                    return instance.GetType();
                }
                return classType.Type;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            //return classType.IsStatic ? classType.Type : Activator.CreateInstance(classType.Type).GetType();
        }

        private (Dictionary<string, PropertyDefinition> Properties, string[] RequiredPropNames) GetMethodParameter(MethodInfo methodInfo)
        {
            var parameters = methodInfo.GetParameters();
            if (parameters.Length == 0)
                return (null, null);

            string parameterType = CheckIfParameterIsMixed(parameters);

            return parameterType switch
            {
                "IsPrimitive" => ExtractPrimitiveProperty(parameters),
                "IsUserDefined" => parameters.Length == 1
                    ? ExtractUserDefinedProperty(parameters[0])
                    : throw new ArgumentException("RagSharp only supports single object parameter for user-defined types."),
                _ => (null, null)
            };
        }

        private string CheckIfParameterIsMixed(ParameterInfo[] parameters)
        {
            bool hasPrimitive = parameters.Any(p => IsAllowedPrimitive(p.ParameterType));
            bool hasUserDefined = parameters.Any(p => IsUserDefined(p.ParameterType));

            if (hasPrimitive && hasUserDefined)
                throw new ArgumentException("Mixed types are not allowed. Use either multiple primitive types or a single user-defined object.");

            return hasUserDefined ? "IsUserDefined" : "IsPrimitive";
        }

        private bool IsUserDefined(Type type)
        {
            return !type.IsPrimitive
                && type != typeof(string)
                && type != typeof(decimal)
                && type != typeof(DateTime)
                && !type.IsEnum
                && !type.IsGenericTypeDefinition
                && !type.IsValueType
                && type.Namespace != null
                && !type.Namespace.StartsWith("System");
        }

        private bool IsAllowedPrimitive(Type type)
        {
            Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;
            return underlyingType.IsPrimitive
                || underlyingType == typeof(string)
                || underlyingType == typeof(decimal)
                || underlyingType == typeof(DateTime)
                //|| type == typeof(DateTimeOffset) // to use these primitive you can uncomment this lines 
                //|| type == typeof(TimeSpan)
                || underlyingType == typeof(Guid)
                || underlyingType == typeof(char)
                || underlyingType.IsEnum;
        }

        private string GetType(Type type)
        {
            if (type == typeof(string)) return "string";
            if (type == typeof(int) || type == typeof(long) || type == typeof(short)) return "integer";
            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal)) return "number";
            if (type == typeof(bool)) return "boolean";
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) return "array";
            if (type.IsEnum) return "integer";
            return "object"; // Default to object for other types
        }
        private (Dictionary<string, PropertyDefinition> Properties, string[] RequiredPropNames) ExtractPrimitiveProperty(ParameterInfo[] parameterInfo)
        {
            var requiredFields = new List<string>();
            var properties = new Dictionary<string, PropertyDefinition>();

            foreach (var parameter in parameterInfo)
            {
                if (!IsAllowedPrimitive(parameter.ParameterType))
                    throw new ArgumentException($"Unsupported primitive type '{parameter.Name}'.");

                var customAttribute = parameter.GetCustomAttribute<RagSharpPropertyAttribute>();
                if (customAttribute == null)
                {
                    if (parameter.HasDefaultValue)
                        continue;
                    throw new ArgumentException($"Please specify RagSharp attribute for the '{parameter.Name}' parameter or make the function parameterless.");
                }

                var attributeValues = GetRagSharpPropsAttributeValue(customAttribute);

                if (attributeValues.IsRequired)
                {
                    requiredFields.Add(parameter.Name);
                }

                var propertyDefinition = new PropertyDefinition
                {
                    Type = GetType(parameter.ParameterType),
                    Description = attributeValues.Description,
                    Enum = parameter.ParameterType.IsEnum ? Enum.GetNames(parameter.ParameterType) : null
                };

                properties.Add(parameter.Name, propertyDefinition);
            }

            return (properties, requiredFields.ToArray());
        }

        private (Dictionary<string, PropertyDefinition> Properties, string[] RequiredPropNames) ExtractUserDefinedProperty(ParameterInfo parameterInfo)
        {
            var properties = new Dictionary<string, PropertyDefinition>();
            var requiredFields = new List<string>();
            var parameterType = parameterInfo.ParameterType;

            foreach (var prop in parameterType.GetProperties())
            {
                var customAttribute = prop.GetCustomAttribute<RagSharpPropertyAttribute>();
                if (customAttribute == null)
                    continue;

                var attributeValues = GetRagSharpPropsAttributeValue(customAttribute);

                if (attributeValues.IsRequired)
                {
                    requiredFields.Add(prop.Name);
                }

                if (IsAllowedPrimitive(prop.PropertyType) || prop.PropertyType.IsEnum)
                {
                    var propertyDefinition = new PropertyDefinition
                    {
                        Type = GetType(prop.PropertyType),
                        Description = attributeValues.Description,
                        Enum = prop.PropertyType.IsEnum ? Enum.GetNames(prop.PropertyType) : null
                    };

                    properties.Add(prop.Name, propertyDefinition);
                }
            }

            return (properties, requiredFields.ToArray());
        }

        private (string Description, bool IsRequired) GetRagSharpPropsAttributeValue(RagSharpPropertyAttribute customAttribute)
        {
            if (string.IsNullOrEmpty(customAttribute._description))
                throw new InvalidOperationException("RagSharp attribute must have a description.");

            return (customAttribute._description, customAttribute._required);
        }

        private (string Description, bool AdditionalProperties, bool Strict) GetRagSharpMethodAttributeValue(RagSharpToolAttribute customAttribute)
        {
            if (string.IsNullOrEmpty(customAttribute._description))
                throw new InvalidOperationException("RagSharp attribute must have a description.");

            return (customAttribute._description, customAttribute._additionalProperties, customAttribute._strict);
        }
    }

    public class ToolFunctionSchema
    {
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("function")]
        public FunctionDefinition Function { get; set; }
    }

    public class FunctionDefinition
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("strict")]
        public bool Strict { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("parameters")]
        public Parameters Parameters { get; set; }
    }

    public class Parameters
    {
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("properties")]
        public Dictionary<string, PropertyDefinition> Properties { get; set; }
        [JsonProperty("required")]
        public string[] Required { get; set; }

        [JsonProperty("additionalProperties")]
        public bool AdditionalProperties { get; set; }
    }

    public class PropertyDefinition
    {
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("enum")]
        public string[] Enum { get; set; }
    }
}
