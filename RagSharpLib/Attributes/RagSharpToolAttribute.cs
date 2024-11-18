using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RagSharpLib.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class RagSharpToolAttribute : Attribute
    {
        public string _description;
        public bool _additionalProperties;
        public bool _strict;
        public RagSharpToolAttribute(string description, bool additionalProperties = false, bool strict = true)
        {
            _description = description;
            _additionalProperties = additionalProperties;
            _strict = strict;
        }
    }
}
