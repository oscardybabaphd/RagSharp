using EventBroadcasting;
using Newtonsoft.Json;
using RagSharpLib.Attributes;
using RagSharpLib.Common;
using RagSharpLib.EventHandler;
using RagSharpLib.HttpClientHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;

namespace RagSharpLib.OpenAI
{
    // Interface for building the OpenAI chat client
    public interface IOpenAIChatBuilder
    {
        IOpenAIChatBuilder AddTool(Type type);
        IOpenAIChatClient Build();
    }

    // Interface for the OpenAI chat client
    public interface IOpenAIChatClient
    {
        Task<ToolCallResponse<T>> CreateAsync<T>(RagSharpOpenAIChatMessages messages) where T : class?;
        Task<ToolCallResponse<object>> CreateAsync(RagSharpOpenAIChatMessages messages);
        Task<string> GetSchemaAsync();
    }

    // Builder class for configuring and creating the OpenAI chat client
    public class RagSharpOpenAIChat : IOpenAIChatBuilder
    {
        private readonly Settings _settings;
        private readonly List<ClassType> _tools = new List<ClassType>();

        public RagSharpOpenAIChat(Settings settings)
        {
            _settings = settings;
        }

        public IOpenAIChatBuilder AddTool(Type type)
        {
            if (!_tools.Any(x => x.Name == type.Name && x.Type == type))
            {
                _tools.Add(new ClassType
                {
                    IsStatic = type.IsAbstract && type.IsSealed,
                    Name = type.Name,
                    Type = type,
                });
            }
            return this;
        }

        public IOpenAIChatClient Build()
        {
            if (_tools.Count == 0)
                throw new InvalidOperationException("No tool added. Please add at least one tool before building the client.");

            return new RagSharpOpenAIChatClient(_settings, _tools);
        }
    }

    // OpenAI chat client responsible for interacting with the OpenAI API
    public class RagSharpOpenAIChatClient : IOpenAIChatClient
    {
        private const string OpenAIBaseUrl = "https://api.openai.com/v1";
        private readonly Settings _settings;
        private readonly List<ClassType> _tools;
        private bool logModelEvent { get; set; }
        private List<dynamic> methodCallResults = new List<dynamic>();
        public RagSharpOpenAIChatClient(Settings settings, List<ClassType> tools)
        {
            _settings = settings;
            _tools = tools;
        }

        public async Task<ToolCallResponse<T>> CreateAsync<T>(RagSharpOpenAIChatMessages messages) where T : class
        {
            if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>))
                throw new NotSupportedException("RagSharp does not support Collection since OpenAI doesn't support direct collection response.");
            logModelEvent = messages.LogModelEvent;
            var schema = new AttributeParser().GenerateToolSchema(_tools);
            dynamic toolChoice = schema.ToolChoice == null ? "auto" : new
            {
                type = "function",
                function = new
                {
                    name = schema.ToolChoice
                }
            };

            var completionRequest = new RagSharpOpenAIChatCompletionModel
            {
                Messages = messages.Messages,
                Model = _settings.Model,
                Temperature = _settings.Temperature,
                Tool = schema.ToolFunctions,
                ToolChoice = toolChoice
            };

            var response = await MakeHttpCallAsync(completionRequest, logModelEvent);

            if (response.Choices[0].FinishReason == "stop")
            {
                bool CanparseResponse = (typeof(T).Name.ToLower() == "object") ? false : true;
                RagSharpOpenAIChatCompletionResponseModel parsedResponse = null;

                if (CanparseResponse)
                {
                    dynamic responseFormat = new RagSharpZodSchemaGenerator(messages.Strict, messages.AdditionalProperties)
                        .GenerateZodJsonSchema<T>();
                    parsedResponse = await ParseResponseAsync(completionRequest, responseFormat, response.Choices[0].Message.Content);
                }

                try
                {
                    return new ToolCallResponse<T>
                    {
                        Content = response.Choices[0].Message.Content,
                        ParsedResponse = parsedResponse != null ? JsonConvert.DeserializeObject<T>(parsedResponse.Choices[0].Message.Content) : null,
                        IsParsed = parsedResponse != null
                    };
                }
                catch (Exception)
                {
                    throw new InvalidCastException(parsedResponse.Choices[0].Message.Content);
                }
            }
            else
            {
                throw new InvalidOperationException($"Unable to complete request due to {response.Choices[0].FinishReason}");
            }

        }

        public async Task<ToolCallResponse<object>> CreateAsync(RagSharpOpenAIChatMessages messages)
        {
            logModelEvent = messages.LogModelEvent;
            return await CreateAsync<object>(messages);
        }

        public async Task<string> GetSchemaAsync()
        {
            var schema = new AttributeParser().GenerateToolSchema(_tools);
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            return JsonConvert.SerializeObject(schema.ToolFunctions, settings);
        }

        private async Task<RagSharpOpenAIChatCompletionResponseModel> ParseResponseAsync(RagSharpOpenAIChatCompletionModel completionRequest, dynamic format, string content)
        {
            completionRequest.Messages = new List<Messages>
            {
                new Messages { Role = "system", Content = $"Extract the information into JSON format, Use the following data as context for parsing: {JsonConvert.SerializeObject(methodCallResults)}" },
                new Messages { Role = "user", Content = content }
            };
            completionRequest.ToolChoice = null;
            completionRequest.Tool = null;
            completionRequest.ResponseFormat = format;
            return await MakeHttpCallAsync(completionRequest, logModelEvent);
        }

        private async Task<RagSharpOpenAIChatCompletionResponseModel> MakeHttpCallAsync(RagSharpOpenAIChatCompletionModel completionRequest, bool logModelEvent = false)
        {
            string url = $"{OpenAIBaseUrl}/chat/completions";

            var response = await HttpHelper.PostAsync<RagSharpOpenAIChatCompletionModel, RagSharpOpenAIChatCompletionResponseModel>(url, completionRequest, _settings.APIKey, logModelEvent);



            while (response.Choices[0].FinishReason == "tool_calls")
            {
                completionRequest.Messages.Add(new Messages
                {
                    Role = "assistant",
                    ToolCall = response.Choices[0].Message.ToolCalls
                });

                foreach (var toolCall in response.Choices[0].Message.ToolCalls)
                {
                    var result = await toolCall.ExecuteAsync(_tools);
                    var _message = new Messages
                    {
                        Role = "tool",
                        ToolCallId = toolCall.Id
                    };
                    if (result != null)
                    {
                        methodCallResults.Add(result);
                        _message.Content = JsonConvert.SerializeObject(result);
                        completionRequest.Messages.Add(_message);
                    }
                    else
                    {
                        _message.Content = "No record return for the tool call";
                        completionRequest.Messages.Add(_message);
                    }
                }
                response = await HttpHelper.PostAsync<RagSharpOpenAIChatCompletionModel, RagSharpOpenAIChatCompletionResponseModel>(url, completionRequest, _settings.APIKey, logModelEvent);
            }

            return response;
        }

    }

    // Class representing a tool's type and metadata
    public class ClassType
    {
        public string Name { get; set; }
        public bool IsStatic { get; set; }
        public Type Type { get; set; }
    }
}
