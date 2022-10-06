using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace DaoLang.SourceGenerators
{
    [Generator]
    public sealed class LanguageResourceSourceGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            //if (context.SyntaxContextReceiver is LanguageReceiver receiver)
            {
                var mainMethod = context.Compilation.GetEntryPoint(context.CancellationToken);

                string source = $@"
using System;

namespace {context.Compilation.AssemblyName}
{{
    public static class {mainMethod.ContainingType.Name}
    {{
        static void HelloFrom(string name)
        {{
            Console.WriteLine($""Generator says: Hi sss from '{{name}}'"");
        }}
    }}
}}
";
                context.AddSource("GeneratedSourceTest.g.cs", source);
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }
}
