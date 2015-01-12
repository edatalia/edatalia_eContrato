using Edatalia_signplyRT.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Edatalia_signplyRT.Ayuntamiento
{
    public sealed partial class CitizenDetailsUserControl : UserControl
    {
        public CitizenDetailsUserControl()
        {
            this.InitializeComponent();
        }

        private async void btSaveClient_Click(object sender, RoutedEventArgs e)
        {
            Citizen selected = (Citizen)this.DataContext;

            var content = new MultipartFormDataContent();
            string jsonClient = JsonConvert.SerializeObject(selected);
            content.Add(new StringContent(jsonClient));

            var res = await App.httpClient.PostAsync(App.uri + "api/Citizens", content);
            res.EnsureSuccessStatusCode();
        }

        private void lbCommunicationLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
           if  (((ComboBoxItem)((ComboBox)sender).SelectedItem).Content.ToString() == "Euskera") ((Citizen)this.DataContext).ComLanguage = LanguageEnum.Euskera;
           if (((ComboBoxItem)((ComboBox)sender).SelectedItem).Content.ToString() == "Castellano") ((Citizen)this.DataContext).ComLanguage = LanguageEnum.Castellano;
          
        }
    }
}
