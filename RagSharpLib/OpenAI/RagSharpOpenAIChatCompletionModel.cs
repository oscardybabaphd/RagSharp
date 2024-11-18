using Newtonsoft.Json;


namespace RagSharpLib.OpenAI
{
    internal class RagSharpOpenAIChatCompletionModel
    {
        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("messages")]
        public List<Messages> Messages { get; set; }

        [JsonProperty("temperature")]
        public double Temperature { get; set; }

        [JsonProperty("tools")]
        public dynamic Tool { get; set; }

        [JsonProperty("tool_choice")]
        public dynamic ToolChoice { get; set; }

        [JsonProperty("response_format")]
        public dynamic ResponseFormat { get; set; }
    }

    public class Settings
    {
        public string Model { get; set; }
        public string APIKey { get; set; }
        public double Temperature { get; set; } = 0.2;
    }
    public enum ToolChoiceEnum
    {
        Required,
        None,
        Function
    }
}
