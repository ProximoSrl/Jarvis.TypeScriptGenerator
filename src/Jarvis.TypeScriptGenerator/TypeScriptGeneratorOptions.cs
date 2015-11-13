using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Jarvis.TypeScriptGenerator
{
    public class TypeScriptGeneratorOptions
    {
        public string DestFolder { get; set; }
        public string[] Assemblies { get; set; }
        public string NgModule { get; set; }
        public string[] References { get; set; }
        public string Namespace { get; set; }
        public string ApiRoot { get; set; }

        public TypeScriptGeneratorOptions()
        {
        }

        public void Init(string workingFolder)
        {
            this.DestFolder = workingFolder;
            this.Assemblies = new[] { "path/to/assembly.dll" };
            this.References = new[] { "typings/tsd.d.ts" };
            this.NgModule = "api";
            this.ApiRoot = "http://localhost/api/v1/";
            this.Namespace = "your.api";
        }

        public void Save(string pathToFile)
        {
            File.WriteAllText(pathToFile, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}
