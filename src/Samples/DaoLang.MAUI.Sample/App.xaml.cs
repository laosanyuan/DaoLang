using DaoLang.Shared.Enums;

namespace DaoLang.MAUI.Sample
{
    public partial class App : Application
    {
        private ResourceDictionary? _localizationDictionary;

        public App()
        {
            Localization.LanguageChanged += LanguageDemo_LanguageChanged;
            Localization.Init();
            InitializeComponent();

            Localization.SetLanguage(LanguageType.ZH_TW);
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }

        private void LanguageDemo_LanguageChanged(LanguageEventArgs args)
        {
            if (Current?.Resources is null || args.ResourceDictionary is null)
            {
                return;
            }

            var dictionaries = Current.Resources.MergedDictionaries;

            if (_localizationDictionary is not null && dictionaries.Contains(_localizationDictionary))
            {
                dictionaries.Remove(_localizationDictionary);
            }

            _localizationDictionary = args.ResourceDictionary;
            dictionaries.Add(args.ResourceDictionary);
        }
    }
}
