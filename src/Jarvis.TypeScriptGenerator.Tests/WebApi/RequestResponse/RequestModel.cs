using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.TypeScriptGenerator.Tests.WebApi.RequestResponse
{
    public class RequestModel
    {
        public class Nested
        {
            public string Id { get; set; }
        }

        public string ThisIsAString { get; set; }
        public double ThisIsADouble { get; set; }
        public short ThisIsAShort { get; set; }
        public int ThisIsAnInt { get; set; }
        public long ThisIsALong { get; set; }
        public bool ThisIsABoolean { get; set; }
        public DateTime ThisIsADate { get; set; }
        public Nested ThisIsANestedObject { get; set; }

        public IList<Nested> NestedList { get; set; }
    }

    public class ResponseModel
    {
        public bool Succeeded { get; set; }
        public IEnumerable<string> StringList { get; set; }
    }

    public class ResponseEnumerableItemModel
    {
        public string Text { get; set; }
    }
}
