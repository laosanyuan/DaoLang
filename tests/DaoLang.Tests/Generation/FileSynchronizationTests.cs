extern alias Generator;

using System;
using System.Diagnostics;
using System.IO;
using Xunit;
using FileSourceGenerator = Generator::DaoLang.SourceGeneration.FileSourceGenerator;

namespace DaoLang.Tests.Generation
{
    public class FileSynchronizationTests
    {
        [Fact]
        public void RemovingSecondaryLanguageDeletesResourceFileAndProjectOutputMarker()
        {
            using var workspace = new TemporaryProject();
            var sourcePath = Path.Combine(workspace.Root, "Localization.cs");
            var projectPath = Path.Combine(workspace.Root, "GeneratorTests.csproj");
            var nugetConfigPath = Path.Combine(workspace.Root, "NuGet.Config");

            File.WriteAllText(projectPath, workspace.CreateProjectFile());
            File.WriteAllText(nugetConfigPath, workspace.CreateNuGetConfig());
            File.WriteAllText(sourcePath, InitialSource);

            var initialBuild = BuildProject(projectPath, workspace.Root, nugetConfigPath);
            Assert.True(initialBuild.ExitCode == 0, initialBuild.Output);

            var removedLanguageFile = Path.Combine(workspace.Root, "Lang", "Language.de_de.xml");
            Assert.True(File.Exists(removedLanguageFile));
            Assert.Contains("Language.de_de.xml", File.ReadAllText(projectPath));
            Assert.Contains("DaoLangCleanupGeneratedResources", File.ReadAllText(projectPath));

            File.WriteAllText(sourcePath, UpdatedSource);

            var updatedBuild = BuildProject(projectPath, workspace.Root, nugetConfigPath);
            Assert.True(updatedBuild.ExitCode == 0, updatedBuild.Output);
            Assert.False(File.Exists(removedLanguageFile));

            var projectContent = File.ReadAllText(projectPath);
            Assert.DoesNotContain("Language.de_de.xml", projectContent);
            Assert.Contains("Language.zh_cn.xml", projectContent);
        }

        private static (int ExitCode, string Output) BuildProject(string projectPath, string workingDirectory, string nugetConfigPath)
        {
            var startInfo = new ProcessStartInfo("dotnet", $"build \"{projectPath}\" --configfile \"{nugetConfigPath}\" -nologo -v minimal -p:UseSharedCompilation=false")
            {
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            startInfo.Environment["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";
            startInfo.Environment["DOTNET_CLI_HOME"] = Path.Combine(workingDirectory, ".dotnet");

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet build.");
            var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
            process.WaitForExit();
            return (process.ExitCode, output);
        }

        private sealed class TemporaryProject : IDisposable
        {
            public TemporaryProject()
            {
                Root = Path.Combine(Path.GetTempPath(), "DaoLang.FileSyncTests", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Root);
            }

            public string Root { get; }

            public string CreateProjectFile()
            {
                var testAssembly = typeof(FileSynchronizationTests).Assembly.Location;
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

            public string CreateNuGetConfig()
            {
                return """
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
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

        private const string InitialSource = """
using DaoLang.Attributes;
using DaoLang.Shared.Enums;

namespace Demo;

[MainLanguage("Lang", "Language", LanguageType.EN_US, FileGenerationType.OutputDirectory)]
[SecondaryLanguage(LanguageType.ZH_CN)]
[SecondaryLanguage(LanguageType.DE_DE)]
public partial class Localization
{
    [Entry("hello")]
    private static string _hello = "Hello";
}
""";

        private const string UpdatedSource = """
using DaoLang.Attributes;
using DaoLang.Shared.Enums;

namespace Demo;

[MainLanguage("Lang", "Language", LanguageType.EN_US, FileGenerationType.OutputDirectory)]
[SecondaryLanguage(LanguageType.ZH_CN)]
public partial class Localization
{
    [Entry("hello")]
    private static string _hello = "Hello";
}
""";
    }
}
