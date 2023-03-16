using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace DaoLang.Avalonia.Sample
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();

            Localization.LanguageChanged += LanguageDemo_LanguageChanged;
            Localization.Init();
        }

        private void LanguageDemo_LanguageChanged(LanguageEventArgs args)
        {
            // Ìæ»»ÓïÑÔ×ÊÔ´
            Application.Current?.Resources.MergedDictionaries.Clear();
            ResourceDictionary dictionary = new();
            foreach (var item in args.Dictionary)
            {
                dictionary.Add(item.Key, item.Value);
            }

            Application.Current?.Resources.MergedDictionaries.Add(dictionary);
        }
    }
}
