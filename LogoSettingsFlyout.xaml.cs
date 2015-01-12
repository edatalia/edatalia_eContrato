using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
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
    public sealed partial class LogoSettingsFlyout : SettingsFlyout
    {
        public LogoSettingsFlyout()
        {
            this.InitializeComponent();

            //get logo from settings
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("LogoPath"))
            {
                string strLogo = ApplicationData.Current.LocalSettings.Values["LogoPath"].ToString();
                tbLogoPath.Text = strLogo;
            }
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("BackgroundPath"))
            {
                string strBack = ApplicationData.Current.LocalSettings.Values["BackgroundPath"].ToString();
                tbBackgroundPath.Text = strBack;
            }
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("ApiKey"))
            {
                string strApiKey = ApplicationData.Current.LocalSettings.Values["ApiKey"].ToString();
                tbApiKey.Text = strApiKey;
            }

            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("SelectedDemo"))
            {
                string demo = ApplicationData.Current.LocalSettings.Values["SelectedDemo"].ToString();
                if (demo == "Ayuntamiento") rbtAyuntamietno.IsChecked = true;
                else rbtSeguros.IsChecked = true;
            }

        }

        private void tbApiKey_TextChanged(object sender, TextChangedEventArgs e)
        {
            string strApiKey = "";
            string newValue = ((TextBox)sender).Text;
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("ApiKey")) strApiKey = ApplicationData.Current.LocalSettings.Values["ApiKey"].ToString();
            if (newValue != strApiKey)
                ApplicationData.Current.LocalSettings.Values["ApiKey"] = newValue;

        }

        private async void btSelectLogo_Click(object sender, RoutedEventArgs e)
        {
            // Launching FilePicker
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            openPicker.ViewMode = PickerViewMode.List;
            openPicker.FileTypeFilter.Add(".jpg");
            openPicker.FileTypeFilter.Add(".jpeg");
            openPicker.FileTypeFilter.Add(".png");

            // Creating async operation for PickSingleFileAsync
            StorageFile logoFile = await openPicker.PickSingleFileAsync();
            if (logoFile != null)
            {
                //Copy to local data File
                StorageFile copyFile = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync(logoFile.Name, CreationCollisionOption.GenerateUniqueName);
                await logoFile.CopyAndReplaceAsync(copyFile);

                tbLogoPath.Text = copyFile.DisplayName;
                ApplicationData.Current.LocalSettings.Values["LogoPath"] = copyFile.Name;

                tblRestart.Visibility = Visibility.Visible;
            }
        }

        private async void btSelectBackground_Click(object sender, RoutedEventArgs e)
        {
            // Launching FilePicker
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            openPicker.ViewMode = PickerViewMode.List;
            openPicker.FileTypeFilter.Add(".jpg");
            openPicker.FileTypeFilter.Add(".jpeg");
            openPicker.FileTypeFilter.Add(".png");

            // Creating async operation for PickSingleFileAsync
            StorageFile logoFile = await openPicker.PickSingleFileAsync();
            if (logoFile != null)
            {
                //Copy to local data File
                StorageFile copyFile = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync(logoFile.Name, CreationCollisionOption.GenerateUniqueName);
                await logoFile.CopyAndReplaceAsync(copyFile);

                tbLogoPath.Text = copyFile.DisplayName;
                ApplicationData.Current.LocalSettings.Values["BackgroundPath"] = copyFile.Name;

                tblRestartBackground.Visibility = Visibility.Visible;
            }
        }

        private async void RadioButtonAseguradora_Checked(object sender, RoutedEventArgs e)
        {
            if (ApplicationData.Current.LocalSettings.Values["SelectedDemo"].ToString() != "Aseguradora")
            {
                ApplicationData.Current.LocalSettings.Values["SelectedDemo"] = "Aseguradora";

               // MessageDialog msgDialog = new MessageDialog("Reinicie la aplicación para cambiar  de demo", "Atención");
                ResourceLoader rloader = new ResourceLoader();
                string strWarning = rloader.GetString("strWarning");
                string strRestartChangeDemo = rloader.GetString("strRestartChangeDemo");
                var msgDialog = new MessageDialog(strRestartChangeDemo, strWarning);
                await msgDialog.ShowAsync();
                App.Current.Exit();
            }
        }

        private async void RadioButtonAyuntamiento_Checked(object sender, RoutedEventArgs e)
        {
            if (ApplicationData.Current.LocalSettings.Values["SelectedDemo"].ToString() != "Ayuntamiento")
            {
                ApplicationData.Current.LocalSettings.Values["SelectedDemo"] = "Ayuntamiento";

               // MessageDialog msgDialog = new MessageDialog("Reinicie la aplicación para cambiar  de demo", "Atención");
                ResourceLoader rloader = new ResourceLoader();
                string strWarning = rloader.GetString("strWarning");
                string strRestartChangeDemo = rloader.GetString("strRestartChangeDemo");
                var msgDialog = new MessageDialog(strRestartChangeDemo, strWarning);
                await msgDialog.ShowAsync();
                App.Current.Exit();
            }
        }
    }
}
