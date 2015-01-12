using Edatalia_signplyRT.Common;
using Edatalia_signplyRT.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Edatalia_signplyRT.Ayuntamiento
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class InitPageAuthorizedCitizen : Page
    {
        Citizen AuthorizedCitizen;

        Citizen RequesterCitizen;

        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        /// <summary>
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// NavigationHelper is used on each page to aid in navigation and 
        /// process lifetime management
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }


        public InitPageAuthorizedCitizen()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;

            try
            {
                //get logo from settings
                if (ApplicationData.Current.LocalSettings.Values.ContainsKey("LogoPath"))
                {
                    string strLogo = ApplicationData.Current.LocalSettings.Values["LogoPath"].ToString();
                    imgTittle.Source = ImageFromRelativePath(strLogo);
                }
                if (ApplicationData.Current.LocalSettings.Values.ContainsKey("BackgroundPath"))
                {
                    string strBack = ApplicationData.Current.LocalSettings.Values["BackgroundPath"].ToString();
                    ImageBrush backgroundBrush = new ImageBrush();
                    backgroundBrush.ImageSource = ImageFromRelativePath(strBack);
                    this.gridMain.Background = backgroundBrush;
                }
            }
            catch { }
        }

        private BitmapImage ImageFromRelativePath(string path)
        {
            string tmpe = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\" + path;

            var uri = new Uri(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\" + path);
            BitmapImage result = new BitmapImage();
            result.UriSource = uri;
            return result;
        }

        /// <summary>
        /// Populates the page with content passed during navigation. Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session. The state will be null the first time a page is visited.</param>
        private void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void navigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// 
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="GridCS.Common.NavigationHelper.LoadState"/>
        /// and <see cref="GridCS.Common.NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ActivityLog log = new ActivityLog() { ActivityLogDescription = "InitPageAuthorizedCitizen", AppGuid = App.appID, ActivityLogParams = "OnNavigatedTo" };
            App.PostActivityLog(log);

            List<object> param = (List<object>)e.Parameter;

            RequesterCitizen = (Citizen)param[0];
          
            navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private async void searchBoxAuthorized_QuerySubmitted(SearchBox sender, SearchBoxQuerySubmittedEventArgs args)
        {
            tblNotCitizenAuthorized.Text = "";

            if ((args.QueryText == string.Empty) || (args.QueryText == "") || (!IsValidDNI(args.QueryText)))
            {
                ResourceLoader rloader = new ResourceLoader();
                string strError = rloader.GetString("strError");
                string strDNIFormatErrormsg = rloader.GetString("strDNIFormatErrormsg");
                var msgDialog = new MessageDialog(strDNIFormatErrormsg, strError);
                //MessageDialog msgDialog = new MessageDialog("Introduce un formato de DNI correcto, incluyendo letra en mayuscula", "Error");
                await msgDialog.ShowAsync();
            }
            else
            {

                try
                {
                    AuthorizedCitizen = await GetCitizen(args);
                }
                catch (Exception ex)
                { }

                if (AuthorizedCitizen == null)
                {
                    AuthorizedCitizen = new Citizen();
                    AuthorizedCitizen.DNI = args.QueryText;
                    ResourceLoader rsLoader = new ResourceLoader();
                    string strNoCitizen =  rsLoader.GetString("tblNotCitizenAuthorized");
                    tblNotCitizenAuthorized.Text = strNoCitizen;
                    tblNotCitizenAuthorized.Visibility = Windows.UI.Xaml.Visibility.Visible;
                   
                }
                ucCitizenDetailsAuthorized.DataContext = AuthorizedCitizen;


                ucCitizenDetailsAuthorized.Visibility = Windows.UI.Xaml.Visibility.Visible;
                appbtnSaveClient.IsEnabled = true;
            }

          
        }

        private async Task<Citizen> GetCitizen(SearchBoxQuerySubmittedEventArgs args)
        {
            Citizen citizen;

          
                    try
                    {
                        var res = await App.httpClient.GetAsync(App.uri + "api/Citizens?dni=" + args.QueryText);
                        res.EnsureSuccessStatusCode();
                        // //todo check if content is available
                        var jsonString = await res.Content.ReadAsStringAsync();
                        citizen = JsonConvert.DeserializeObject<Citizen>(jsonString);
                        return citizen;
                    }
                    catch (Exception ex)
                    {
                        return null;
                    }
               
            return null;
        }

        private bool IsValidDNI(string dni)
        {
            string pattern = "^[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][A-Z]$";

            Match DNIMatch = Regex.Match(dni, pattern);

            if (DNIMatch.Success) return true;
            else return false;
        }

        private async void AppBarButtonSaveClient_Click(object sender, RoutedEventArgs e)
        {
            Citizen selected = (Citizen)ucCitizenDetailsAuthorized.DataContext;

            if (await ValidateCitizenData(selected))
            {
                try
                {
                    string jsonClient = JsonConvert.SerializeObject(selected);
                    var content = new StringContent(jsonClient, System.Text.Encoding.UTF8, "application/json");

                    var res = await App.httpClient.PostAsync(App.uri + "api/Citizens", content);
                    res.EnsureSuccessStatusCode();

                    Frame rootFrame = Window.Current.Content as Frame;

                    rootFrame.Navigate(typeof(FillDataPage), new List<object> { RequesterCitizen, selected});
                }
                catch (Exception ex)//show connection error to user
                {
                    string msg = ex.ToString();
                    ResourceLoader rloader = new ResourceLoader();
                    string strError = rloader.GetString("strError");
                    string strDBConnectionErrormsg = rloader.GetString("strDBConnectionErrormsg");
                    var msgDialog = new MessageDialog(strDBConnectionErrormsg, strError);
                   // Windows.UI.Popups.MessageDialog msgDialog = new MessageDialog("No se pudo establecer conexión con la base de datos ", "Error");
                    msgDialog.ShowAsync();
                }
            }
            else
            {

            }
        }

        private async Task<bool> ValidateCitizenData(Citizen citizen)
        {
            bool valid = true;

            string str = "Rellene los siguientes campos con datos válidos:\n";

            //if (client.DNI == null || !IsValidDNI(client.DNI)) { str = str + "DNI\n"; valid = false; }
            //if (client.Name == string.Empty || client.Name == null) { str = str + "Nombre\n"; valid = false; }
            //if (client.FirstName == string.Empty || client.FirstName == null) { str = str + "Primer apellido\n"; valid = false; }
            ////  if (client.BornDate == null) { str = str + "Fecha de nacimiento\n"; valid = false; }
            //if (client.SecondName == string.Empty || client.SecondName == null) { str = str + "Segundo apellido\n"; valid = false; }
            //if (client.Telephone != null && !IsValidoPhone(client.Telephone)) { str = str + "Teléfono\n"; valid = false; }
            //if (client.Mail != null && !IsValidEmail(client.Mail)) { str = str + "eMail\n"; valid = false; }

            if (!valid)
            {
              //  MessageDialog msgDialog = new MessageDialog(str, "Atención");
                ResourceLoader rloader = new ResourceLoader();
                string strWarning = rloader.GetString("strWarning");
                var msgDialog = new MessageDialog(str, strWarning);
                await msgDialog.ShowAsync();
            }
            return valid;
        }

    }
}
