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
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
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

namespace Edatalia_signplyRT.Ayuntamiento
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class FillDataPage : Page
    {
        Citizen RequesterCitizen;
        Citizen AuthorizedCitizen;
        RequestAyuntamiento AyuntamientoRequest;

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


        public FillDataPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;
            this.SizeChanged += FillDataPage_SizeChanged;

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

        void FillDataPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
             var size = Window.Current.Bounds;

             if (size.Width < size.Height) stcUserData.Orientation = Orientation.Vertical;
             else stcUserData.Orientation = Orientation.Horizontal;
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
            ActivityLog log = new ActivityLog() { ActivityLogDescription = "FillDataPage", AppGuid = App.appID, ActivityLogParams = "OnNavigatedTo" };
            App.PostActivityLog(log);

            List<object> param = (List<object>)e.Parameter;

            RequesterCitizen = (Citizen)param[0];
            if (param.Count > 1)
                AuthorizedCitizen = (Citizen)param[1];
            else AuthorizedCitizen = null;

            ucQueryingCitizen.DataContext = RequesterCitizen;
            ucAuthorizedCitizen.DataContext = AuthorizedCitizen;

            AyuntamientoRequest = new RequestAyuntamiento();
            AyuntamientoRequest.AuthorizedCitizen = AuthorizedCitizen;
            AyuntamientoRequest.RequesterCitizen = RequesterCitizen;
            ucRequestDetail.DataContext = AyuntamientoRequest;

            navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private async void AppBarButtonSaveClient_Click(object sender, RoutedEventArgs e)
        {
            string audioPhrase = "";

            Guid contractID = Guid.NewGuid();
            var appFolder = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFolderAsync(contractID.ToString());
            var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

            var uri = new System.Uri("ms-appx:///DocTemplates/Ayto_Bilbao_instancia_I2.docx");
            StorageFile model = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(uri);


            //StorageFile model = await localFolder.GetFileAsync("Ayto_Bilbao_instancia_I2.docx");

            StorageFile selModel = await model.CopyAsync(appFolder, "Model" + contractID.ToString()+".docx", NameCollisionOption.ReplaceExisting);

            prgGeneratingContract.Visibility = Windows.UI.Xaml.Visibility.Visible;
            prgGeneratingContract.IsActive = true;
            ucRequestDetail.IsEnabled = false;

            appbtnSaveClient.IsEnabled = false;

           
            //generate docx from model and data
            WordDocument document = new WordDocument();
            

            Stream inputStream = await selModel.OpenStreamForReadAsync();
            await document.OpenAsync(inputStream, FormatType.Docx);
            inputStream.Dispose();

            var bookmarksNavigator = new BookmarksNavigator(document);

            try
            {
                bookmarksNavigator.MoveToBookmark("DNI");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(RequesterCitizen.DNI, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("NOMBRE");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(RequesterCitizen.Name, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("PRIM_APELLIDO");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(RequesterCitizen.FirstName, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("SEG_APELLIDO");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(RequesterCitizen.SecondName, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("NACIONALIDAD");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(RequesterCitizen.Nationality, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("NUM_PASAPORTE");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(RequesterCitizen.PassportNumber, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("CHECBOX_EUSKERA1");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                if (RequesterCitizen.ComLanguage != null && RequesterCitizen.ComLanguage == LanguageEnum.Euskera) bookmarksNavigator.ReplaceBookmarkContent("X", true);
                else bookmarksNavigator.ReplaceBookmarkContent("", true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("CHECBOX_CASTELLANO1");
                if (RequesterCitizen.ComLanguage!=null && RequesterCitizen.ComLanguage == LanguageEnum.Castellano) bookmarksNavigator.ReplaceBookmarkContent("X", true);
                else bookmarksNavigator.ReplaceBookmarkContent("", true);
                //bookmarksNavigator.ReplaceBookmarkContent(RequesterCitizen.ComLanguage, true);
               
            }
            catch { }

            try
            {
                bookmarksNavigator.MoveToBookmark("CALLE");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(RequesterCitizen.Street, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("NUM_PORTAL");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(RequesterCitizen.Number.ToString(), true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("BIS");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(RequesterCitizen.Bis, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("ESCALERA");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(RequesterCitizen.Staircase, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("PLANTA");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(RequesterCitizen.Floor, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("MANO");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(RequesterCitizen.Hand, true);
            }
            catch { }


            try
            {
                bookmarksNavigator.MoveToBookmark("PUERTA");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(RequesterCitizen.Door, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("PAIS");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(RequesterCitizen.Country, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("MUNICIPIO");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(RequesterCitizen.City, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("COD_POSTAL");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(RequesterCitizen.PostalCode.ToString(), true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("TELEFONO");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(RequesterCitizen.Telephone, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("TELEFONO_MOV");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(RequesterCitizen.MobilePhone, true);
            }
            catch { }

            try
            {
                bookmarksNavigator.MoveToBookmark("CORREO_ELECTRONICO");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(RequesterCitizen.Mail, true);
            }
            catch { }

            //authorized

            try
            {
                bookmarksNavigator.MoveToBookmark("DNI1");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(AuthorizedCitizen.DNI, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("NOMBRE1");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(AuthorizedCitizen.Name, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("PRIM_APELLIDO1");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(AuthorizedCitizen.FirstName, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("SEG_APELLIDO1");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(AuthorizedCitizen.SecondName, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("NACIONALIDAD1");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(AuthorizedCitizen.Nationality, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("NUM_PASAPORTE1");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(AuthorizedCitizen.PassportNumber, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("CHECBOX_EUSKERA11");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                if (AuthorizedCitizen.ComLanguage != null && AuthorizedCitizen.ComLanguage == LanguageEnum.Euskera) bookmarksNavigator.ReplaceBookmarkContent("X", true);
                else bookmarksNavigator.ReplaceBookmarkContent("", true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("CHECBOX_CASTELLANO11");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                if (AuthorizedCitizen.ComLanguage != null && AuthorizedCitizen.ComLanguage == LanguageEnum.Castellano) bookmarksNavigator.ReplaceBookmarkContent("X", true);
                else bookmarksNavigator.ReplaceBookmarkContent("", true);
                //bookmarksNavigator.ReplaceBookmarkContent(RequesterCitizen.ComLanguage, true);

            }
            catch { }

            try
            {
                bookmarksNavigator.MoveToBookmark("CALLE1");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(AuthorizedCitizen.Street, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("NUM_PORTAL1");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(AuthorizedCitizen.Number.ToString(), true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("BIS1");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(AuthorizedCitizen.Bis, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("ESCALERA1");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(AuthorizedCitizen.Staircase, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("PLANTA1");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(AuthorizedCitizen.Floor, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("MANO1");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(AuthorizedCitizen.Hand, true);
            }
            catch { }


            try
            {
                bookmarksNavigator.MoveToBookmark("PUERTA1");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(AuthorizedCitizen.Door, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("PAIS1");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(AuthorizedCitizen.Country, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("MUNICIPIO1");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(AuthorizedCitizen.City, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("COD_POSTAL1");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(AuthorizedCitizen.PostalCode.ToString(), true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("TELEFONO1");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(AuthorizedCitizen.Telephone, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("TELEFONO_MOV1");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(AuthorizedCitizen.MobilePhone, true);
            }
            catch { }

            try
            {
                bookmarksNavigator.MoveToBookmark("CORREO_ELECTRONICO1");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(AuthorizedCitizen.Mail, true);
            }
            catch { }


            //Request Ayuntamiento


            try
            {
                bookmarksNavigator.MoveToBookmark("ADM_DESTINO");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(AyuntamientoRequest.Admin, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("AREA");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(AyuntamientoRequest.Department, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("UNID_AD_DESTINO");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(AyuntamientoRequest.AdminUnit, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("ASUNTO");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(AyuntamientoRequest.Subject, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("MOTIVO_SOLIC");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(AyuntamientoRequest.Cause, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("OPOSICION");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(AyuntamientoRequest.Oposition, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("MATRICULA");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(AyuntamientoRequest.LicencePlate, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("ESPEDIENTE");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(AyuntamientoRequest.ExpedientNumber, true);

            }
            catch { }

            try
            {
                bookmarksNavigator.MoveToBookmark("OBSERVACIONES");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(AyuntamientoRequest.Notes, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("ESCRITO");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                if (AyuntamientoRequest.WritingAttached) bookmarksNavigator.ReplaceBookmarkContent("X", true);
                else bookmarksNavigator.ReplaceBookmarkContent("", true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("DOCUMENTOS");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                if (AyuntamientoRequest.DocumentAttached) bookmarksNavigator.ReplaceBookmarkContent("X", true);
                else bookmarksNavigator.ReplaceBookmarkContent("", true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("DOMIC_BANCARIA");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                if (AyuntamientoRequest.BankAttached) bookmarksNavigator.ReplaceBookmarkContent("X", true);
                else bookmarksNavigator.ReplaceBookmarkContent("", true);
            }
            catch { }

            try
            {
                DateTime dt = DateTime.Now;
                bookmarksNavigator.MoveToBookmark("FECHA");
                bookmarksNavigator.ReplaceBookmarkContent(dt.ToString("dd MM yyyy"), true);
            }
            catch { }

            //recogemos la frase a repetir en la grabación de audio

            try
            {
                bookmarksNavigator.MoveToBookmark("NOMBREA");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(RequesterCitizen.Name, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("APEL1A");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(RequesterCitizen.FirstName, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("APEL2A");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(RequesterCitizen.SecondName, true);
            }
            catch { }

            try
            {
                bookmarksNavigator.MoveToBookmark("DNIA");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(RequesterCitizen.DNI, true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("MOTIVO_SOLA");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(AyuntamientoRequest.Cause.ToString(), true);

            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("DEP_DESTA");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(AyuntamientoRequest.Department.ToString(), true);
            }
            catch { }
            try
            {
                bookmarksNavigator.MoveToBookmark("ADM_DESTA");
                bookmarksNavigator.ReplaceBookmarkContent("", true);
                bookmarksNavigator.ReplaceBookmarkContent(AyuntamientoRequest.Admin.ToString(), true);
            }
            catch { }
            try
            {
                DateTime dt = DateTime.Now;
                bookmarksNavigator.MoveToBookmark("FECHAA");
                bookmarksNavigator.ReplaceBookmarkContent(dt.ToString("dd MM yyyy"), true);
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
            bool res = await document.SaveAsync(ms, FormatType.Docx);

            

            //Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            //C:\Users\alarranaga\AppData\Local\Packages\3022db98-afb6-4c9c-9153-21d964781953_a6xmkfdj4czm2\LocalState
          //  StorageFile tempSaveFile = selModel;

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
            StorageFile pdfConvertedFile = await Upload(tempSaveFile, appFolder, contractID);

            if (pdfConvertedFile != null)
            {
                //load pdf file MainPage
                Frame rootFrame = Window.Current.Content as Frame;
                //   new List<object> { selectedClient, modelFile, tbImporte.Text, dtPicker.Date.UtcTicks }
                // rootFrame.Navigate(typeof(MainPage), pdfConvertedFile);
                //rootFrame.Navigate(typeof(MainPage), new List<object> { pdfConvertedFile, client, selModel.DisplayName, tbImporte.Text, dtStartDate, audioPhrase });
                //tO CHANGE

                XmlSerializer xsSubmit = new XmlSerializer(typeof(RequestAyuntamiento));

                StringWriter sww = new StringWriter();
                XmlWriter writer = XmlWriter.Create(sww);
                xsSubmit.Serialize(writer, AyuntamientoRequest);
                var xml = sww.ToString();


                SaveXmlFile(xml, appFolder);

                rootFrame.Navigate(typeof(MainPage), new List<object> { pdfConvertedFile, null  , selModel.DisplayName, "", DateTime.Now, audioPhrase });
            }
            else
            {
                ResourceLoader rloader = new ResourceLoader();
                string strError = rloader.GetString("strError");
                string strPDFCreationErrormsg = rloader.GetString("strPDFCreationErrormsg");
                var msgDialog = new MessageDialog(strPDFCreationErrormsg, strError);
               // MessageDialog msgDialog = new MessageDialog("No se ha podido generar el pdf, intentelo de nuevo reiniciando la aplicación", "Error");
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

                ActivityLog log = new ActivityLog() { ActivityLogDescription = "FillDataPage", AppGuid = App.appID, ActivityLogParams = "Generating pdf, apiKey:" + strApiKey };
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

                    ActivityLog log1 = new ActivityLog() { ActivityLogDescription = "FillDataPage", AppGuid = App.appID, ActivityLogParams = "PDF generated, apiKey:" + strApiKey };
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
