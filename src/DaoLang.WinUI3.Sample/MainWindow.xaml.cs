using DaoLang.Shared.Enums;
using Microsoft.UI.Xaml;

namespace DaoLang.WinUI3.Sample
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Localization.SetLanguage(LanguageType.ZH_CN);
        }

        private void Button_English_Click(object sender, RoutedEventArgs e)
        {
            Localization.SetLanguage(LanguageType.EN_US);
        }

        private void Button_TW_Click(object sender, RoutedEventArgs e)
        {
            Localization.SetLanguage(LanguageType.ZH_TW);
        }

        private void Button_Arab_Click(object sender, RoutedEventArgs e)
        {
            Localization.SetLanguage(LanguageType.AR_SA);
        }

        private void Button_Korean_Click(object sender, RoutedEventArgs e)
        {
            Localization.SetLanguage(LanguageType.KO_KR);
        }

        private void Button_German_Click(object sender, RoutedEventArgs e)
        {
            Localization.SetLanguage(LanguageType.DE_DE);
        }

        private void Button_Japanese_Click(object sender, RoutedEventArgs e)
        {
            Localization.SetLanguage(LanguageType.JA_JP);
        }
    }
}
