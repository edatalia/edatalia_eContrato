using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Settings Flyout item template is documented at http://go.microsoft.com/fwlink/?LinkId=273769

namespace Edatalia_signplyRT
{
    public sealed partial class LanguageSettingsFlyout : SettingsFlyout
    {
        public LanguageSettingsFlyout()
        {
            this.InitializeComponent();

            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("Language"))
            {
                string language = (string)ApplicationData.Current.LocalSettings.Values["Language"];
                if (language == "Español")
                {
                    Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "es-ES";
                    rdBt_Espanol.IsChecked = true;
                }
                else
                {
                    if (language == "English")
                    {
                        Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "en-US";
                        rdBt_English.IsChecked = true;
                    }
                    if (language == "Euskera")
                    {
                        Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "eu-ES";
                        rdBt_Euskera.IsChecked = true;
                    }
                }
               
            }
           
        }

        
        private void HandleCheckCastellano(object sender, RoutedEventArgs e)
        {
            RadioButton rb = sender as RadioButton;

            string newSelection = rb.Content.ToString();
            string oldSelection = ApplicationData.Current.LocalSettings.Values["Language"].ToString();

            if (newSelection != oldSelection)
            {

                ApplicationData.Current.LocalSettings.Values["Language"] = newSelection;

                 Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "es-ES";
            
            }
        }

        private void HandleCheckIngles(object sender, RoutedEventArgs e)
        {
            RadioButton rb = sender as RadioButton;

            string newSelection = rb.Content.ToString();
            string oldSelection = ApplicationData.Current.LocalSettings.Values["Language"].ToString();

            if (newSelection != oldSelection)
            {

                ApplicationData.Current.LocalSettings.Values["Language"] = newSelection;

                Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "en-US";

            }
        }

        private void HandleCheckEuskera(object sender, RoutedEventArgs e)
        {
            RadioButton rb = sender as RadioButton;

            string newSelection = rb.Content.ToString();
            string oldSelection = ApplicationData.Current.LocalSettings.Values["Language"].ToString();

            if (newSelection != oldSelection)
            {

                ApplicationData.Current.LocalSettings.Values["Language"] = newSelection;

                Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "eu-ES";

            }
        }
    }
}
