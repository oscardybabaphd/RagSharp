using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RagSharpLib.OpenAI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace RagSharpLib.Common
{
    internal static class ToolCallExecution
    {
        public static async Task<dynamic> ExecuteAsync(this ToolCall toolCall, List<ClassType> classTypes)
        {
            foreach (var item in classTypes)
            {
                var method = item.Type.GetMethod(toolCall.Function.Name);
                if (method == null)
                    continue;

                if (method.GetParameters().Length == 0) // Parameterless function call
                {
                    return await ExecuteMethodAsync(method, item);
                }
                else
                {
                    // for user defined object we only support one object
                    if (method.GetParameters().Length == 1 && IsUserDefined(method.GetParameters()[0].ParameterType))
                    {
                        return await ExecuteUserDefinedMethodAsync(method, item, toolCall);
                    }
                    else
                    {
                        return await ExecuteMultiplePrimitiveMethodAsync(method, item, toolCall);
                    }
                }
            }
            return new Dictionary<object, object>();
        }

        private static Task<dynamic> ExecuteUserDefinedMethodAsync(MethodInfo method, ClassType item, ToolCall toolCall)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            if (toolCall?.Function?.Arguments == null)
                throw new ArgumentNullException(nameof(toolCall.Function.Arguments));

            Type parameterType = method.GetParameters()[0].ParameterType;

            object deserializedObject;
            try
            {
                deserializedObject = JsonConvert.DeserializeObject(toolCall.Function.Arguments, parameterType);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Deserialization failed.", ex);
            }

            return ExecuteMethodAsync(method, item, new object[] { deserializedObject });
        }
        private static bool IsUserDefined(Type type)
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
        private static async Task<dynamic> ExecuteMultiplePrimitiveMethodAsync(MethodInfo method, ClassType item, ToolCall toolCall)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            if (toolCall?.Function?.Arguments == null)
                throw new ArgumentNullException(nameof(toolCall.Function.Arguments));

            JObject arguments = JObject.Parse(toolCall.Function.Arguments);
            var argumentList = method.GetParameters()
                                     .Select(parameter => GetParameterValue(parameter, arguments))
                                     .ToList();

            return await ExecuteMethodAsync(method, item, argumentList.ToArray());
        }
        private static object GetParameterValue(ParameterInfo parameter, JObject arguments)
        {
            //string parameterName = parameter.Name.ToLower();
            JToken valueToken = arguments[parameter.Name];

            try
            {
                if (valueToken != null)
                {
                    return Convert.ChangeType(valueToken.ToObject(typeof(object)), parameter.ParameterType);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("type cast failed.", ex);
            }
            // Use default value if available; otherwise, return null
            return parameter.HasDefaultValue ? parameter.DefaultValue : null;
        }
        private static async Task<dynamic> ExecuteMethodAsync(MethodInfo method, ClassType item, object[] args = null)
        {
            try
            {
                var instance = !item.IsStatic ? Activator.CreateInstance(item.Type) : null;
                var result = method.Invoke(instance, (args != null) ? args : null);

                if (result is Task task)
                {
                    await task.ConfigureAwait(false);
                    if (task.GetType().IsGenericType)
                    {
                        return ((dynamic)task).Result;
                    }
                    return null;
                }
                else if (result is ValueTask valueTask)
                {
                    await valueTask.ConfigureAwait(false);
                    return null;
                }
                else if (result.GetType().IsGenericType && result.GetType().GetGenericTypeDefinition() == typeof(ValueTask<>))
                {
                    return await (dynamic)result;
                }
                else
                {
                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("method execution failed", ex);
            }
        }
    }
}
