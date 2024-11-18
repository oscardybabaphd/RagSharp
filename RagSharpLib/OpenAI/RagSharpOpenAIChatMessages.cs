using Newtonsoft.Json;

namespace RagSharpLib.OpenAI
{
    public class RagSharpOpenAIChatMessages
    {
        [JsonProperty("messages")]
        public List<Messages> Messages { get; set; }
        public bool Strict { get; set; } = true;
        public bool AdditionalProperties { get; set; } = false;
        public bool LogModelEvent { get; set; } = false;
    }

    public class Messages
    {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("tool_call_id")]
        public string ToolCallId { get; internal set; }

        [JsonProperty("tool_calls")]
        public List<ToolCall> ToolCall { get; internal set; }
    }

    public class ToolCallResponse<T> where T : class?
    {
        public ToolCallResponse()
        {

        }
        public string Content { get; set; }
        public T? ParsedResponse { get; set; }
        public bool IsParsed { get; set; }
    }
}
