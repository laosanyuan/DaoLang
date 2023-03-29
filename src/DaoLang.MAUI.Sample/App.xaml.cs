using DaoLang.Shared.Enums;

namespace DaoLang.MAUI.Sample
{
    public partial class App : Application
    {
        public App()
        {
            Localization.LanguageChanged += LanguageDemo_LanguageChanged;
            Localization.Init();
            InitializeComponent();

            MainPage = new AppShell();

            Localization.SetLanguage(LanguageType.ZH_TW);
        }

        private void LanguageDemo_LanguageChanged(LanguageEventArgs args)
        {
            // 替换语言资源
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(args.ResourceDictionary);
        }
    }
}