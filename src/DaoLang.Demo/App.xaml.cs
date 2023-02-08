using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace DaoLang.Demo
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

        private static void LanguageDemo_LanguageChanged(LanguageEventArgs args)
        {
            // 替换语言资源
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(args.ResourceDictionary);
        }
    }
}
