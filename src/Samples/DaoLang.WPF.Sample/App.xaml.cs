using System.Windows;

namespace DaoLang.Sample
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Localization.LanguageChanged += LanguageDemo_LanguageChanged;
            Localization.Init();
        }

        private void LanguageDemo_LanguageChanged(LanguageEventArgs args)
        {
            // 替换语言资源
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(args.ResourceDictionary);
        }
    }
}
