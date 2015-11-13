using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Http;
using Fasterflect;
using Swashbuckle.Swagger.Annotations;

namespace Jarvis.TypeScriptGenerator.Builders
{
    public class TypeScriptApiController
    {
        private readonly string _ns;
        private readonly string _cname;
        private readonly IDictionary<string, string> _dependencies;
        private readonly Method[] _methods;
        private readonly Type _type;
        private readonly string _moduleName;
        private readonly string _endpoint;

        public TypeScriptApiController(string ns, Type type, string moduleName, string endpoint)
        {
            _ns = ns;
            _type = type;
            _moduleName = moduleName;
            _endpoint = endpoint;

            _methods = _type.Methods(Flags.InstancePublicDeclaredOnly)
                .Where(x => !x.Name.StartsWith("get_") && !x.Name.StartsWith("set_"))
                .Select(x => new Method(x))
                .ToArray();

            _dependencies = new Dictionary<string, string>
            {
                {"$http", "ng.IHttpService"}
            };
            _cname = _type.Name.Replace("Controller", "Api");
        }

        public string FileName
        {
            get { return _cname; }
        }

        private void WriteTo(TypeScriptWriter writer)
        {
            WriteInterface(writer);
            WriteImpl(writer);
        }

        private void WriteInterface(TypeScriptWriter writer)
        {
            writer.IncIndent();
            writer.StartLine("export interface I{0} {{\n", _type.Name.Replace("Controller", "Api"));

            using (writer.Indent())
            {
                foreach (var method in _methods)
                {
                    WriteMethodSignature(writer, method);
                    writer.Append(";\n");
                }
            }
            writer.StartLine("}\n");
        }

        private void WriteMethodSignature(TypeScriptWriter writer, Method method)
        {
            writer.StartLine("{0}(", TextUtils.CamelCase(method.Name));
            for (var index = 0; index < method.ParamInfos.Length; index++)
            {
                if (index > 0) writer.Append(", ");
                var pi = method.ParamInfos[index];

                writer.AppendFormat("{0}: {1}",
                    TextUtils.CamelCase(pi.Name),
                    GetTsType(pi.Type)
                    );

                writer.AddType(pi.Type);
            }
            writer.Append(")");

            if (method.ReturnType == typeof(HttpResponseMessage))
            {
                writer.Append(" : any");
            }
            else if (method.ReturnType == typeof(void))
            {
                writer.Append(" : ng.IHttpPromise<{}>");
            }
            else
            {
                var returnType = GetTsType(method.ReturnType);
                writer.AddType(method.ReturnType);
                writer.AppendFormat(" : ng.IHttpPromise<{0}>", returnType);
            }
        }

        private string GetTsType(Type type)
        {
            if (type.IsGenericType && type.IsAbstract)
            {
                if (type.GetGenericTypeDefinition() == typeof (IEnumerable<>))
                {
                    return GetTsType(type.GenericTypeArguments[0]) + "[]";
                }
            }
            return "I" + type.Name;
        }

