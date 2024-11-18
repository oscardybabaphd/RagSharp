using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RagSharpLib.EventHandler
{
    public class LLMEvent
    {
        public dynamic Data { get; set; }
        public EventType EventType { get; set; }
    }

    public enum EventType
    {
        LLMCallEvent,
        LLMResponseEvent
    }
}
