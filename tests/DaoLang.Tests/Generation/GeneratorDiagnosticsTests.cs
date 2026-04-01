extern alias Generator;

using System;
using System.Diagnostics;
using System.IO;
using Xunit;
using FileSourceGenerator = Generator::DaoLang.SourceGeneration.FileSourceGenerator;

namespace DaoLang.Tests.Generation
{
    public class GeneratorDiagnosticsTests
    {
        [Fact]
        public void ReportsDiagnosticWhenLocalizationClassIsNotPartial()
        {
            var output = BuildProject("""
using DaoLang.Attributes;
using DaoLang.Shared.Enums;

namespace Demo;

[MainLanguage("Lang", "Language", LanguageType.EN_US)]
public class Localization
{
    [Entry("hello")]
    private static string _hello = "Hello";
}
""");

            Assert.Contains("DAL001", output);
        }

        [Fact]
        public void ReportsDiagnosticWhenEntryFieldIsNotPrivateStaticString()
        {
            var output = BuildProject("""
using DaoLang.Attributes;
using DaoLang.Shared.Enums;

namespace Demo;

[MainLanguage("Lang", "Language", LanguageType.EN_US)]
public partial class Localization
{
    [Entry("hello")]
    public static int _hello = 1;
}
""");

            Assert.Contains("DAL002", output);
        }

        [Fact]
        public void ReportsDiagnosticWhenEntryFieldInitializerIsNotStringLiteral()
        {
            var output = BuildProject("""
using DaoLang.Attributes;
using DaoLang.Shared.Enums;

namespace Demo;

[MainLanguage("Lang", "Language", LanguageType.EN_US)]
public partial class Localization
{
    [Entry("hello")]
    private static string _hello = nameof(Localization);
}
""");

            Assert.Contains("DAL003", output);
        }

        private static string BuildProject(string source)
        {
            using var workspace = new TemporaryProject();
            var sourcePath = Path.Combine(workspace.Root, "Localization.cs");
            var projectPath = Path.Combine(workspace.Root, "GeneratorTests.csproj");
            File.WriteAllText(sourcePath, source);
            File.WriteAllText(projectPath, workspace.CreateProjectFile());

            var startInfo = new ProcessStartInfo("dotnet", $"build \"{projectPath}\" -nologo -v minimal")
            {
                WorkingDirectory = workspace.Root,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet build.");
            var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
            process.WaitForExit();
            return output;
        }

        private sealed class TemporaryProject : IDisposable
        {
            public TemporaryProject()
            {
                Root = Path.Combine(Path.GetTempPath(), "DaoLang.GeneratorTests", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Root);
            }

            public string Root { get; }

            public string CreateProjectFile()
            {
                var testAssembly = typeof(GeneratorDiagnosticsTests).Assembly.Location;
                var generatorAssembly = typeof(FileSourceGenerator).Assembly.Location;

                return $"""
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="DaoLang.Tests">
      <HintPath>{testAssembly}</HintPath>
    </Reference>
    <Analyzer Include="{generatorAssembly}" />
  </ItemGroup>
</Project>
""";
            }

            public void Dispose()
            {
                if (Directory.Exists(Root))
                {
                    Directory.Delete(Root, true);
                }
            }
        }
    }
}
