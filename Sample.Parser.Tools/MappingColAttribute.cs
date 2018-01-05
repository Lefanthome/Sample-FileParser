using System;
using System.Collections.Generic;
using System.Text;

namespace Sample.Parser.Tools
{
    [AttributeUsage(AttributeTargets.Property)]
    public class MappingColAttribute : Attribute
    {
        public string ColName { get; set; }

        public MappingColAttribute(string colName)
        {
            ColName = colName;
        }
    }
}
