using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.TypeScriptGenerator.Builders;
using NUnit.Framework;

namespace Jarvis.TypeScriptGenerator.Tests.WebApi.RequestResponse
{
    [TestFixture]
    public class RequestResponseTests
    {
        [Test]
        public void generate()
        {
            var root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"WebApi");
            var builder = new TypeScriptBuilder(root,"apiModule");
            builder.AddReference("typings/tsd.d.ts");

            var ts = builder.GenerateClientApi(typeof (RequestResponseController), "Demo.Api","http://demo/api/");
            Debug.WriteLine("File generated: {0}",(object)ts);
        }
    }
}
