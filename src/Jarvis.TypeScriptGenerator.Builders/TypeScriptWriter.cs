using System;
using System.Collections.Generic;
using System.Text;
using TypeLite;

namespace Jarvis.TypeScriptGenerator.Builders
{
    public class TypeScriptWriter
    {
        private class IndentScope : IDisposable
        {
            private readonly TypeScriptWriter _writer;

            public IndentScope(TypeScriptWriter writer)
            {
                _writer = writer;
                _writer.IncIndent();
            }

            public void Dispose()
            {
                _writer.DecIndent();
            }
        }

        private readonly HashSet<Type> _knownTypes = new HashSet<Type>();
        readonly StringBuilder _sb = new StringBuilder();
        private int _indentLevel = 0;

        public void IncIndent()
        {
            _indentLevel++;
        }

        public void DecIndent()
        {
            _indentLevel--;
        }

        public IDisposable Indent()
        {
            return new IndentScope(this);
        }

        private string IndentString()
        {
            return "                                                      ".Substring(0, 2 * _indentLevel);
        }

        public void StartLine(string line)
        {
            _sb.Append(IndentString());
            _sb.Append(line);
        }

        public void NewLine()
        {
            _sb.Append("\n");
        }

        public void StartLine(string line, params object[] args)
        {
            _sb.Append(IndentString());
            _sb.AppendFormat(line, args);
        }

        public string Write(string moduleName)
        {
            TsGenerator generator = new TsJamesModelGenerator();
            var ts = new TypeScriptFluent(generator);
            ts.WithTypeFormatter((type, formatter) => "I" + ((TypeLite.TsModels.TsClass)type).Name);
            ts.WithMemberFormatter((identifier) => TextUtils.CamelCase(identifier.Name));
            // custom module wrapping!
            ts.WithModuleNameFormatter(module => string.Empty);
            generator.SetTypeVisibilityFormatter((@class, name) => true);   // export
            generator.IndentationString = "    ";

            foreach (var knownType in _knownTypes)
            {
                if ((knownType.IsClass && knownType.Namespace != "System") || knownType.IsEnum )
                {
                    ts.For(knownType);
                }
            }

            var tsModule = ts.Generate();
            var controller = _sb.ToString();

            return string.Format("module {0}{{{1}{2}}}",
                moduleName,
                tsModule,
                controller
                );
        }

        public void AppendFormat(string text, params object[] args)
        {
            _sb.AppendFormat(text, args);
        }

        public void Append(string text)
        {
            _sb.Append(text);
        }

        public void AddType(Type type)
        {
            if (type.IsArray)
            {
                _knownTypes.Add(type.GetElementType());
                return;
            }
            
            if(type.IsGenericType && type.IsAbstract)
            {
                foreach (var gt in type.GenericTypeArguments)
                {
                    AddType(gt);
                }
            }

            _knownTypes.Add(type);
        }
    }
}