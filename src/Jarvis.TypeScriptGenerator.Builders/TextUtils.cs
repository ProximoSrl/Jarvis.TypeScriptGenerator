using System;
using System.Text;

namespace Jarvis.TypeScriptGenerator.Builders
{
    public static class TextUtils
    {
        public static string CamelCase(string identifier)
        {
            var sb = new StringBuilder();
            for (int index = 0; index < identifier.Length; index++)
            {
                char c = identifier[index];
                var toLower = index == 0 || (index < identifier.Length - 1 && Char.IsUpper(identifier[index + 1]));
                sb.Append(toLower ? Char.ToLower(c) : c);
            }

            return sb.ToString();
        }
    }
}