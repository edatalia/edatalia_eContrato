using Edatalia_signplyRT.Common;
using Edatalia_signplyRT.Model;
using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
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

namespace Edatalia_signplyRT
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class ContractConfirmationPage : Page
    {

        private Client client;
        private StorageFile selModel;
        private List<object> modelParams;
        private DateTime dtStartDate;

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


        public ContractConfirmationPage()
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
                //get background from settings
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

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ActivityLog log = new ActivityLog() { ActivityLogDescription = "ContractConfirmationPage", AppGuid = App.appID, ActivityLogParams="OnNavigatedTo" };
            App.PostActivityLog(log);

            List<object> param = (List<object>)e.Parameter;

            client = (Client)param[0];
            selModel = (StorageFile)param[1];
            modelParams = new List<object>();
            for (int i = 2; i < param.Count; i++)
            {
                modelParams.Add(param[i]);
            }

            //fill client and contract data
            tbClientDNI.Text = client.DNI;
            tbClientName.Text = client.Name;
            tbClientFirstName.Text = client.FirstName;
            tbClientSecondName.Text = client.SecondName;
            tbService.Text = selModel.DisplayName;
            tbImporte.Text = modelParams[0].ToString();

            dtStartDate = new DateTime((long)modelParams[1]);
           // DateTimeOffset dtOffset = new DateTimeOffset(dt);
            //dtPickerInitDate.Date = dtOffset;
            dtPickerInitDate.Text = dtStartDate.ToString("dd/MM/yyyy");

            navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void chbConfidentialTems_Checked(object sender, RoutedEventArgs e)
        {
            appbtnGenerateContractClient.Visibility = Windows.UI.Xaml.Visibility.Visible;
            bottomAppBar.IsSticky = true;
            bottomAppBar.IsOpen = true;
        }


        private async void AppBarButtonGenerateContract_Click(object sender, RoutedEventArgs e)
        {
            string audioPhrase = "";

            Guid contractID = Guid.NewGuid();
            var appFolder = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFolderAsync(contractID.ToString());

            prgGeneratingContract.Visibility = Windows.UI.Xaml.Visibility.Visible;
            prgGeneratingContract.IsActive = true;

            appbtnGenerateContractClient.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            bottomAppBar.IsSticky = false;
            bottomAppBar.IsOpen = false;


            //generate docx from model and data
            WordDocument document = new WordDocument();
            Stream inputStream = await selModel.OpenStreamForReadAsync();
            await document.OpenAsync(inputStream, FormatType.Docx);
            inputStream.Dispose();

            var bookmarksNavigator = new BookmarksNavigator(document);
            try
            {
                bookmarksNavigator.MoveToBookmark("Name");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(client.Name, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("FirstName");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(client.FirstName, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("SecondName");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(client.SecondName, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("Address");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(client.Address, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("DNI");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(client.DNI, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("Importe");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(modelParams[0].ToString() + "€", true);
            }
            catch { }
            try
            {
                DateTime dt = new DateTime((long)modelParams[1]);
                bookmarksNavigator.MoveToBookmark("InitDate");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(dt.ToString("dd MM yyyy"), true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("Check1");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent("X", true);  
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("Check2");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent("", true);
            }
            catch { }


            //recogemos la frase a repetir en la grabación de audio

            try
            {
                bookmarksNavigator.MoveToBookmark("Name1");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(client.Name, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("FirstName1");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(client.FirstName, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("SecondName1");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(client.SecondName, true);
            }
            catch { }
        
            try
            {
                bookmarksNavigator.MoveToBookmark("DNI1");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(client.DNI, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("Importe1");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(modelParams[0].ToString() + "€", true);
            }
            catch { }
            try
            {
                DateTime dt = new DateTime((long)modelParams[1]);
                bookmarksNavigator.MoveToBookmark("InitDate1");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(dt.ToString("dd MM yyyy"), true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("ContractName");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(selModel.DisplayName, true);
            }
            catch { }

            try
            {
                bookmarksNavigator.MoveToBookmark("AudioPhrase");
                //bookmarksNavigator.ReplaceBookmarkContent(selModel.DisplayName, true);
                TextBodyPart prueba = bookmarksNavigator.GetBookmarkContent();
                audioPhrase = bookmarksNavigator.CurrentBookmarkItem.OwnerParagraph.Text;
                bookmarksNavigator.ReplaceBookmarkContent("", true);
            }
            catch { }

         




            MemoryStream ms = new MemoryStream();
            await document.SaveAsync(ms, FormatType.Docx);

            //Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            //C:\Users\alarranaga\AppData\Local\Packages\3022db98-afb6-4c9c-9153-21d964781953_a6xmkfdj4czm2\LocalState
            StorageFile tempSaveFile = await appFolder.CreateFileAsync(contractID.ToString() + ".docx", CreationCollisionOption.ReplaceExisting);

            if (tempSaveFile != null)
            {
                using (IRandomAccessStream zipStream = await tempSaveFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    // Write compressed data from memory to file
                    using (Stream outstream = zipStream.AsStreamForWrite())
                    {
                        byte[] buffer = ms.ToArray();
                        outstream.Write(buffer, 0, buffer.Length);
                        outstream.Flush();
                    }
                }
            }

            ms.Dispose();

            //send to create pdf file
            StorageFile pdfConvertedFile = await Upload(tempSaveFile, appFolder,contractID);

            if (pdfConvertedFile != null)
            {
                //load pdf file MainPage
                Frame rootFrame = Window.Current.Content as Frame;
             //   new List<object> { selectedClient, modelFile, tbImporte.Text, dtPicker.Date.UtcTicks }
               // rootFrame.Navigate(typeof(MainPage), pdfConvertedFile);


                ContractXMLData contractData = new ContractXMLData();
                contractData.ClientRequester = client;
                contractData.ContractGuid = contractID;
                contractData.TipoContrato = selModel.DisplayName;
                contractData.Import = Convert.ToDouble(tbImporte.Text);
                contractData.StartDate = dtStartDate;


                XmlSerializer xsSubmit = new XmlSerializer(typeof(ContractXMLData));
                
                StringWriter sww = new StringWriter();
                XmlWriter writer = XmlWriter.Create(sww);
                xsSubmit.Serialize(writer, contractData);

               
                var xml = sww.ToString();

                SaveXmlFile(xml, appFolder);

                rootFrame.Navigate(typeof(MainPage), new List<object> { pdfConvertedFile, client, selModel.DisplayName, tbImporte.Text, dtStartDate, audioPhrase });
            }
            else 
            {
                // msgDialog = new MessageDialog("No se ha podido generar el pdf, intentelo de nuevo reiniciando la aplicación", "Error");
                ResourceLoader rloader = new ResourceLoader();
                string strError = rloader.GetString("strError");
                string strPDFCreationErrormsg = rloader.GetString("strPDFCreationErrormsg");
                var msgDialog = new MessageDialog(strPDFCreationErrormsg, strError);
                await msgDialog.ShowAsync();
                App.Current.Exit();
            }
        }

        private async void SaveXmlFile(string xmlContent, StorageFolder appFolder)
        {
            try
            {
                StorageFile file = await appFolder.CreateFileAsync("OnPremise.xml", CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(file, xmlContent);
            }
            catch { }
        }

        private async Task<StorageFile> Upload(StorageFile file, StorageFolder folder, Guid fileName)
        {
            string strApiKey = ApplicationData.Current.LocalSettings.Values["ApiKey"].ToString();

            try
            {    
                var randomAccessStream = await file.OpenReadAsync();
                Stream stream = randomAccessStream.AsStreamForRead();

                HttpContent fileStreamContent = new StreamContent(stream);

                ActivityLog log = new ActivityLog() { ActivityLogDescription = "ContractConfirmationPage", AppGuid = App.appID, ActivityLogParams = "Generating pdf, apiKey:" + strApiKey };
                App.PostActivityLog(log);

                using (var client = new HttpClient())
                using (var formData = new MultipartFormDataContent())
                {

                    formData.Add(new StringContent("pdf"), "OutputFormat");
                    formData.Add(new StringContent("Mypdf"), "OutputFileName");
                    //formData.Add(new StringContent("659631010"), "ApiKey");
                    formData.Add(new StringContent(strApiKey), "ApiKey");
                    formData.Add(fileStreamContent, "File", file.Name);

                    var response = await client.PostAsync("http://do.convertapi.com/word2pdf", formData);
                    response.EnsureSuccessStatusCode();

                    Stream stream1 = response.Content.ReadAsStreamAsync().Result;
                    HttpContent httpContent = response.Content;

                    //Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

                    StorageFile file1 = await folder.CreateFileAsync(fileName.ToString() + ".pdf", CreationCollisionOption.ReplaceExisting);

                    string cnt = await httpContent.ReadAsStringAsync();

                    byte[] buffer = await httpContent.ReadAsByteArrayAsync();

                    await Windows.Storage.FileIO.WriteBytesAsync(file1, buffer);

                    ActivityLog log1 = new ActivityLog() { ActivityLogDescription = "ContractConfirmationPage", AppGuid = App.appID, ActivityLogParams = "PDF generated, apiKey:" + strApiKey };
                    App.PostActivityLog(log1);

                    return file1;
                }
            }
            catch
            {
                ActivityLog log2 = new ActivityLog() { ActivityLogDescription = "ContractConfirmationPage", AppGuid = App.appID, ActivityLogParams = "PDF generation ERROR, apiKey:" + strApiKey };
                App.PostActivityLog(log2);
                return null;
            }
        }
    }
}
