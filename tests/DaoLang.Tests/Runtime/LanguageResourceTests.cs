using DaoLang.Shared.Enums;
using DaoLang.Shared.Models;
using System;
using System.IO;
using System.Xml.Serialization;
using Xunit;

namespace DaoLang.Tests.Runtime
{
    public class LanguageResourceTests
    {
        [Fact]
        public void SetLanguage_ReturnsTrueAndFallsBackToMainLanguageContent()
        {
            using var workspace = new TemporaryWorkspace();
            WriteLanguageFile(
                workspace.Root,
                LanguageType.EN_US,
                ("Hello", "Hello"),
                ("Goodbye", "Goodbye"));
            WriteLanguageFile(
                workspace.Root,
                LanguageType.ZH_CN,
                ("Hello", string.Empty),
                ("Goodbye", "再见"));

            using var currentDirectory = workspace.UseAsCurrentDirectory();
            TestLocalization.Configure("Lang");

            var result = TestLocalization.SetLanguage(LanguageType.ZH_CN);

            Assert.True(result);
            Assert.Equal("Hello", TestLocalization.Hello);
            Assert.Equal("再见", TestLocalization.Goodbye);
            Assert.Equal("Hello", TestLocalization.LastDictionaryValue("Hello"));
            Assert.Equal("再见", TestLocalization.LastDictionaryValue("Goodbye"));
        }

        [Fact]
        public void SetLanguage_FallsBackToMainLanguageWhenSecondaryLanguageIsNotConfigured()
        {
            using var workspace = new TemporaryWorkspace();
            WriteLanguageFile(
                workspace.Root,
                LanguageType.EN_US,
                ("Hello", "Hello"),
                ("Goodbye", "Goodbye"));

            using var currentDirectory = workspace.UseAsCurrentDirectory();
            TestLocalization.Configure("Lang");

            var result = TestLocalization.SetLanguage(LanguageType.DE_DE);

            Assert.True(result);
            Assert.Equal("Hello", TestLocalization.Hello);
            Assert.Equal("Goodbye", TestLocalization.Goodbye);
        }

        private static void WriteLanguageFile(string root, LanguageType languageType, params (string Key, string Content)[] items)
        {
            var directory = Path.Combine(root, "Lang");
            Directory.CreateDirectory(directory);

            var language = new Language
            {
                LanguageType = languageType,
                IsMainLanguage = languageType == LanguageType.EN_US
            };

            foreach (var item in items)
            {
                language.Add(new LanguageItem { Key = item.Key, Content = item.Content });
            }

            var serializer = new XmlSerializer(typeof(Language));
            using var stream = File.Create(Path.Combine(directory, $"Language.{languageType.ToString().ToLowerInvariant()}.xml"));
            serializer.Serialize(stream, language);
        }

        private sealed class TemporaryWorkspace : IDisposable
        {
            public TemporaryWorkspace()
            {
                Root = Path.Combine(Path.GetTempPath(), "DaoLang.Tests", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Root);
            }

            public string Root { get; }

            public IDisposable UseAsCurrentDirectory() => new CurrentDirectoryScope(Root);

            public void Dispose()
            {
                if (Directory.Exists(Root))
                {
                    Directory.Delete(Root, true);
                }
            }
        }

        private sealed class CurrentDirectoryScope : IDisposable
        {
            private readonly string _originalDirectory = Environment.CurrentDirectory;

            public CurrentDirectoryScope(string path)
            {
                Environment.CurrentDirectory = path;
            }

            public void Dispose()
            {
                Environment.CurrentDirectory = _originalDirectory;
            }
        }

        private sealed class TestLocalization : LanguageResource
        {
            private static string _hello = "Hello";
            private static string _goodbye = "Goodbye";

            public static string Hello => _hello;

            public static string Goodbye => _goodbye;

            private static System.Windows.ResourceDictionary? _lastDictionary;

            public static void Configure(string folder)
            {
                _hello = "Hello";
                _goodbye = "Goodbye";
                _lastDictionary = null;
                SourceType = typeof(TestLocalization);
                Folder = folder;
                FileFlag = "Language";
                MainLanguage = LanguageType.EN_US;
                SecondaryLanguages = new[] { LanguageType.ZH_CN };
                MainSource = null;
                FileGenerationType = FileGenerationType.OutputDirectory;
                LanguageChanged += args => _lastDictionary = args.ResourceDictionary;
            }

            public static string? LastDictionaryValue(string key)
            {
                return _lastDictionary?[key] as string;
            }
        }
    }
}
