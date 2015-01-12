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

namespace Edatalia_signplyRT
{
    public sealed partial class ClientDetailsUserControl : UserControl
    {
        public ClientDetailsUserControl()
        {
            this.InitializeComponent();
        }

        private async void btSaveClient_Click(object sender, RoutedEventArgs e)
        {
            Client selected = (Client)this.DataContext;

            var content = new MultipartFormDataContent();
            string jsonClient = JsonConvert.SerializeObject(selected);
            content.Add(new StringContent(jsonClient));

            var res = await App.httpClient.PostAsync(App.uri + "api/Clients", content);
            res.EnsureSuccessStatusCode();
        }

        //private void btNext_Click(object sender, RoutedEventArgs e)
        //{
        //    Frame rootFrame = Window.Current.Content as Frame;

        //    rootFrame.Navigate(typeof(ServiceContractPage), null);
        //}
    }
}
