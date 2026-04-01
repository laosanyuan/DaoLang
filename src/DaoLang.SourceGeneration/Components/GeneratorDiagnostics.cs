using Microsoft.CodeAnalysis;

namespace DaoLang.SourceGenerators.Components
{
    internal static class GeneratorDiagnostics
    {
        public static readonly DiagnosticDescriptor MainLanguageClassMustBePartial =
            new(
                id: "DAL001",
                title: "Localization class must be partial",
                messageFormat: "Class '{0}' must be declared partial when using DaoLang language attributes.",
                category: "DaoLang",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor EntryFieldMustBePrivateStaticString =
            new(
                id: "DAL002",
                title: "Entry field declaration is invalid",
                messageFormat: "Field '{0}' must be declared as a single private static string field to use [Entry].",
                category: "DaoLang",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor EntryFieldMustUseStringLiteralInitializer =
            new(
                id: "DAL003",
                title: "Entry field initializer must be a string literal",
                messageFormat: "Field '{0}' must use a string literal initializer so DaoLang can generate the main language resource file.",
                category: "DaoLang",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);
    }
}
