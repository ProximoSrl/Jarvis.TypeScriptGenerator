using TypeLite;
using TypeLite.TsModels;

namespace Jarvis.TypeScriptGenerator.Builders
{
    public class TsJamesModelGenerator : TsGenerator
    {
        protected override void AppendEnumDefinition(TsEnum enumModel, ScriptBuilder sb, TsGeneratorOutput output)
        {
            base.AppendEnumDefinition(enumModel, sb, TsGeneratorOutput.Enums);
        }
    }
}