        private void WriteImpl(TypeScriptWriter writer)
        {
            writer.StartLine("\n");
            writer.StartLine("/* implementation */\n");
            writer.StartLine("class {0} implements I{0}{{\n", _cname);
            using (writer.Indent())
            {
                writer.StartLine("\n");
                writer.StartLine("static endpoint =\"{0}\";\n", _endpoint);

                /*
                 * $inject
                 * */
                writer.StartLine("static $inject = [");

                var keys = _dependencies.Keys.ToArray();

                for (var index = 0; index < keys.Length; index++)
                {
                    var dependency = keys[index];
                    if (index > 0)
                        writer.Append(", ");
                    writer.AppendFormat("\"{0}\"", dependency);
                }

                writer.Append("];\n");

                /*
                 * constructor
                 * */
                writer.NewLine();
                writer.StartLine("constructor(\n");
                using (writer.Indent())
                {
                    for (var index = 0; index < keys.Length; index++)
                    {
                        var dependency = keys[index];
                        if (index > 0)
                            writer.Append(",\n");
                        writer.StartLine("private {0}: {1}", dependency, _dependencies[dependency]);
                    }
                    writer.Append("\n");
                }
                writer.StartLine(") { }\n");

                /*
                 * factory
                 * @@TODO switch to service
                 * */
                writer.NewLine();
                writer.StartLine("static factory() {\n");
                using (writer.Indent())
                {
                    writer.StartLine("return (");
                    for (var index = 0; index < keys.Length; index++)
                    {
                        var dependency = keys[index];
                        if (index > 0)
                            writer.Append(",");
                        writer.AppendFormat("{0}: {1}", dependency, _dependencies[dependency]);
                    }
                    writer.AppendFormat(") => new {0}(", _cname);
                    for (var index = 0; index < keys.Length; index++)
                    {
                        var dependency = keys[index];
                        if (index > 0)
                            writer.Append(",");
                        writer.Append(dependency);
                    }
                    writer.Append(");\n");
                }
                writer.StartLine("}\n");

                /*
                 * methods
                 * */
                var controllerRoot = _type.Name.ToLowerInvariant().Replace("controller", "");
                foreach (var method in _methods)
                {
                    writer.NewLine();
                    var methodUri = method.GetUri(controllerRoot);
                    WriteMethodSignature(writer, method);
                    writer.Append("{\n");
                    var httpMethod = method.HttpMethod;

                    using (writer.Indent())
                    {
                        if (method.ParamInfos.Length == 0)
                        {
                            writer.StartLine(
                                "return this.$http.{0}<{3}>({2}.endpoint + \"{1}\", {{}});\n",
                                httpMethod, methodUri, _cname, GetTsType(method.ReturnType) 
                            );
                        }
                        else if (method.ParamInfos.Length == 1)
                        {
                            writer.StartLine(
                                "return this.$http.{0}<{4}>({3}.endpoint + \"{1}\", {2});\n",
                                httpMethod, methodUri, method.ParamInfos.First().Name, _cname, GetTsType(method.ReturnType)
                            );
                        }
                        else
                        {
                            throw new NotImplementedException("todo");
                        }
                    }

                    writer.StartLine("}\n");
                }
            }
            writer.StartLine("}\n");

            writer.NewLine();
            writer.StartLine("export const apiId = \"{0}\";\n", TextUtils.CamelCase(_cname));
            writer.StartLine("angular.module(\"{0}\")\n", _moduleName);
            writer.StartLine("       .factory(apiId, {0}.factory());", _cname);
            writer.NewLine();
        }

        public string Generate()
        {
            var writer = new TypeScriptWriter();
            WriteTo(writer);
            return writer.Write(_ns + "." + _cname.Replace("Api", ""));
        }

        private class ParamInfo
        {
            public ParamInfo(ParameterInfo parameterInfo)
            {
                Name = parameterInfo.Name;
                Type = parameterInfo.ParameterType;
            }

            public string Name { get; private set; }
            public Type Type { get; private set; }
        }

        private class Method
        {
            public Method(MethodInfo mi)
            {
                HttpMethod = "get";
                Name = mi.Name;
                ParamInfos = mi.Parameters().Select(x => new ParamInfo(x)).ToArray();
                ReturnType = mi.ReturnType;

                var swaggerAttrs =
                    mi.GetCustomAttributes(typeof(SwaggerResponseAttribute), false).Cast<SwaggerResponseAttribute>();
                var ok = swaggerAttrs.SingleOrDefault(x => x.StatusCode == (int)HttpStatusCode.OK);
                if (ok != null)
                {
                    ReturnType = ok.Type;
                }

                var isPost = mi.GetCustomAttributes(typeof(HttpPostAttribute), false).Any();
                if (isPost)
                {
                    HttpMethod = "post";
                }
            }

            public string GetUri(string root)
            {
                return root + "/" + Name.ToLowerInvariant();
            }

            public string Name { get; private set; }
            public ParamInfo[] ParamInfos { get; private set; }
            public Type ReturnType { get; private set; }
            public string HttpMethod { get; private set; }
        }
    }
}
