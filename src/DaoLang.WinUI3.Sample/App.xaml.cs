// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using DaoLang.Base;
using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DaoLang.WinUI3.Sample
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            Localization.LanguageChanged += LanguageDemo_LanguageChanged;
            Localization.Init();

            m_window = new MainWindow();
            m_window.Activate();
        }

        private static void LanguageDemo_LanguageChanged(LanguageEventArgs args)
        {
            // WinUI 3 中不支持像WPF中DynamicResource那样的动态绑定方式，所以需要通过ObservableString来实现实时更新
            foreach (var item in args.ResourceDictionary)
            {
                var key = item.Key as string;
                var value = item.Value as string;
                if (Application.Current.Resources.ContainsKey(key))
                {
                    (Application.Current.Resources[key] as ObservableString).Value = value;
                }
                else
                {
                    Application.Current.Resources.Add(key, new ObservableString(value));
                }
            }
        }

        private Window m_window;
    }
}
