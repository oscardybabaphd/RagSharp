using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RagSharpLib.OpenAI;

namespace RagSharpLib.Common
{
    public abstract class RagSharpChatCompletion<T> where T : class
    {
        public abstract Task<ToolCallResponse<T>> Create(RagSharpOpenAIChatMessages messages);
        public abstract string GetSchema();
    }
}
