using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RagSharpLib.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public class RagSharpPropertyAttribute : Attribute
    {
        public string _description;
        public bool _required = false;
        public RagSharpPropertyAttribute(string description, bool required = false)
        {
            _description = description;
            _required = required;
        }
    }
}
