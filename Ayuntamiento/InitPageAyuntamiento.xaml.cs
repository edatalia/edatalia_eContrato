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
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Edatalia_signplyRT.Ayuntamiento
{
    
    public sealed partial class InitPageAyuntamiento : Page
    {
        Citizen QueryingCitizen;
       
        StorageFile modelFile;

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


        public InitPageAyuntamiento()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;

            Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

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

        private BitmapImage ImageFromRelativePath(FrameworkElement parent, string path)
        {
            var uri = new Uri(parent.BaseUri, path);
            BitmapImage bmp = new BitmapImage();
            bmp.UriSource = uri;
            return bmp;
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

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            ActivityLog log = new ActivityLog() { ActivityLogDescription = "InitPageAyuntamiento", AppGuid = App.appID, ActivityLogParams = "OnNavigatedTo" };
            App.PostActivityLog(log);

            //ApplicationData.Current.LocalSettings.Values["ApiKey"] = null;
            navigationHelper.OnNavigatedTo(e);

            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey("ApiKey"))
            {
               // var messageBox = new Windows.UI.Popups.MessageDialog("Configure la seccion ApiKey de la app", "Atención");
                ResourceLoader rloader = new ResourceLoader();
                string strWarning = rloader.GetString("strWarning");
                string strApiKeyConf = rloader.GetString("strApiKeyConf");
                var msgDialog = new MessageDialog(strApiKeyConf, strWarning);
                await msgDialog.ShowAsync();
            }

        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion


        private async void searchBox_QuerySubmitted(SearchBox sender, SearchBoxQuerySubmittedEventArgs args)
        {
            tblNotCitizen.Text = "";

           
                if ((args.QueryText == string.Empty) || (args.QueryText == "") || (!IsValidDNI(args.QueryText)))
                {
                   // MessageDialog msgDialog = new MessageDialog("Introduce un formato de DNI correcto, incluyendo letra en mayuscula", "Error");
                    ResourceLoader rloader = new ResourceLoader();
                    string strError = rloader.GetString("strError");
                    string strDNIFormatErrormsg = rloader.GetString("strDNIFormatErrormsg");
                    var msgDialog = new MessageDialog(strDNIFormatErrormsg, strError);
                    await msgDialog.ShowAsync();
                }
                else
                {

                    try
                    {
                        QueryingCitizen = await GetCitizen(args);
                    }
                    catch (Exception ex)
                    { }

                    if (QueryingCitizen == null)
                    {
                        QueryingCitizen = new Citizen();
                        QueryingCitizen.DNI = args.QueryText;
                        ResourceLoader rsLoader = new ResourceLoader();
                        string strNoCitizen = rsLoader.GetString("tblNotCitizenAuthorized");
                        tblNotCitizen.Text = strNoCitizen;
                        tblNotCitizen.Visibility = Windows.UI.Xaml.Visibility.Visible;
                        
                    }
                    ucCitizenDetails.DataContext = QueryingCitizen;

                    ucCitizenDetails.Visibility = Windows.UI.Xaml.Visibility.Visible;
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

        private void btNext_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void AppBarButtonSaveClient_Click(object sender, RoutedEventArgs e)
        {
            Citizen selected = (Citizen)ucCitizenDetails.DataContext;

            if (await ValidateCitizenData(selected))
            {
                try
                {
                    string jsonClient = JsonConvert.SerializeObject(selected);
                    var content = new StringContent(jsonClient, System.Text.Encoding.UTF8, "application/json");

                    var res = await App.httpClient.PostAsync(App.uri + "api/Citizens", content);
                    res.EnsureSuccessStatusCode();

                    Frame rootFrame = Window.Current.Content as Frame;
                    if ((bool)chbAuthorized.IsChecked)
                        rootFrame.Navigate(typeof(InitPageAuthorizedCitizen), new List<object> { selected });
                    else rootFrame.Navigate(typeof(FillDataPage), new List<object> { selected });
                }
                catch (Exception ex)//show connection error to user
                {
                    string msg = ex.ToString();
                   // Windows.UI.Popups.MessageDialog msgDialog = new MessageDialog("No se pudo establecer conexión con la base de datos ", "Error");
                    ResourceLoader rloader = new ResourceLoader();
                    string strError = rloader.GetString("strError");
                    string strDBConnectionErrormsg = rloader.GetString("strDBConnectionErrormsg");
                    var msgDialog = new MessageDialog(strDBConnectionErrormsg, strError);
                    msgDialog.ShowAsync();
                }
            }
            else
            {

            }
        }

        private async Task<bool> ValidateModelData()
        {
            bool valid = true;
            //string msg = "Rellene los siguientes campos con datos válidos\n";

            //if (tbImporte.Text == null || !IsValidImport(tbImporte.Text)) { msg = msg + "Importe"; valid = false; }

            //if (!valid)
            //{
            //    MessageDialog msgDialog = new MessageDialog(msg, "Atención");
            //    await msgDialog.ShowAsync();
            //}

            return valid;
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
                //MessageDialog msgDialog = new MessageDialog(str, "Atención");
                ResourceLoader rloader = new ResourceLoader();
                string strWarning = rloader.GetString("strWarning");
                var msgDialog = new MessageDialog(str, strWarning);
                await msgDialog.ShowAsync();
            }
            return valid;
        }

        private bool IsValidEmail(string emailAddress)
        {
            if (emailAddress == null || emailAddress == string.Empty) return false;

            string pattern = "^[a-zA-Z][\\w\\.-]*[a-zA-Z0-9]@[a-zA-Z0-9][\\w\\.-]*[a-zA-Z0-9]\\.[a-zA-Z][a-zA-Z\\.]*[a-zA-Z]$";

            Match emailAddressMatch = Regex.Match(emailAddress, pattern);

            if (emailAddressMatch.Success) return true;
            else return false;
        }

        private bool IsValidoPhone(string phoneNumber)
        {
            if (phoneNumber == null || phoneNumber == string.Empty) return false;

            string pattern = "^[0-9]*$";

            Match phoneNumberMatch = Regex.Match(phoneNumber, pattern);

            if (phoneNumberMatch.Success) return true;
            else return false;
        }

        private bool IsValidDNI(string dni)
        {
            string pattern = "^[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][A-Z]$";

            Match DNIMatch = Regex.Match(dni, pattern);

            if (DNIMatch.Success) return true;
            else return false;
        }

        private bool IsValidImport(string import)
        {
            string pattern = "^[0-9][0-9]*([.,][0-9]{1,2})?$";

            Match ImportMatch = Regex.Match(import, pattern);

            if (ImportMatch.Success) return true;
            else return false;
        }

        private  void AppBarButtonOpen_Click(object sender, RoutedEventArgs e)
        {
            //try
            //{
            //    Client selected = (Client)ucClientDetails.DataContext;
            //    if (selected != null && (await ValidateClientData(selected)))
            //    {
            //        try
            //        {
            //            // Launching FilePicker
            //            FileOpenPicker openPicker = new FileOpenPicker();
            //            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            //            openPicker.ViewMode = PickerViewMode.List;
            //            openPicker.FileTypeFilter.Add(".docx");

            //            // Creating async operation for PickSingleFileAsync
            //            modelFile = await openPicker.PickSingleFileAsync();
            //            if (modelFile != null)
            //            {
            //                spContractData.Visibility = Visibility.Visible;
            //                ucClientDetails.IsEnabled = false;
            //                searchBox.Visibility = Visibility.Collapsed;
            //                appbtnConfirm.Visibility = Visibility.Visible;
            //            }

            //            tblNotClient.Visibility = Visibility.Collapsed;

            //        }
            //        catch (Exception ex)
            //        { }
            //    }
            //}
            //catch
            //{
            //    //pop up message to select client first!!
            //}
        }

        private  void AppBarButtonConfirm_Click(object sender, RoutedEventArgs e)
        {
            //Client selected = (Client)ucClientDetails.DataContext;

            //if (await ValidateModelData())
            //{

            //    Frame rootFrame = Window.Current.Content as Frame;

            //    rootFrame.Navigate(typeof(ContractConfirmationPage), new List<object> { selectedClient, modelFile, tbImporte.Text, dtPicker.Date.UtcTicks });
            //}
        }

        private void chbAuthorized_Checked(object sender, RoutedEventArgs e)
        {

        }



    }
}
