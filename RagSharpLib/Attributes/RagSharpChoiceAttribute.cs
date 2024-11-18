using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RagSharpLib.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class RagSharpChoiceAttribute : Attribute
    {
        public RagSharpChoiceAttribute()
        {

        }
    }
}
