using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Data.Pdf;
using PdfViewModel;
using Windows.UI.Input;
using Windows.UI.Xaml.Shapes;
using Windows.UI;
using Windows.Devices.Input;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.UI.Xaml.Markup;
using Windows.UI.Input.Inking;

using Windows.Graphics.Imaging;
using Windows.UI.ViewManagement;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Windows.Security.Cryptography;
using Windows.ApplicationModel;
using Windows.Foundation.Metadata;
using Windows.Media.Capture;
using System.IO.Compression;
using Edatalia_signplyRT.Model;
using Windows.Graphics.Display;
using Edatalia_signplyRT.Ayuntamiento;
using Windows.ApplicationModel.Resources;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Edatalia_signplyRT
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //PDF Views to load 
        private PdfDocViewModel pdfViewToLoad; //list view 
        private PdfDocViewModel pdfViewToLoadDetail; //landscape detail view
        private PdfDocViewModel pdfViewToLoadDetailVertical; //portrait detail view

        private StorageFile pdfFileToLoad; 
        private PdfDocument pdfDocument;

        private StorageFile clientImageFile;
        private StorageFile clientDNIImageFile;
        private StorageFile clientAudioRecordingFile;

        private StorageFolder storageFolder;
        private string folderName;
        private string contractID;
        private Contract contractData;
        private string contractText;

        //user input points
        Windows.UI.Input.Inking.InkManager inkManager = new Windows.UI.Input.Inking.InkManager();
        List<ApiPointTime> lstApiPoints = new List<ApiPointTime>();
        List<ApiSegment> lstApiSegments = new List<ApiSegment>();

        private uint penID;
        private uint touchID;
        private Point previousContactPt;

        private Point currentContactPt;
        private double x1;
        private double y1;
        private double x2;
        private double y2;

        // initial screen orientation state, to block or allow signing process    
        string InitialOrientationState = String.Empty;
       
        //data transfer manager to allow sharing contract
        private DataTransferManager dataTransferManager;

       // private string contractText = "";
        
        public MainPage()
        {
            this.InitializeComponent();
            
            //windows size change management
            this.SizeChanged += WindowingPage_SizeChanged;

            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("BackgroundPath"))
            {
                string strBack = ApplicationData.Current.LocalSettings.Values["BackgroundPath"].ToString();
                ImageBrush backgroundBrush = new ImageBrush();
                backgroundBrush.ImageSource = ImageFromRelativePath(strBack);
                this.gridMain.Background = backgroundBrush;
            }           
        }

        #region manage page size and visual state
        private void WindowingPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //get actual visual state
            string state = DetermineVisualState();
            
            //Full Screen Landscape && (signing process not started || signing process started in landscape)
            if ((state == "FullScreenLandscape") && ((InitialOrientationState == "FullScreenLandscape") || (InitialOrientationState == String.Empty)))
            {
                VisualStateManager.GoToState(this, state, true);
                bottomAppBar.IsOpen = true;
                GetItemsPanelTemplate("HorzIPT");
                ToggleIsCompact(false);

                if (listviewThumbnails.Items.Count > 0) this.gridVerticalEdatalia.Visibility = Visibility.Visible;
            }
            //Full Screen Portrait && (signing process not started || signing process started in portrait)
            else if ((state == "FullScreenPortrait") && ((InitialOrientationState == "FullScreenPortrait") || (InitialOrientationState == String.Empty)))
            {
                VisualStateManager.GoToState(this, state, true);
                bottomAppBar.IsOpen = true;
                ToggleIsCompact(true);
            }
            else if ((state == "FullScreenPortrait") || (state == "FullScreenLandscape")) //signing process started in the other state
            {
                VisualStateManager.GoToState(this, "ChangeOrientation", true);
                bottomAppBar.IsOpen = false;
            }
            else //landscape NOT fullscreen view
            {
                //if page size allows show edatalia vertical logo
                if (e.NewSize.Width < 1000) this.gridVerticalEdatalia.Visibility = Visibility.Collapsed;

                //small page size, show pdf pages list in 1 column
                if ((e.NewSize.Width < 700) || e.NewSize.Width < e.NewSize.Height)
                {
                    GetItemsPanelTemplate("VertIPT");
                    //minimize botton app bar menu size, remove text
                    ToggleIsCompact(true);
                }
                else  //page size big enough to show page list in 2 columns
                {
                    GetItemsPanelTemplate("HorzIPT");
                    //show complete app bar menu data
                    ToggleIsCompact(false);
                }
            }
        }

      
        /// <summary>
        /// set listview landscape template to 1 or 2 columns
        /// </summary>
        /// <param name="iptTemplateName">
        /// HorzIPT to set 2 column
        /// VertIPT to set 1 column</param>
        private void GetItemsPanelTemplate(string iptTemplateName)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            var pageV = (Page)rootFrame.Content;
            object titleV;
            pageV.Resources.TryGetValue(iptTemplateName, out titleV);
            this.listviewThumbnails.ItemsPanel = (ItemsPanelTemplate)titleV;
        }

        /// <summary>
        /// Toogle bottom app bar
        /// </summary>
        /// <param name="isCompact">
        /// true remove text from app button 
        /// false show text in app button</param>
        private void ToggleIsCompact(bool isCompact)
        {
            // Get the app bar's root Panel.
            Panel root = bottomAppBar.Content as Panel;
            if (root != null)
            {
                // Get the Panels that hold the controls.
                foreach (Panel panel in root.Children)
                {
                    foreach (ICommandBarElement child in panel.Children)
                    {
                        child.IsCompact = isCompact;
                    }
                }
            }
        }

        /// <summary>
        /// get actual visual state, returns: FullScreenLandscape, FullScreenPortrait or Dimension500
        /// </summary>
        /// <returns></returns>
        private string DetermineVisualState()
        {
            var state = string.Empty;
            var applicationView = ApplicationView.GetForCurrentView();
            var size = Window.Current.Bounds;

            if (applicationView.IsFullScreen)
            {
                if (applicationView.Orientation == ApplicationViewOrientation.Landscape)
                    state = "FullScreenLandscape";
                else
                    state = "FullScreenPortrait";
            }
            else
            {
                state = "Dimension500";
            }
            return state;
        }

        #endregion //manage page size and visual state

        #region page navigation overrides
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                ActivityLog log = new ActivityLog() { ActivityLogDescription = "MainPage", AppGuid = App.appID, ActivityLogParams = "OnNavigatedTo" };
                App.PostActivityLog(log);

                //register to share system contract
                RegisterForShare();

                // if (e.Parameter.GetType() == typeof(StorageFile))
                if (e.Parameter.GetType() == typeof(List<object>))
                {
                    List<object> param = (List<object>)e.Parameter;
                    //  new List<object> {pdfConvertedFile, selectedClient, modelFile, tbImporte.Text, dtPicker.Date.UtcTicks });
                    try
                    {
                        await this.InitFromPDFStorageFile((StorageFile)param[0]);

                        Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                        storageFolder = await localFolder.GetFolderAsync(((StorageFile)param[0]).DisplayName.Split('.')[0]);
                        contractID = ((StorageFile)param[0]).DisplayName.Split('.')[0];

                        if (ApplicationData.Current.LocalSettings.Values["SelectedDemo"].ToString() == "Aseguradora")
                        {
                            contractData = new Contract();

                            contractData.TipoContrato = param[2].ToString();
                            contractData.ClientID = ((Client)param[1]).ClientID;
                            contractData.DNI = ((Client)param[1]).DNI;
                            contractData.StartDate = (DateTime)param[4];
                            contractData.Import = Convert.ToDouble((string)param[3]);

                        }
                        else { }
                        contractText = "";
                        contractText = param[5].ToString();
                        
                    }
                    catch (Exception ex1)
                    {
                        //MessageDialog msgDialog = new MessageDialog("No se pudo cargar el pdf del contrato - " + ex1.Message, "Atención");
                        ResourceLoader rloader = new ResourceLoader();
                        string strError = rloader.GetString("strError");
                        string strPDFLoadException = rloader.GetString("strPDFLoadException");
                        var msgDialog = new MessageDialog(strPDFLoadException + " - " + ex1.Message  , strError);
                        msgDialog.ShowAsync();
                    }
                }
                //if application is started from share invokation
                else if ((e.Parameter).GetType() == typeof(ShareOperation))
                {
                    ShareOperation shareOperation = (ShareOperation)e.Parameter;

                    if (shareOperation.Data.Contains(StandardDataFormats.StorageItems))
                    {
                        try
                        {
                            //get shared file
                            var sharedStorageItems = await shareOperation.Data.GetStorageItemsAsync();
                            //load app from shared pdf file
                            await this.InitFromPDFStorageFile((StorageFile)sharedStorageItems[0]);
                            Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                            storageFolder = await localFolder.GetFolderAsync(((StorageFile)sharedStorageItems[0]).DisplayName.Split('.')[0]);
                            contractID = ((StorageFile)sharedStorageItems[0]).DisplayName.Split('.')[0];
                        }
                        catch (Exception ex)
                        {
                           // MessageDialog msgDialog = new MessageDialog("Failed GetStorageItemsAsync - " + ex.Message, "Atención");
                            ResourceLoader rloader = new ResourceLoader();
                            string strError = rloader.GetString("strError");
                            string strGetStorageItemException = rloader.GetString("strGetStorageItemException");
                            var msgDialog = new MessageDialog(strGetStorageItemException + " - " + ex.Message, strError);
                            msgDialog.ShowAsync();
                        }
                    }
                }
                base.OnNavigatedTo(e);
            }
            catch (Exception ex1)
            {
                string str = ex1.ToString();
            }
        }

       
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.dataTransferManager.DataRequested -= new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(this.ShareStorageItemsHandler);
            base.OnNavigatedFrom(e);
        }

        #endregion //page navigation overrides

        #region aux functions
        /// <summary>
        /// Sharing contract
        /// </summary>
        /// 
        private void RegisterForShare()
        {
            dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager,
                DataRequestedEventArgs>(this.ShareStorageItemsHandler);
        }

        private void ShareStorageItemsHandler(DataTransferManager sender,
        DataRequestedEventArgs e)
        {
            DataRequest request = e.Request;
            request.Data.Properties.Title = "Edatalia";
            request.Data.Properties.Description = "Edatalia";
            
            // Because we are making async calls in the DataRequested event handler,
            // we need to get the deferral first.
            DataRequestDeferral deferral = request.GetDeferral();

            // Make sure we always call Complete on the deferral.
            try
            {
                List<IStorageItem> storageItems = new List<IStorageItem>();
                storageItems.Add(this.pdfFileToLoad);
                request.Data.SetStorageItems(storageItems);
            }
            finally
            {
                deferral.Complete();
            }
        }

       
        /// <summary>
        /// Initialize all views from pdf Storage file
        /// </summary>
        /// <param name="pdfFile"></param>
        private async Task<bool> InitFromPDFStorageFile(StorageFile pdfFile)
        {
            if (pdfFile != null)
            {
                // Validating if selected file is not the same as file currently loaded
                if ((this.pdfFileToLoad == null) || (this.pdfFileToLoad.Path != pdfFile.Path))
                {
                    this.pdfFileToLoad = pdfFile;
                    await LoadPDF(pdfFile);

                    if (ApplicationData.Current.LocalSettings.Values.ContainsKey("BackgroundPath"))
                    {
                        string strBack = ApplicationData.Current.LocalSettings.Values["BackgroundPath"].ToString();
                        ImageBrush backgroundBrush = new ImageBrush();
                        backgroundBrush.ImageSource = ImageFromRelativePath(strBack);
                        this.gridMain.Background = backgroundBrush;
                        //this.gridPdfViewer.Background = backgroundBrush;
                        //this.gridPdfViewerVertical.Background = backgroundBrush;
                    }


                  //  ImageBrush backgroundBrush = new ImageBrush();
                  ////  backgroundBrush.ImageSource = new BitmapImage(new Uri(@"Assets\background.jpg", UriKind.Relative));
                  //  backgroundBrush.ImageSource = ImageFromRelativePath(this, @"Assets\background.jpg");
                  //  backgroundBrush.AlignmentY = AlignmentY.Top;
                  //  backgroundBrush.AlignmentX = AlignmentX.Center;;
                    
                  
                    //open and stick bottomAppBar
                    this.bottomAppBar.IsOpen = true;
                    this.bottomAppBar.IsSticky = true;
                    //disable appbtnOpen, enable appbtnCancelPdf
                   // this.appbtnOpen.IsEnabled = false;
                    this.appbtnCancelPdf.IsEnabled = true;
                    this.appbtnCancelPdf.Visibility = Windows.UI.Xaml.Visibility.Visible;
                }
                return true;
            }
            return false;
        }

        private async Task<bool> LoadPDF(StorageFile pdfFile)
        {
            this.pdfDocument = await PdfDocument.LoadFromFileAsync(pdfFile);

            if (this.pdfDocument != null)
            {
                // Page Size is set to zero for items in main view so that pages of original size are rendered
                Size pageSize;
                Size pageSizeLandscape = GetSize("Landscape");
                if (pageSizeLandscape.Width > pageSizeLandscape.Height) pageSizeLandscape.Width = 595;

                Size pageSizePortrait = GetSize("Vertical");

                // Page size for thumbnail view is set to 150px as this gives good view of the thumbnails on all resolutions
                pageSize.Width = (double)this.Resources["thumbnailWidth"];
                pageSize.Height = (double)this.Resources["thumbnailHeight"];

                // get listview, landscape and portrait pdf views
                this.pdfViewToLoad = new PdfDocViewModel(pdfDocument, pageSize, SurfaceType.VirtualSurfaceImageSource);
                this.pdfViewToLoadDetail = new PdfDocViewModel(pdfDocument, pageSizeLandscape, SurfaceType.VirtualSurfaceImageSource);
                this.pdfViewToLoadDetailVertical = new PdfDocViewModel(pdfDocument, pageSizePortrait, SurfaceType.VirtualSurfaceImageSource);
                //set listview (landscape and vertical) item source
                this.listviewThumbnails.ItemsSource = pdfViewToLoad;
                this.listviewThumbnails.Visibility = Windows.UI.Xaml.Visibility.Visible;
                this.listviewThumbnailsVertical.ItemsSource = pdfViewToLoad;
                this.listviewThumbnailsVertical.Visibility = Windows.UI.Xaml.Visibility.Visible;
                //set selected index to first pdf file page
                this.listviewThumbnailsVertical.SelectionMode = ListViewSelectionMode.Single;
                this.listviewThumbnails.SelectionMode = ListViewSelectionMode.Single;
                this.listviewThumbnails.SelectedIndex = 0;
                this.listviewThumbnailsVertical.SelectedIndex = 0;

                //enable appbtnsign, disable appbtnPosSize and appbtnPosSize options
                this.appbtnSign.IsEnabled = true;
                this.appbtnPosSize.IsEnabled = false;
                this.appbtnPosSize.IsEnabled = false;

                InitialOrientationState = string.Empty;
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Get Page size
        /// </summary>
        /// <param name="p">
        /// Landscape, get app landscape mode size
        /// Vertical, get app portrait mode size
        /// </param>
        /// <returns>Page size</returns>
        private Size GetSize(string p)
        {
            double width = Window.Current.Bounds.Width;
            double height = Window.Current.Bounds.Height;

            if (width > height) //App started in Landscape
            {
                if (p == "Landscape")
                    //return new Size (595,842); //aran atento din-A4 size
                    //345 orientative, 150*2 listview details + margins; 260 edatalia vertical image
                    return new Size(width - 345 - 260, height - bottomAppBar.ActualHeight);
                else
                    //return new Size(595, 842);
                    return new Size(height, width);
            }
            else  //App started in Portrait
            {
                if (p == "Landscape")
                   // return new Size(595, 842);
                    return new Size(height, width - bottomAppBar.ActualHeight);
                else
                    //return new Size(595, 842);
                    return new Size(width, height);
            }
        }

        /// <summary>
        /// Erase inking data and the signature points
        /// </summary>
        private void EraseInkingDetails()
        {
            inkManager.Mode = Windows.UI.Input.Inking.InkManipulationMode.Erasing;
            var strokes = inkManager.GetStrokes();

            for (int i = 0; i < strokes.Count; i++)
            {
                strokes[i].Selected = true;
            }

            inkManager.DeleteSelected();
            inkManager.Mode = Windows.UI.Input.Inking.InkManipulationMode.Inking;

            //initialize posible signing points
            lstApiPoints = new List<ApiPointTime>();
            
            canvasInkingArea.Children.Clear();
            canvasInkingAreaVertical.Children.Clear();

            ResourceLoader rsLoader = new ResourceLoader();
           string strSignHere =  rsLoader.GetString("strSignHere");

            //Add sign here textbox, landscape view
            TextBlock tbSign = new TextBlock();
            tbSign.Height = 40;
            tbSign.Width = 150;
            tbSign.Text = strSignHere;
            tbSign.Visibility = Windows.UI.Xaml.Visibility.Visible;
            tbSign.Foreground = new SolidColorBrush(Colors.Black);
            tbSign.FontSize = 20;
            //Add sign here textbox, portrait view
            TextBlock tbSignV = new TextBlock();
            tbSignV.Height = 40;
            tbSignV.Width = 150;
            tbSignV.Text = strSignHere;
            tbSignV.Visibility = Windows.UI.Xaml.Visibility.Visible;
            tbSignV.Foreground = new SolidColorBrush(Colors.Black);
            tbSignV.FontSize = 20;

            tbSignHere = tbSign;
            canvasInkingArea.Children.Add (tbSignHere);

            tbSignHereVertical = tbSignV;
            canvasInkingAreaVertical.Children.Add(tbSignHereVertical);  
        }

        /// <summary>
        /// Renew canvasInkingArea: remove actual canvas features and add new canvas in initial position
        /// landscape mode canvas
        /// </summary>
        /// 

        private void ReNewInkingCanvas()
        {
            this.canvasInkingArea.ManipulationMode = ManipulationModes.None;
            //remove landscape mode signing canvas manipulation mode events
          
            this.canvasInkingArea.PointerExited -= canvasInkingArea_PointerExited;
            this.canvasInkingArea.PointerEntered -= canvasInkingArea_PointerEntered;
            this.canvasInkingArea.PointerMoved -= canvasInkingArea_PointerMoved;
            this.canvasInkingArea.PointerPressed -= canvasInkingArea_PointerPressed;
            this.canvasInkingArea.PointerReleased -= canvasInkingArea_PointerReleased;

            //remove actual canvasInkingArea
            this.CanvasPdfDetail.Children.Remove(this.canvasInkingArea);
            this.canvasInkingArea = null;

            ResourceLoader rsLoader = new ResourceLoader();
            string strSignHere = rsLoader.GetString("strSignHere");

            //add new canvasInkingArea in initial position
            Canvas newCanvas = new Canvas();
            newCanvas.Background = new SolidColorBrush(Colors.LightBlue);
            newCanvas.Opacity = 0.5;
            newCanvas.Width = 250;
            newCanvas.Height = 150;
            //set new canvas manipulation mode
            newCanvas.ManipulationMode = ManipulationModes.All;
            newCanvas.ManipulationDelta += canvasInkingArea_ManipulationDelta;
            //add "sign here" help text
            TextBlock tb = new TextBlock();
            tb.Height = 40;
            tb.Text = strSignHere;
            tb.Visibility = Windows.UI.Xaml.Visibility.Visible;
            tb.Foreground = new SolidColorBrush(Colors.Black);
            tb.FontSize = 20;
            this.tbSignHere = tb;
            newCanvas.Children.Add(tb);
            //add new canvas to the page
            this.canvasInkingArea = newCanvas;
            this.CanvasPdfDetail.Children.Add(this.canvasInkingArea);
        }

        /// <summary>
        /// Renew canvasInkingArea  (vertical): remove actual canvas features and add new canvas in initial position
        /// portrait mode canvas
        /// </summary>
        private void ReNewInkingCanvasVertical()
        {
            this.canvasInkingAreaVertical.ManipulationMode = ManipulationModes.None;
            //remove portrait mode signing canvas manipulation mode events
           
            this.canvasInkingAreaVertical.PointerExited -= canvasInkingArea_PointerExited;
            this.canvasInkingAreaVertical.PointerEntered -= canvasInkingArea_PointerEntered;
            this.canvasInkingAreaVertical.PointerMoved -= canvasInkingArea_PointerMoved;
            this.canvasInkingAreaVertical.PointerPressed -= canvasInkingArea_PointerPressed;
            this.canvasInkingAreaVertical.PointerReleased -= canvasInkingArea_PointerReleased;
            //remove actual canvasInkingArea
            this.CanvasPdfDetailVertical.Children.Remove(this.canvasInkingAreaVertical);
            this.canvasInkingAreaVertical = null;
            
            //add new canvasInkingArea in initial position
            Canvas newCanvas = new Canvas();
            newCanvas.Background = new SolidColorBrush(Colors.LightBlue);
            newCanvas.Opacity = 0.5;
            newCanvas.Width = 250;
            newCanvas.Height = 150;
            //set new canvas manipulation mode
            newCanvas.ManipulationMode = ManipulationModes.All;
            newCanvas.ManipulationDelta += canvasInkingArea_ManipulationDeltaVertical;
            //add "sign here" help text
            TextBlock tb = new TextBlock();
            tb.Height = 40;
            ResourceLoader rsLoader = new ResourceLoader();
            string strSignHere = rsLoader.GetString("strSignHere");
            tb.Text = strSignHere;
            tb.Foreground = new SolidColorBrush(Colors.Black);
            tb.FontSize = 20;
            this.tbSignHereVertical = tb;
            tb.Visibility = Windows.UI.Xaml.Visibility.Visible;
            newCanvas.Children.Add(tb);
            //add new canvas to the portrait view page
            this.canvasInkingAreaVertical = newCanvas;
            this.CanvasPdfDetailVertical.Children.Add(this.canvasInkingAreaVertical);
        }
        
        #endregion aux functions

        #region appbarbutton click events

        private async void AppBarButtonOpen_Click(object sender, RoutedEventArgs e)
        {
            bool res =  await PickAndLoadFile();
            if (res)
            {
                //enable and disable appbtn options
                this.appbtnCancelPdf.Visibility = Visibility.Visible;
                this.appbtnCancelPdf.IsEnabled = true;
                //this.appbtnOpen.IsEnabled = false;
                this.appbtnSave.IsEnabled = false;
                this.gridVerticalEdatalia.Visibility = Visibility.Visible;
                this.scrollViewerCanvasPdfDetail.ChangeView(0, 0, 1);
                this.scrollViewerCanvasPdfDetailVertical.ChangeView(0, 0, 1);
            }
            //restart inking details
            EraseInkingDetails();
        }

        private async Task<bool> PickAndLoadFile()
        {
            try
            {
                // Launching FilePicker
                FileOpenPicker openPicker = new FileOpenPicker();
                openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                openPicker.ViewMode = PickerViewMode.List;
                openPicker.FileTypeFilter.Add(".pdf");

                // Creating async operation for PickSingleFileAsync
                StorageFile pdfFile = await openPicker.PickSingleFileAsync();
                if (pdfFile != null)
                {
                    await InitFromPDFStorageFile(pdfFile);
                    return true;
                }
            }
            catch 
            {
            }
            return false;
        }

        private async void AppBarButtonCancelPdf_Click(object sender, RoutedEventArgs e)
        {
            if (this.pdfFileToLoad != null)
            {
               // MessageDialog msgDialog = new MessageDialog("¿Estas seguro de que deseas abrir un nuevo documento y descartar el actual?", "Atención");

                ResourceLoader rloader = new ResourceLoader();
                string strAtencion = rloader.GetString("strWarning");
                string strDiscardQuestion = rloader.GetString("strDiscardQuestion");
                
                MessageDialog msgDialog = new MessageDialog(strDiscardQuestion, strAtencion);

                //OK Button, unload viewing pdf file and restart signing procedure 
                string strYes = rloader.GetString("strYes");
                UICommand BtnOk = new UICommand(strYes);
                BtnOk.Invoked = OkBtnClick;
                msgDialog.Commands.Add(BtnOk);

                string strNo = rloader.GetString("strNo");
                UICommand BtnCancel = new UICommand(strNo);
                //BtnCancel.Invoked = BtnCancelClick; //no action needed
                msgDialog.Commands.Add(BtnCancel);

                //Show message
                await msgDialog.ShowAsync();
            }
        }

        /// <summary>
        /// Initialize all views and signing variables to restart with a new file
        /// </summary>
        /// <param name="command"></param>
        private void OkBtnClick(IUICommand command)
        {
            try
            {
                //Initialize all variables
                this.pdfFileToLoad = null;
                this.pdfViewToLoad = null;
                this.pdfViewToLoadDetail = null;
                this.pdfViewToLoadDetailVertical = null;
                this.pdfDocument = null;
                this.listviewThumbnails.ItemsSource = null;
                this.listviewThumbnailsVertical.ItemsSource = null;
                this.imgPageDetail.Source = null;
                this.imgPageDetailVertical.Source = null;

                this.clientDNIImageFile = null;
                this.clientAudioRecordingFile = null;
                this.clientImageFile = null;

                //erase all inking data and renew inking canvas
                EraseInkingDetails();
                ReNewInkingCanvas();
                ReNewInkingCanvasVertical();

                //set appbtn options
                this.canvasInkingArea.Visibility = Visibility.Collapsed;
                this.canvasInkingAreaVertical.Visibility = Visibility.Collapsed;
                this.appbtnCancelSign.Visibility = Visibility.Collapsed;
                this.appbtnSign.IsEnabled = false;
                this.appbtnCancelSign.IsEnabled = false;
                this.appbtnCancelSign.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                this.appbtnValidate.IsEnabled = false;
                this.appbtnRestart.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                this.appbtnPosSize.IsEnabled = false;
                this.appbtnCancelPdf.Visibility = Visibility.Collapsed;
              //  this.appbtnOpen.IsEnabled = true;

                //initialize signing page mode
                InitialOrientationState = string.Empty;

                //set canvas scroll and zoom to enabled
                SetScrollViewersCanvasMode("Enabled");

                //set initial image
                if (ApplicationData.Current.LocalSettings.Values.ContainsKey("BackgroundPath"))
                {
                    string strBack = ApplicationData.Current.LocalSettings.Values["BackgroundPath"].ToString();
                    ImageBrush backgroundBrush = new ImageBrush();
                    backgroundBrush.ImageSource = ImageFromRelativePath(strBack);
                    this.gridMain.Background = backgroundBrush;
                    //this.gridPdfViewer.Background = backgroundBrush;
                    //this.gridPdfViewerVertical.Background = backgroundBrush;
                    this.gridVerticalEdatalia.Visibility = Visibility.Collapsed;
                }
                else
                {
                    ImageBrush myBrush = new ImageBrush();
                    BitmapImage img = ImageFromRelativePath(this, "/Assets/background1.jpg");
                    myBrush.ImageSource = img;
                    this.gridMain.Background = myBrush;
                    //gridPdfViewer.Background = myBrush;
                    //gridPdfViewerVertical.Background = myBrush;
                    this.gridVerticalEdatalia.Visibility = Visibility.Collapsed;
                }


                Frame rootFrame = Window.Current.Content as Frame;
                rootFrame.Navigate(typeof(InitPage), null);
            }
            catch
            {
            }
        }

        private BitmapImage ImageFromRelativePath(string path)
        {
            string tmpe = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\" + path;

            var uri = new Uri(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\" + path);
            BitmapImage result = new BitmapImage();
            result.UriSource = uri;
            return result;
        }

        private BitmapImage ImageFromRelativePath(FrameworkElement parent, string path)
        {
            var uri = new Uri(parent.BaseUri, path);
            BitmapImage bmp = new BitmapImage();
            bmp.UriSource = uri;
            return bmp;
        }

        private void AppBarButtonSign_Click(object sender, RoutedEventArgs e)
        {
            //disable scrollviewers manipulation
            SetScrollViewersCanvasMode("Disabled");

            //set scrollviewers to initial size and position
            this.scrollViewerCanvasPdfDetail.ChangeView(0, 0, 1);
            this.scrollViewerCanvasPdfDetailVertical.ChangeView(0, 0, 1);
            this.listviewThumbnails.SelectionMode = ListViewSelectionMode.None;
            this.listviewThumbnailsVertical.SelectionMode = ListViewSelectionMode.None;

            //show canvas inking area to sign
            this.canvasInkingArea.Visibility = Visibility.Visible;
            this.canvasInkingAreaVertical.Visibility = Visibility.Visible;
            this.canvasInkingArea.ManipulationMode = ManipulationModes.All;
            this.canvasInkingAreaVertical.ManipulationMode = ManipulationModes.All;

            //set appbtn options
            this.appbtnSign.IsEnabled = false;
            this.appbtnValidate.IsEnabled = false;
            this.appbtnPosSize.IsEnabled = true;

            //set signing initial orientation value
            InitialOrientationState = GetCurrentState();
        }

        /// <summary>
        /// get current visual state
        /// </summary>
        /// <returns> current visual state, FullScreenLandscape, FullScreenPortrait or ChangeOrientation</returns>
        public string GetCurrentState()
        {
            IList<VisualStateGroup> list = VisualStateManager.GetVisualStateGroups(VisualTreeHelper.GetChild(this, 0) as FrameworkElement);

            string str = string.Empty;

            if (list.Count > 0) return list[0].CurrentState.Name;
            else return string.Empty;
        }

        private async void AppBarButtonValidate_Click(object sender, RoutedEventArgs e)
        {
            ResourceLoader rloader = new ResourceLoader();
            string msg = rloader.GetString("strAttached") + "\n";

            
            if (clientImageFile != null) msg = msg + rloader.GetString("strPhotoAttached") + "\n";
            else msg = msg + rloader.GetString("strPhotoAttachedMissing") + "\n";

            if (clientDNIImageFile != null) msg = msg + rloader.GetString("strDNIPhotoAttached") + "\n";
            else msg = msg + rloader.GetString("strDNIPhotoAttachedMissing") + "\n";

            if (clientAudioRecordingFile != null) msg = msg + rloader.GetString("strAudioAttached") + "\n";
            else msg = msg + rloader.GetString("strAudioAttachedMissing") + "\n";

            MessageDialog msgDialog = new MessageDialog(msg, rloader.GetString("strResumen"));

            //OK Button, unload viewing pdf file and restart signing procedure
            string Validatemsg = rloader.GetString("strValidateSign");
            UICommand BtnOk = new UICommand(Validatemsg);
            BtnOk.Invoked = OkBtnValidarFirmaClick;
            msgDialog.Commands.Add(BtnOk);

            string AddItemmsg = rloader.GetString("AddItemmsg");
            UICommand BtnCancel = new UICommand(AddItemmsg);
           // BtnCancel.Invoked = BtnCancelValidarClick; //no action needed
            msgDialog.Commands.Add(BtnCancel);

            //Show message
            await msgDialog.ShowAsync();
        }

        private void BtnCancelValidarClick(IUICommand command)
        {
           // throw new NotImplementedException();
        }

        private async void OkBtnValidarFirmaClick(IUICommand command)
        {
            //Disable all app bar options if enabled yet
            appbtnPhoto.IsEnabled = false;
            appbtnDNIPhoto.IsEnabled = false;
            appbtnRecordVoice.IsEnabled = false;
            appbtnRestart.IsEnabled = false;
            appbtnCancelPdf.IsEnabled = false;

            //Validate signature
            ValidarFirma();
        }

        private async void ValidarFirma ()
        {
           StorageFile signatureFile;
           StorageFile dataFile;

            try
            {
                ActivityLog log = new ActivityLog() { ActivityLogDescription = "ValidarFirma", AppGuid = App.appID, ActivityLogParams = "Init" };
                App.PostActivityLog(log);

                    if (lstApiSegments.Count > 0)
                    {   //save signature bitmap
                        signatureFile = await GetSignatureJpgFile();

                        if (signatureFile != null)
                        {
                            dataFile = await CreateDataFileAsync();

                            if (dataFile != null)
                            {
                                StorageFile res = await SendDatatoEdataliaAPI(signatureFile, dataFile);

                                //if the signing process ok
                                if (res!=null)
                                {
                                    ActivityLog log1 = new ActivityLog() { ActivityLogDescription = "ValidarFirma", AppGuid = App.appID, ActivityLogParams = "Signed file received" + res.DisplayName};
                                    App.PostActivityLog(log1);

                                    await ReloadViewNewPdfFile(res);

                                    this.progressRingLandscape.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                                    this.progressRingLandscape.IsActive = false;
                                    this.progressRingPortrait.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                                    this.progressRingPortrait.IsActive = false;
                                }
                                else
                                {
                                    ActivityLog log2 = new ActivityLog() { ActivityLogDescription = "ValidarFirma", AppGuid = App.appID, ActivityLogParams = "ERROR receiving signed file"};
                                    App.PostActivityLog(log2);

                                    ResourceLoader rloader = new ResourceLoader();
                                    string strWarning = rloader.GetString("strWarning");
                                    string strSigninProcessError = rloader.GetString("strSigninProcessError");
                                    var msgDialog = new MessageDialog(strSigninProcessError, strWarning);
                                    //MessageDialog msgDialog = new MessageDialog("Ha habido problemas en el proceso de firma, la aplicación se apagará. Reinicie e inténtelo de nuevo", "Atención");
                                    await msgDialog.ShowAsync();
                                    App.Current.Exit();
                                }
                            }
                            else
                            {
                                ActivityLog log3 = new ActivityLog() { ActivityLogDescription = "ValidarFirma", AppGuid = App.appID, ActivityLogParams = "ERROR generating dat file" };
                                App.PostActivityLog(log3);

                                ResourceLoader rloader = new ResourceLoader();
                                string strWarning = rloader.GetString("strWarning");
                                string strSigninProcessError = rloader.GetString("strSigninProcessError");
                                var msgDialog = new MessageDialog(strSigninProcessError, strWarning);
                                //MessageDialog msgDialog = new MessageDialog("Ha habido problemas en el proceso de firma, la aplicación se apagará. Reinicie e inténtelo de nuevo", "Atención");
                                await msgDialog.ShowAsync();
                                App.Current.Exit();
                            }
                        }
                        else
                        {
                            ActivityLog log4 = new ActivityLog() { ActivityLogDescription = "ValidarFirma", AppGuid = App.appID, ActivityLogParams = "ERROR generating jpeg file" };
                            App.PostActivityLog(log4);

                            ResourceLoader rloader = new ResourceLoader();
                            string strWarning = rloader.GetString("strWarning");
                            string strSigninProcessError = rloader.GetString("strSigninProcessError");
                            var msgDialog = new MessageDialog(strSigninProcessError, strWarning);
                            //MessageDialog msgDialog = new MessageDialog("Ha habido problemas en el proceso de firma, la aplicación se apagará. Reinicie e inténtelo de nuevo", "Atención");
                            await msgDialog.ShowAsync();
                            App.Current.Exit();
                        }
                    }

                    else
                    {
                        ResourceLoader rloader = new ResourceLoader();
                        string strWarning = rloader.GetString("strWarning");
                        string strNoSign = rloader.GetString("strNoSign");
                        var msgDialog = new MessageDialog(strNoSign, strWarning);
                        //MessageDialog msgDialog = new MessageDialog("No hay ninguna firma que guardar", "Atención");
                        await msgDialog.ShowAsync();
                    }
            
            }
            catch
            {
                //var dlge = new MessageDialog("Image is empty");
                //dlge.ShowAsync();
            }
        }

        private async Task<bool> ReloadViewNewPdfFile(StorageFile res)
        {
            this.pdfFileToLoad = res;
            EraseInkingDetails();
            ReNewInkingCanvas();
            ReNewInkingCanvasVertical();
            this.canvasInkingArea.Visibility = Visibility.Collapsed;
            this.canvasInkingAreaVertical.Visibility = Visibility.Collapsed;
            this.listviewThumbnails.SelectionMode = ListViewSelectionMode.Single;
            this.listviewThumbnailsVertical.SelectionMode = ListViewSelectionMode.Single;
            await LoadPDF(res);

            //set appbtn options
            this.appbtnSign.IsEnabled = false;
            this.appbtnValidate.IsEnabled = false;
            this.appbtnPosSize.IsEnabled = false;
            this.appbtnCancelSign.IsEnabled = false;
            this.appbtnCancelSign.Visibility = Visibility.Collapsed;
            this.appbtnRestart.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            this.appbtnRestart.IsEnabled = false;
            this.appbtnSave.IsEnabled = true;

            //remove landscape mode signing canvas manipulation mode events
            
            this.canvasInkingArea.ManipulationMode = ManipulationModes.None;
            this.canvasInkingArea.PointerExited -= canvasInkingArea_PointerExited;
            this.canvasInkingArea.PointerEntered -= canvasInkingArea_PointerEntered;
            this.canvasInkingArea.PointerMoved -= canvasInkingArea_PointerMoved;
            this.canvasInkingArea.PointerPressed -= canvasInkingArea_PointerPressed;
            this.canvasInkingArea.PointerReleased -= canvasInkingArea_PointerReleased;

            //remove portrait mode signing canvas manipulation mode events
            this.canvasInkingAreaVertical.ManipulationMode = ManipulationModes.None;
            
            this.canvasInkingAreaVertical.PointerExited -= canvasInkingArea_PointerExited;
            this.canvasInkingAreaVertical.PointerEntered -= canvasInkingArea_PointerEntered;
            this.canvasInkingAreaVertical.PointerMoved -= canvasInkingArea_PointerMoved;
            this.canvasInkingAreaVertical.PointerPressed -= canvasInkingArea_PointerPressed;
            this.canvasInkingAreaVertical.PointerReleased -= canvasInkingArea_PointerReleased;

            return true;
        }

        private async Task<StorageFile> GetSignatureJpgFile()
        {
            StorageFile signatureFile;
            try
            {
                if (InitialOrientationState == "FullScreenPortrait")
                {
                    signatureFile = await CreateSaveBitmapAsync(canvasInkingAreaVertical);
                    this.progressRingPortrait.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    this.progressRingPortrait.IsActive = true;
                }
                else
                {
                    signatureFile = await CreateSaveBitmapAsync(canvasInkingArea);
                    this.progressRingLandscape.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    this.progressRingLandscape.IsActive = true;
                }
                return signatureFile;
            }
            catch
            { }
            return null;
        }

        /// <summary>
        /// Create content and post data to edatalia webapi
        /// </summary>
        /// <param name="signatureFile"></param>
        /// <param name="dataFile"></param>
        /// <returns>the signed file recieved from Edatalia webapi if signing process ok
        /// else return null</returns>
        private async Task<StorageFile> SendDatatoEdataliaAPI(StorageFile signatureFile, StorageFile dataFile)   
        {
            //C:\Users\alarranaga\AppData\Local\Packages\3022db98-afb6-4c9c-9153-21d964781953_a6xmkfdj4czm2\LocalState
            try
            {
               // Uri uri = new Uri("https://172.16.31.56/editaliaWebApi/api/Upload");
      
                Uri uri = new Uri("https://editaliawebapi.azurewebsites.net/api/Upload");
               // Uri uri = new Uri("https://localhost:44300/api/Upload");
        
                using (var randomStream = (await pdfFileToLoad.OpenReadAsync()))
                {
                    using (var stream = randomStream.AsStream())
                    {
                        using (var randomStreamImage = (await signatureFile.OpenReadAsync()))
                        {
                            using (var streamImage = randomStreamImage.AsStream())
                            {
                                using (var randomStreamDataFile = (await dataFile.OpenReadAsync()))
                                {
                                    using (var streamData = randomStreamDataFile.AsStream())
                                    {
                                        using (var content = new MultipartFormDataContent())
                                        {
                                            try
                                            {
                                                //pdf file stream
                                                content.Add(new StreamContent(stream));
                                                //signature jpg stream
                                                content.Add(new StreamContent(streamImage));
                                                //dat file stream
                                                content.Add(new StreamContent(streamData));
                                                ////serialization of the signature points
                                                //string json = JsonConvert.SerializeObject(lstApiPoints);
                                                //content.Add(new StringContent(json));
                                                //////serialization of the signature position
                                                //Point p = GetSignaturePosition();
                                                //string jsonPoint = JsonConvert.SerializeObject(p);
                                                //content.Add(new StringContent(jsonPoint));


                                                ////serialization of the signature position
                                                WidgetPosition wPos = GetWidgetPosition();
                                                //wPos = PixelsToUserUnits(wPos);
                                                
                                                string jsonPoint = JsonConvert.SerializeObject(wPos);
                                                content.Add(new StringContent(jsonPoint));
                                                
                                                //signed page number
                                                int page = GetSignedPage();
                                                content.Add(new StringContent(page.ToString()));

                                                ////serialization of the signature size
                                                WidgetSize wSize = GetWidgetSize();
                                               // wSize = PixelsToUserUnits(wSize); //ojo aran!!
                                              
                                                string jsonSize = JsonConvert.SerializeObject(wSize);
                                                content.Add(new StringContent(jsonSize));

                                                //send post request
                                                var httpClient = new HttpClient();
                                                var res = await httpClient.PostAsync(uri, content);
                                                res.EnsureSuccessStatusCode();
                                                //todo check if content is available
                                                StorageFile signedFile = await SaveSignedFile(res.Content);
                                                return signedFile;
                                              
                                            }
                                            catch (Exception ex1)
                                            {
                                                string s = ex1.ToString();
                                                return null;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string str = ex.ToString();
            }
            return null;
        }

        private Point PixelsToUserUnits(Point point)
        {
            float ppi = DisplayInformation.GetForCurrentView().LogicalDpi;

            float ppuu = ppi / 72;

            double x = point.X * ppuu;
            double y = point.Y * ppuu;

            Point pointInUserUnits = new Point();

            pointInUserUnits.X = (int)x;
            pointInUserUnits.Y = (int)y;

            return pointInUserUnits;
        }

        private WidgetSize PixelsToUserUnits(WidgetSize wSize)
        {
            float ppi = DisplayInformation.GetForCurrentView().LogicalDpi;

            float ppuu = ppi / 72;

            float x = wSize.Width * ppuu;
            float y = wSize.Height * ppuu;

            WidgetSize wSizeInUserUnits = new WidgetSize();

            wSizeInUserUnits.Width = (int)x;
            wSizeInUserUnits.Height = (int)y;

            return wSizeInUserUnits;
        }

        private WidgetPosition PixelsToUserUnits(WidgetPosition posInPixels)
        {
            float ppi = DisplayInformation.GetForCurrentView().LogicalDpi;

            float ppuu = ppi / 72;

            float x = posInPixels.X * ppuu;
            float y = posInPixels.Y * ppuu;

            WidgetPosition wPosInUserUnits = new WidgetPosition();
          
            wPosInUserUnits.X = (int)x;
            wPosInUserUnits.Y = (int)y;

            return wPosInUserUnits;
        }

        private WidgetSize GetWidgetSize()
        {
            WidgetSize wSize = new WidgetSize();
            wSize.Height = Convert.ToInt32(GetSignatureHeight());
            wSize.Width = Convert.ToInt32(GetSignatureWidth());

            //return wSize;

            ////////////////

            int dinA4Width = 595;
            int dinA4Height = 842;

            if (InitialOrientationState == "FullScreenPortrait")
            {
                try
                {
                    if (imgPageDetailVertical.ActualWidth > imgPageDetailVertical.ActualHeight)
                    {
                        dinA4Width = 842;
                        dinA4Height = 595;
                    }

                    wSize.Height = (int) GetSignatureHeight(dinA4Height, imgPageDetailVertical.ActualHeight);
                    wSize.Width = (int) GetSignatureWidth(dinA4Width, imgPageDetailVertical.ActualWidth);


                    //var Image = (CompositeTransform)canvasInkingAreaVertical.RenderTransform;

                    //wPos.X = (int)(dinA4Width * Image.TranslateX / imgPageDetailVertical.ActualWidth);
                    //wPos.Y = (int)((dinA4Height * (imgPageDetailVertical.ActualHeight - Image.TranslateY)) / imgPageDetailVertical.ActualHeight) - wSize.Height;
                    //return wPos;

                }
                catch
                {
                    wSize.Height = Convert.ToInt32(GetSignatureHeight());
                    wSize.Width = Convert.ToInt32(GetSignatureWidth());
                }
            }
            else
            {
                try
                {
                    if (imgPageDetail.ActualWidth > imgPageDetail.ActualHeight)
                    {
                        dinA4Width = 842;
                        dinA4Height = 595;
                    }

                    wSize.Height = (int) GetSignatureHeight(dinA4Height, imgPageDetail.ActualHeight);
                    wSize.Width = (int) GetSignatureWidth(dinA4Width, imgPageDetail.ActualWidth);

                    //var Image = (CompositeTransform)canvasInkingArea.RenderTransform;



                    //wPos.X = (int)(dinA4Width * Image.TranslateX / imgPageDetail.ActualWidth);
                    //wPos.Y = (int)((dinA4Height * (imgPageDetail.ActualHeight - Image.TranslateY)) / imgPageDetail.ActualHeight) - wSize.Height;

                    //return wPos;
                }
                catch
                {
                    wSize.Height = Convert.ToInt32(GetSignatureHeight());
                    wSize.Width = Convert.ToInt32(GetSignatureWidth());
                }


                ////////////////
            }
            return wSize;
        }


        private int GetSignedPage()
        {
            if (InitialOrientationState == "FullScreenPortrait")
            {
                var pdfpageview = (PdfPageViewModel) this.imgPageDetailVertical.DataContext;
                return (int)pdfpageview.PageIndex + 1;
            }
            else 
            {
                var pdfpageview = (PdfPageViewModel)this.imgPageDetail.DataContext;
                return (int) pdfpageview.PageIndex + 1;
        }
        }

        /// <summary>
        /// Save the signed file received from the Edatalia WebApi temporally in the AppData folder
        /// </summary>
        /// <param name="httpContent"></param>
        /// <returns>the temporally signed storageFile</returns>
        private async Task<StorageFile> SaveSignedFile(HttpContent httpContent)
        {
            if (storageFolder==null)
                storageFolder = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFolderAsync(folderName);
           
            //Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

            //C:\Users\alarranaga\AppData\Local\Packages\3022db98-afb6-4c9c-9153-21d964781953_a6xmkfdj4czm2\LocalState
            StorageFile file = await storageFolder.CreateFileAsync("TempSignedFile.pdf", CreationCollisionOption.ReplaceExisting);

            string cnt = await httpContent.ReadAsStringAsync();
            byte[] buffer = await httpContent.ReadAsByteArrayAsync();

            await Windows.Storage.FileIO.WriteBytesAsync(file, buffer);
            
            return file;
        }
        /// <summary>
        /// Save canvas content as jpg image
        /// </summary>
        /// <param name="canvas"></param>
        /// <returns></returns>
        //private async Task<StorageFile> CreateSaveBitmapAsync(Canvas canvas)
        private async Task<StorageFile> CreateSaveBitmapAsync(Canvas canvas)
        {
            try
            {
                if (canvas != null)
                {
                    canvas.Background = new SolidColorBrush(Colors.White);
                    canvas.Opacity = 1;
                    //double imageWidth;
                    //double imageHeight;
                    //CompositeTransform imgSize;
                    //float ppi = DisplayInformation.GetForCurrentView().LogicalDpi;
                    //float ppuu = ppi / 72;

                    WidgetSize wSize = GetWidgetSize();

                    //try
                    //{   
                    //    imgSize = (CompositeTransform)(canvas.RenderTransform);
                       
                    //    imageWidth = canvas.Width * imgSize.ScaleX;
                    //    imageHeight = canvas.Height * imgSize.ScaleY;
                       
                    //}
                    //catch
                    //{
                    //    imageWidth = canvas.Width;
                    //    imageHeight = canvas.Height;
                    //}

                    //from pixels to point per user unit
                    //imageWidth = imageWidth * ppuu;
                    //imageHeight = imageHeight * ppuu;

                    RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap();
                    //await renderTargetBitmap.RenderAsync(canvas, (int)imageWidth, (int)imageHeight);
                    await renderTargetBitmap.RenderAsync(canvas, (int)wSize.Width, (int)wSize.Height);
                  
                   // Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                    if (storageFolder == null)
                             storageFolder = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFolderAsync(folderName);

                    StorageFile file = await storageFolder.CreateFileAsync("tmpCanvas.jpg", CreationCollisionOption.ReplaceExisting);

                    if (file != null)
                    {
                        var pixels = await renderTargetBitmap.GetPixelsAsync();

                        using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                        {
                            var encoder = await
                                BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
                            byte[] bytes = pixels.ToArray();


                            //encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                            //                   BitmapAlphaMode.Ignore,
                            //                  (uint)imageWidth, (uint)imageHeight,
                            //                   96, 96, bytes);

                            encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                                              BitmapAlphaMode.Ignore,
                                             (uint)wSize.Width, (uint)wSize.Height,
                                              96, 96, bytes);

                            await encoder.FlushAsync();
                            return file;
                        }
                    }
                    return file;
                }
           
            }
            catch (Exception ex)
            {
                string mst = ex.ToString();
            }
            return null;
        }

        private async Task<StorageFile> CreateDataFileAsync()
        {
            try
            {
               // Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                if (storageFolder == null)
                    storageFolder = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFolderAsync(folderName);

                StorageFile file = await storageFolder.CreateFileAsync("dataFile.dat", CreationCollisionOption.ReplaceExisting);

                if (file != null)
                {
                    using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        string strIn = "MAX_X;MAX_Y;MAX_PRESS;LCD_WIDTH;LCD_HEIGHT;LX;RX;TY;BY;TIME_START;TIME_END;HARDWARE;SOFTWARE";

                        // Convert the string to Utf8 binary data.
                        IBuffer header = CryptographicBuffer.ConvertStringToBinary(strIn, BinaryStringEncoding.Utf8);
                        await stream.WriteAsync(header);
                        strIn = "\r\n";
                        header = CryptographicBuffer.ConvertStringToBinary(strIn, BinaryStringEncoding.Utf8);
                        await stream.WriteAsync(header);

                        Package package = Package.Current;
                        string software = package.DisplayName + " " + versionString(package.Id.Version);
                        byte[] bt = System.Text.Encoding.UTF8.GetBytes(software);
                        string sftw = Convert.ToBase64String(bt);

                        Windows.Security.ExchangeActiveSyncProvisioning.EasClientDeviceInformation deviceInfo = new Windows.Security.ExchangeActiveSyncProvisioning.EasClientDeviceInformation();
                        string manufacter = deviceInfo.SystemProductName;
                        bt = System.Text.Encoding.UTF8.GetBytes(manufacter);
                        string mnftr = Convert.ToBase64String(bt);

                        DateTime startTime = GetTime(lstApiPoints[0].TimeSpan);
                        DateTime endTime = GetTime(lstApiPoints[lstApiPoints.Count - 1].TimeSpan);

                        //string max_x = "10400";
                        //string max_y = "6048";
                        //string max_press = "256";
                        //string lcd_width = "800";
                        //string lcd_height = "480";
                        //todo, revisar,... no está bien!!! y vertical????
                        string imgSize ;
                        string imgHeight ;

                        if (InitialOrientationState == "FullScreenPortrait")
                        {
                            imgSize = Math.Round(imgPageDetailVertical.ActualWidth).ToString();
                            imgHeight = Math.Round(imgPageDetailVertical.ActualHeight).ToString();
                        }
                        else 
                        {

                            imgSize = Math.Round(imgPageDetail.ActualWidth).ToString();
                            imgHeight = Math.Round(imgPageDetail.ActualHeight).ToString();
                        }


                        string max_x = imgSize;
                        string max_y = imgHeight;
                        string max_press = "256";
                        string lcd_width = imgSize;
                        string lcd_height = imgHeight;

                        WidgetPosition posWidget = GetWidgetPosition();
                        Point p = new Point(posWidget.X, posWidget.Y);
                    //    p = PixelsToUserUnits(p);
                      
                        WidgetSize signatureSize = GetWidgetSize();
                      //  signatureSize = PixelsToUserUnits(signatureSize);

                        double signatureWidth = signatureSize.Width;
                        double signatureHeight = signatureSize.Height;
                        string lx = Math.Round(p.X, 0).ToString();
                        string rx = Math.Round((p.X + signatureWidth), 0).ToString();
                        string ty = Math.Round(p.Y, 0).ToString();
                        string by = Math.Round((p.Y + signatureHeight), 0).ToString();

                        // 10400;6048;511;800;480;88;722;104;298;16/05/2012@13:18:00;16/05/2012@13:18:31;R2FsYXh5IE5vdGUgMTAuMQ==;ZWNvU2lnbmF0dXJlIEFuZHJvaWQgMC4x
                        strIn = max_x + ";" + max_y + ";" + max_press + ";" + lcd_width + ";" + lcd_height + ";" + lx + ";" + rx + ";" + ty + ";" + by + ";" + startTime.ToString("dd/MM/yyyy@HH:mm:ss") + ";" + endTime.ToString("dd/MM/yyyy@HH:mm:ss") + ";" + mnftr + ";" + sftw;
                        header = CryptographicBuffer.ConvertStringToBinary(strIn, BinaryStringEncoding.Utf8);
                        await stream.WriteAsync(header);

                        strIn = "\r\n";
                        header = CryptographicBuffer.ConvertStringToBinary(strIn, BinaryStringEncoding.Utf8);
                        await stream.WriteAsync(header);

                        strIn = "X;Y;PRESS;SW;TIME";
                        header = CryptographicBuffer.ConvertStringToBinary(strIn, BinaryStringEncoding.Utf8);
                        await stream.WriteAsync(header);
                  
                        strIn = "\r\n";
                        header = CryptographicBuffer.ConvertStringToBinary(strIn, BinaryStringEncoding.Utf8);
                        await stream.WriteAsync(header);

                        double x;
                        double y;
                     
                        ulong dtDif;
                        int roundPressure;

                        //DateTime dtPoint;
                        TimeSpan ts;
                        

                        foreach (ApiPointTime pt in lstApiPoints)
                        {
                            x = Math.Round(posWidget.X + pt.XPos, 0);
                            y = Math.Round(posWidget.Y + pt.YPos, 0);


                            dtDif =  (pt.TimeSpan - lstApiPoints[0].TimeSpan);
                            ts = new TimeSpan((long)dtDif);

                            if (pt.Pressure == 0.5) roundPressure = 256;
                            else roundPressure = (int)Math.Ceiling(pt.Pressure); 
                            //strIn = pt.XPos + ";" + pt.YPos + ";" + pt.Pressure + ";" + "1"  + ";" + pt.TimeSpan + "\r\n";
                           // strIn = x + ";" + y + ";" + pt.Pressure + ";" + "1" + ";" + pt.TimeSpan + "\r\n";
                            strIn = x + ";" + y + ";" + roundPressure + ";" + "1" + ";" + Math.Round(ts.TotalMilliseconds*10) + "\r\n";
                            header = CryptographicBuffer.ConvertStringToBinary(strIn, BinaryStringEncoding.Utf8);
                            await stream.WriteAsync(header);
                        }

                        await stream.FlushAsync();
                    }
                }
                return file;
            }
            catch
            {
                return null;
            }
        }

        private double GetSignatureHeight()
        {
            double height = 0;
            if (InitialOrientationState == "FullScreenPortrait")
            {
                height = GetCanvasHeight(canvasInkingAreaVertical);
            }
            else
            {
                height = GetCanvasHeight(canvasInkingArea);
            }
            return height;
        }

        private double GetSignatureWidth()
        {
            double width = 0;
            if (InitialOrientationState == "FullScreenPortrait")
            {
                width = GetCanvasWidth(canvasInkingAreaVertical);

            }
            else
            {
                width = GetCanvasWidth(canvasInkingArea);
            }
            return width;
        }

        private double GetSignatureHeight(int dina4Height, double actualHeight)
        {
            double height = 0;
            if (InitialOrientationState == "FullScreenPortrait")
            {
                height = GetCanvasHeight(canvasInkingAreaVertical);
            }
            else
            {
                height = GetCanvasHeight(canvasInkingArea);
            }
            //return height;
            double newHeight = dina4Height * height / actualHeight;
            return newHeight;
        }

        private double GetSignatureWidth(int dina4Width, double actualWidth)
        {
            double width = 0;
            if (InitialOrientationState == "FullScreenPortrait")
            {
                width = GetCanvasWidth(canvasInkingAreaVertical);

            }
            else
            {
                width = GetCanvasWidth(canvasInkingArea);
            }
            //return width;
            double newWidht = width * dina4Width / actualWidth;
            return newWidht;
        }

        private double GetCanvasHeight(Canvas canvas)
        {
            CompositeTransform imgSize;
           double imageHeight = 0;
            try
            {
                imgSize = (CompositeTransform)(canvas.RenderTransform);

               // imageWidth = canvas.Width * imgSize.ScaleX;
                imageHeight = canvas.Height * imgSize.ScaleY;
            }
            catch
            {
               // imageWidth = canvas.Width;
                imageHeight = canvas.Height;
            }
            return imageHeight;
        }
        private double GetCanvasWidth(Canvas canvas)
        {
            CompositeTransform imgSize;
            double imageWidth = 0;
            try
            {
                imgSize = (CompositeTransform)(canvas.RenderTransform);

                 imageWidth = canvas.Width * imgSize.ScaleX;
                //imageHeight = canvas.Height * imgSize.ScaleY;
            }
            catch
            {
                 imageWidth = canvas.Width;
                //imageHeight = canvas.Height;
            }
            return imageWidth;
        }

        private DateTime GetTime(ulong ts)
        {
            DateTime dt = DateTime.Now.AddMilliseconds(-System.Environment.TickCount);
            return dt.AddMilliseconds(ts/1000);
        }


       private String versionString(PackageVersion version)
        {
            return String.Format("{0}.{1}.{2}.{3}",
                                 version.Major, version.Minor, version.Build, version.Revision);
        }

        private void AppBarButtonCancelSign_Click(object sender, RoutedEventArgs e)
        {
            //initialize inking details
            EraseInkingDetails();
            //Hide "firme aqui"
            tbSignHere.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            tbSignHereVertical.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            
            //set appbtn options
            this.appbtnSign.IsEnabled = false;
            this.appbtnValidate.IsEnabled = true;
            this.appbtnPosSize.IsEnabled = false;
            this.appbtnCancelSign.IsEnabled = true;
            this.appbtnCancelSign.Visibility = Visibility.Visible;
        }

        private void AppBarButtonPosSize_Click(object sender, RoutedEventArgs e)
        {
            //set appbtn options
            this.appbtnSign.IsEnabled = false;
            this.appbtnValidate.IsEnabled = true;
            this.appbtnPosSize.IsEnabled = false;
            this.appbtnCancelSign.IsEnabled = true;
            this.appbtnCancelSign.Visibility = Visibility.Visible;
            this.appbtnRestart.Visibility = Visibility.Visible;
            this.appbtnRestart.IsEnabled = true;

            this.canvasInkingArea.ManipulationMode = ManipulationModes.None;
            //add landscape mode signing canvas manipulation mode events
           
            this.canvasInkingArea.PointerExited += canvasInkingArea_PointerExited;
            this.canvasInkingArea.PointerEntered += canvasInkingArea_PointerEntered;
            this.canvasInkingArea.PointerMoved += canvasInkingArea_PointerMoved;
            this.canvasInkingArea.PointerPressed += canvasInkingArea_PointerPressed;
            this.canvasInkingArea.PointerReleased += canvasInkingArea_PointerReleased;
            tbSignHere.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            this.canvasInkingAreaVertical.ManipulationMode = ManipulationModes.None;
            //add portrait mode signing canvas manipulation mode events
           
            this.canvasInkingAreaVertical.PointerExited += canvasInkingArea_PointerExited;
            this.canvasInkingAreaVertical.PointerEntered += canvasInkingArea_PointerEntered;
            this.canvasInkingAreaVertical.PointerMoved += canvasInkingArea_PointerMoved;
            this.canvasInkingAreaVertical.PointerPressed += canvasInkingArea_PointerPressed;
            this.canvasInkingAreaVertical.PointerReleased += canvasInkingArea_PointerReleased;
            tbSignHereVertical.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private void AppBarButtonRestart_Click(object sender, RoutedEventArgs e)
        {
            //set appbtn options
            this.appbtnRestart.Visibility = Visibility.Collapsed;
            this.appbtnSign.IsEnabled = false;
            this.appbtnValidate.IsEnabled = false;
            this.appbtnPosSize.IsEnabled = true;
            this.appbtnCancelSign.IsEnabled = false;
            this.appbtnCancelSign.Visibility = Visibility.Collapsed;
            this.appbtnCancelSign.Visibility = Visibility.Visible;
            this.appbtnRestart.Visibility = Visibility.Collapsed;
            this.appbtnRestart.IsEnabled = false;

            //disable scrolling and zooming of the pdf page view
            SetScrollViewersCanvasMode("Disabled");

            //Initialize all signing values
            EraseInkingDetails();
            ReNewInkingCanvas();
            ReNewInkingCanvasVertical();
            AppBarButtonSign_Click(null, null);
        }

        private void SetScrollViewersCanvasMode(string mode)
        {
            ScrollMode scMode;
            ZoomMode zmMode;
            if (mode == "Disabled")
            {
                scMode = ScrollMode.Disabled;
                zmMode = ZoomMode.Disabled;
            }
            else
            {
                scMode = ScrollMode.Enabled;
                zmMode = ZoomMode.Enabled;
            }
            this.scrollViewerCanvasPdfDetailVertical.HorizontalScrollMode = scMode;
            this.scrollViewerCanvasPdfDetailVertical.VerticalScrollMode = scMode;
            this.scrollViewerCanvasPdfDetailVertical.ZoomMode = zmMode;

            this.scrollViewerCanvasPdfDetail.HorizontalScrollMode = scMode;
            this.scrollViewerCanvasPdfDetail.VerticalScrollMode = scMode;
            this.scrollViewerCanvasPdfDetail.ZoomMode = zmMode;
        }

        private async void AppBarButtonSave_Click(object sender, RoutedEventArgs e)
        {
            ////save the pdf file
            //var picker = new FileSavePicker();
            //picker.FileTypeChoices.Add(".PDF file", new string[] { ".pdf" });
            //StorageFile file = await picker.PickSaveFileAsync();
            //if (file != null)
            //{
            //    using (IRandomAccessStream streamToCopy = await pdfFileToLoad.OpenReadAsync())
            //    {
            //        if (streamToCopy.Size > 0)
            //        {
            //            byte[] binput = new byte[streamToCopy.Size];
            //            IBuffer output = await streamToCopy.ReadAsync(binput.AsBuffer(0, (int)streamToCopy.Size), (uint)streamToCopy.Size, InputStreamOptions.Partial);
            //            byte[] boutput = output.ToArray();
            //            await Windows.Storage.FileIO.WriteBytesAsync(file, boutput);
            //        }
            //    }

            //    await ReloadViewNewPdfFile(file);
            //}



            // Retrieve files to compress
            IReadOnlyList<StorageFile> filesToCompress = await GetStorageFiles(storageFolder as IStorageItem);
            // Created new file to store compressed files
            //This will create a file under the selected folder in the name   “Compressed.zip”
            Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            StorageFile zipFile = await localFolder.CreateFileAsync(contractID+".zip",CreationCollisionOption.GenerateUniqueName);
            // Create stream to compress files in memory (ZipArchive  can't stream to an IRandomAccessStream, see
            //   http://social.msdn.microsoft.com/Forums/en-US/winappswithcsharp/thread/62541424-ba7d-43d3-9585-1fe53dc7d9e2
            // for details on this issue)
            using (MemoryStream zipMemoryStream = new MemoryStream())
            {
                // Create zip archive
                using (ZipArchive zipArchive = new ZipArchive(zipMemoryStream, ZipArchiveMode.Create))
                {
                    // For each file to compress...
                    foreach (StorageFile fileToCompress in filesToCompress)
                    {
                        //Read the contents of the file
                        byte[] buffer = WindowsRuntimeBufferExtensions.ToArray(await FileIO.ReadBufferAsync(fileToCompress));
                        // Create a zip archive entry
                        ZipArchiveEntry entry = zipArchive.CreateEntry(fileToCompress.Name);
                        // And write the contents to it
                        using (Stream entryStream = entry.Open())
                        {
                            await entryStream.WriteAsync(buffer, 0, buffer.Length);
                        }
                    }
                }
                using (IRandomAccessStream zipStream = await zipFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    // Write compressed data from memory to file
                    using (Stream outstream = zipStream.AsStreamForWrite())
                    {
                        byte[] buffer = zipMemoryStream.ToArray();
                        outstream.Write(buffer, 0, buffer.Length);
                        outstream.Flush();
                    }
                }
            }

            if (ApplicationData.Current.LocalSettings.Values["SelectedDemo"].ToString() == "Aseguradora")
            {
                string jsonClient = JsonConvert.SerializeObject(contractData);
                var content = new StringContent(jsonClient, System.Text.Encoding.UTF8, "application/json");


                var res = await App.httpClient.PostAsync(App.uri + "api/Contracts", content);
                res.EnsureSuccessStatusCode();
            }
            else if (ApplicationData.Current.LocalSettings.Values["SelectedDemo"].ToString() == "Ayuntamiento")
            {
                //TODO si se requiere guardar todas las solicitudes!!!
                //string jsonClient = JsonConvert.SerializeObject(requestAyuntameintoData);
                //var content = new StringContent(jsonClient, System.Text.Encoding.UTF8, "application/json");


                //var res = await App.httpClient.PostAsync(App.uri + "api/Requests", content);
                //res.EnsureSuccessStatusCode();
            }

            ResourceLoader rloader = new ResourceLoader();
            string strMsg = rloader.GetString("strMsg");
            string strZipSaved = rloader.GetString("strZipSaved");
            var msgDialog = new MessageDialog(strZipSaved, strMsg);
            //MessageDialog msgDialog = new MessageDialog("El zip con todos los datos se ha guardado correctamente", "Msg");



            //OK Button
            string Closemsg = rloader.GetString("strCloseMsg");
            UICommand BtnOk = new UICommand(Closemsg);
            //BtnOk.Invoked = OkBtnValidarFirmaClick;
            msgDialog.Commands.Add(BtnOk);




            await msgDialog.ShowAsync();
            Frame rootFrame = Window.Current.Content as Frame;

            if (ApplicationData.Current.LocalSettings.Values["SelectedDemo"].ToString() == "Ayuntamiento")
                rootFrame.Navigate(typeof(InitPageAyuntamiento), null);
            else     rootFrame.Navigate(typeof(InitPage), null);
     

        }

        async Task<List<StorageFile>> GetStorageFiles(IStorageItem storageItem)
        {
            List<StorageFile> storageFileList = new List<StorageFile>();
            // Gets the items under the selected folder (Storage Item)
            IReadOnlyList<IStorageItem> items = await (storageItem as StorageFolder).GetItemsAsync();
            foreach (IStorageItem item in items)
            {
                switch (item.Attributes)
                {
                    case FileAttributes.Directory:
                        // If the item is a directory under the selected folder, then retrieve   the files under the directory by calling the same function recursively
                        List<StorageFile> temp = await GetStorageFiles(item);
                        // Copy the files under the directory to the storage file list
                        Copy(temp, storageFileList);
                        break;
                    default:
                        // If the item is a file, Add the item to the storage file list
                        storageFileList.Add(item as StorageFile);
                        break;
                }
            }
            // Return storage file list for compression
            return storageFileList;
        }

        private void Copy(List<StorageFile> source, List<StorageFile> destination)
        {
            // For each file item present under the directory copy it to the   destination storage file list
            foreach (StorageFile file in source)
            {
                destination.Add(file);
            }
        }

        private async void AppBarButtonAddPhoto_Click(object sender, RoutedEventArgs e)
        {
            clientImageFile = await CapturePhoto(new Size(10, 9), "fotoCliente.jpg");

            if (clientImageFile != null) appbtnPhoto.IsEnabled = false;
        }

        private async Task<StorageFile> CapturePhoto(Size size, string photoName)
        {
            StorageFile photoFile = null;
            CameraCaptureUI dialog = new CameraCaptureUI();
            
            Size aspectRatio = size;
            dialog.PhotoSettings.CroppedAspectRatio = aspectRatio;

            StorageFile file = await dialog.CaptureFileAsync(CameraCaptureUIMode.Photo);

            if (file != null)
            {
                photoFile = await file.CopyAsync(storageFolder, photoName, NameCollisionOption.GenerateUniqueName);
                await file.DeleteAsync();
            }
           
            return photoFile;
        }

        private async void AppBarButtonAddDNI_Click(object sender, RoutedEventArgs e)
        {
            clientDNIImageFile = await CapturePhoto(new Size(16, 9), "fotoDNI.jpg");
            if (clientDNIImageFile != null) appbtnDNIPhoto.IsEnabled = false;
        }

        AudioRecordUserControl ucAudio;
        private void AppBarButtonRecordVoice_Click(object sender, RoutedEventArgs e)
        {
            //string contractText  = "yo, arantxa larrañaga con dni 72459759 acpeto el contrato de seguro de vida,....";
            //string contractText = "";
            ucAudio = new AudioRecordUserControl(contractID, contractText);
            ucAudio.Visibility = Visibility;
            gridMain.Children.Add(ucAudio);

            ucAudio.AudioRecordEvent += ucAudio_AudioRecordEvent;
            
            bottomAppBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        void ucAudio_AudioRecordEvent(StorageFile file)
        {
            bottomAppBar.Visibility = Windows.UI.Xaml.Visibility.Visible;
            gridMain.Children.Remove(ucAudio);
            clientAudioRecordingFile = file;

            if (clientAudioRecordingFile != null) appbtnRecordVoice.IsEnabled = false;
        }

        #endregion //appbarbutton click events


        #region listviews selection changed event
        private void listviewThumbnails_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.listviewThumbnails.SelectedIndex != -1)
            {
                //show selected page details, landscape mode
                this.imgPageDetail.DataContext = pdfViewToLoadDetail[this.listviewThumbnails.SelectedIndex];
            }
        }
        private void listviewThumbnailsVertical_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.listviewThumbnailsVertical.SelectedIndex != -1)
            {
                //show selected page details, portrait mode
                this.imgPageDetailVertical.DataContext = pdfViewToLoadDetailVertical[this.listviewThumbnailsVertical.SelectedIndex];
            }
        }

        #endregion //listviews selection changed event

        #region canvas inking areas manipulation, position and size change
        private void canvasInkingArea_ManipulationDeltaVertical(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            Canvas inkingCanvas = sender as Canvas;
            CompositeTransform ct = inkingCanvas.RenderTransform as CompositeTransform;

            if (ct == null)
            {
                inkingCanvas.RenderTransform = ct = new CompositeTransform();
            }

            var Image = (CompositeTransform)inkingCanvas.RenderTransform;
            Point p;
            p.X = 0;
            p.Y = 0;
            Point point = Image.TransformPoint(p);
            double windowWidth = imgPageDetailVertical.ActualWidth;
            double windowHeight = imgPageDetailVertical.ActualHeight;
            double imageWidth = inkingCanvas.Width * Image.ScaleX;
            double imageHeight = inkingCanvas.Height * Image.ScaleY;

            //traslación x, y
            if ((windowWidth  > (imageWidth + point.X + e.Delta.Translation.X) &&
            (0 < (point.X + e.Delta.Translation.X))))
            {
                Image.TranslateX += e.Delta.Translation.X;
            }
            if ((windowHeight > (imageHeight + point.Y + e.Delta.Translation.Y)) &&
            (0 < (point.Y + e.Delta.Translation.Y)))
            {
                Image.TranslateY += e.Delta.Translation.Y;
            }

            //escalado
            if ((windowWidth  > Image.TranslateX + imageWidth * e.Delta.Scale) &&
            (0  < (point.Y + e.Delta.Scale)))
            {
                Image.ScaleX *= e.Delta.Scale;
            }
            if ((windowHeight  > Image.TranslateY + imageHeight * e.Delta.Scale) &&
            (0 < (point.Y + e.Delta.Scale)))
            {
                Image.ScaleY *= e.Delta.Scale;
            }

            //Line line = new Line()
            //{
            //    X1 = 0,
            //    Y1 = 0,
            //    X2 = Image.TranslateX,
            //    Y2 = Image.TranslateY,
            //    StrokeThickness = 1,
            //    Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(50, 50, 50, 50);
            //};

            //padre.Children.Add(line);

        }
        private void canvasInkingArea_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            Canvas inkingCanvas = sender as Canvas;
            CompositeTransform ct = inkingCanvas.RenderTransform as CompositeTransform;

            if (ct == null)
            {
                inkingCanvas.RenderTransform = ct = new CompositeTransform();
            }

            var Image = (CompositeTransform)inkingCanvas.RenderTransform;
            Point p;
            p.X = 0;
            p.Y = 0;
            Point point = Image.TransformPoint(p);
            double windowWidth= imgPageDetail.ActualWidth;
            double windowHeight= imgPageDetail.ActualHeight;
            double imageWidth = inkingCanvas.Width * Image.ScaleX;
            double imageHeight = inkingCanvas.Height * Image.ScaleY;

            //traslación x, y
            if ((windowWidth > (imageWidth + point.X + e.Delta.Translation.X) &&
            (0 < (point.X + e.Delta.Translation.X))))
            {
                Image.TranslateX += e.Delta.Translation.X;
            }
            if ((windowHeight  > (imageHeight + point.Y + e.Delta.Translation.Y)) &&
            (0  < (point.Y + e.Delta.Translation.Y)))
            {
                Image.TranslateY += e.Delta.Translation.Y;
            }

            //escalado
            if ((windowWidth  > Image.TranslateX + imageWidth * e.Delta.Scale) &&
            (0  < (point.Y + e.Delta.Scale)))
            {
                Image.ScaleX *= e.Delta.Scale;
            }
            if ((windowHeight  > Image.TranslateY + imageHeight * e.Delta.Scale) &&
            (0  < (point.Y + e.Delta.Scale)))
            {
                Image.ScaleY *= e.Delta.Scale;
            }

            //Line line = new Line()
            //{
            //    X1 = 0,
            //    Y1 = 0,
            //    X2 = Image.TranslateX,
            //    Y2 = Image.TranslateY,
            //    StrokeThickness = 1,
            //    Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(50, 50, 50, 50);
            //};

            //padre.Children.Add(line);

        }

        private Point GetSignaturePosition()
        {
            if (InitialOrientationState == "FullScreenPortrait")
            {
                try
                {
                    var Image = (CompositeTransform)canvasInkingAreaVertical.RenderTransform;
                    return new Point(Image.TranslateX, Image.TranslateY);
                }
                catch { }
            }
            else
            {
                try
                {
                    var Image = (CompositeTransform)canvasInkingArea.RenderTransform;
                    return new Point(Image.TranslateX, Image.TranslateY);
                }
                catch { }
            }
            return new Point(0, 0);
        }

        private WidgetPosition GetWidgetPosition()
        {
            WidgetPosition wPos = new WidgetPosition() { X = 0, Y = 0 };

            WidgetSize wSize = GetWidgetSize();

            int dinA4Width = 595;
            int dinA4Height = 842;

            if (InitialOrientationState == "FullScreenPortrait")
            {
                try
                {
                    if (imgPageDetailVertical.ActualWidth > imgPageDetailVertical.ActualHeight)
                    {
                         dinA4Width = 842;
                         dinA4Height = 595;
                    }
                    var Image = (CompositeTransform)canvasInkingAreaVertical.RenderTransform;
                   
                    wPos.X = (int)(dinA4Width * Image.TranslateX / imgPageDetailVertical.ActualWidth);
                    wPos.Y = (int)((dinA4Height * (imgPageDetailVertical.ActualHeight - Image.TranslateY)) / imgPageDetailVertical.ActualHeight) - wSize.Height;
                    return wPos;
                 
                }
                catch {
                    wPos.X = 0;
                    wPos.Y = (int)((dinA4Height * (imgPageDetailVertical.ActualHeight)) / imgPageDetailVertical.ActualHeight) - wSize.Height;
                    return wPos;
                }
            }
            else
            {
                try
                {
                    if (imgPageDetail.ActualWidth > imgPageDetail.ActualHeight)
                    {
                        dinA4Width = 842;
                        dinA4Height = 595;
                    }

                    var Image = (CompositeTransform)canvasInkingArea.RenderTransform;


                   
                    wPos.X = (int)(dinA4Width * Image.TranslateX / imgPageDetail.ActualWidth);
                    wPos.Y = (int)((dinA4Height * (imgPageDetail.ActualHeight - Image.TranslateY)) / imgPageDetail.ActualHeight) - wSize.Height;
                
                    return wPos;
                }
                catch {
                    wPos.X = 0;
                   
                    wPos.Y = (int)((dinA4Height * (imgPageDetail.ActualHeight)) / imgPageDetail.ActualHeight) - wSize.Height;
                    
                    return wPos;
                }
            }
        }

        #endregion //listviews selection changed event

        #region  canvas inking area pointer navigation events 
        private void canvasInkingArea_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerId == penID || e.Pointer.PointerId == touchID)
            {
                Canvas senderCanvas = (Canvas)sender;
                Windows.UI.Input.PointerPoint pt = e.GetCurrentPoint(senderCanvas);
                // Pass the pointer information to the InkManager. 
                inkManager.ProcessPointerUp(pt);

                //Add new point to edatalia sign points
                ApiPointTime ptApi = new ApiPointTime(pt);
                lstApiPoints.Add(ptApi);
            }

            touchID = 0;
            penID = 0;

            // Call an application-defined function to render the ink strokes.
            e.Handled = true;
        }

        public void canvasInkingArea_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerId == penID || e.Pointer.PointerId == touchID)
            {
                Canvas senderCanvas = (Canvas)sender;
                Windows.UI.Input.PointerPoint pt = e.GetCurrentPoint((UIElement)sender);
                // Pass the pointer information to the InkManager. 
                inkManager.ProcessPointerUp(pt);

                //Add new point to edatalia sign points
                ApiPointTime ptApi = new ApiPointTime(pt);
                lstApiPoints.Add(ptApi);
            }

            touchID = 0;
            penID = 0;

            // Call an application-defined function to render the ink strokes.
            e.Handled = true;
        }

        private void canvasInkingArea_PointerMoved(object sender, PointerRoutedEventArgs e)
        {

            if (e.Pointer.PointerId == penID)
            {
                Canvas senderCanvas = (Canvas)sender;
                PointerPoint pt = e.GetCurrentPoint((UIElement)sender);

                // Render a red line on the canvas as the pointer moves. 
                // Distance() is an application-defined function that tests
                // whether the pointer has moved far enough to justify 
                // drawing a new line.
                currentContactPt = pt.Position;
                x1 = previousContactPt.X;
                y1 = previousContactPt.Y;
                x2 = currentContactPt.X;
                y2 = currentContactPt.Y;

                if (Distance(x1, y1, x2, y2) > 1.0)
                {
                    Line line = new Line()
                    {
                        X1 = x1,
                        Y1 = y1,
                        X2 = x2,
                        Y2 = y2,
                        StrokeThickness = 1.0,
                        Stroke = new SolidColorBrush(Colors.Black)
                    };

                    previousContactPt = currentContactPt;

                    // Draw the line on the canvas by adding the Line object as
                    // a child of the Canvas object.
                    if (InitialOrientationState == "FullScreenPortrait") this.canvasInkingAreaVertical.Children.Add(line);
                    else this.canvasInkingArea.Children.Add(line);

                    // Pass the pointer information to the InkManager.
                    inkManager.ProcessPointerUpdate(pt);
                    
                    //Add new point to edatalia sign points
                    ApiPointTime ptApi = new ApiPointTime(pt);
                    lstApiPoints.Add(ptApi);
                    lstApiSegments.Last().AddPoint(ptApi);
                }
            }
            //todo check this
            else if (e.Pointer.PointerId == touchID)
            {
                // Process touch input
            }
        }

        private double Distance(double x1, double y1, double x2, double y2)
        {
            double d = 0;
            d = Math.Sqrt(Math.Pow((x2 - x1), 2) + Math.Pow((y2 - y1), 2));
            return d;
        }

        public void canvasInkingArea_PointerPressed(object sender, PointerRoutedEventArgs e)
        {

            Canvas senderCanvas = (Canvas)sender;
            // Get information about the pointer location.
            PointerPoint pt = e.GetCurrentPoint((UIElement)sender);
            previousContactPt = pt.Position;

            // Accept input only from a pen or mouse with the left button pressed. 
            PointerDeviceType pointerDevType = e.Pointer.PointerDeviceType;

           if (pointerDevType == PointerDeviceType.Touch ||
               pointerDevType == PointerDeviceType.Pen || 
                    pointerDevType == PointerDeviceType.Mouse &&
                    pt.Properties.IsLeftButtonPressed)
            {
                // Pass the pointer information to the InkManager.
                try
                {
                    penID = pt.PointerId;
                    inkManager.ProcessPointerDown(pt);
                }
                catch { }
                //Add new point to edatalia sign points
                ApiPointTime ptApi = new ApiPointTime(pt);
                lstApiPoints.Add(ptApi);
                //Add new segment to signature segments
                lstApiSegments.Add(new ApiSegment());
                lstApiSegments.Last().AddPoint(ptApi);

                e.Handled = true;
            }
           //else if (pointerDevType == PointerDeviceType.Touch)
           //{
           //    ApiPointTime ptApi = new ApiPointTime(pt);
           //    lstApiPoints.Add(ptApi);
           //    lstApiSegments.Add(new ApiSegment());
           //    lstApiSegments.Last().AddPoint(ptApi);
           //}
        }

        void canvasInkingArea_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            //Canvas senderCanvas = (Canvas)sender;
            //// Get information about the pointer location.
            //PointerPoint pt = e.GetCurrentPoint(senderCanvas);
            //previousContactPt = pt.Position;

            //// Accept input only from a pen or mouse with the left button pressed. 
            //PointerDeviceType pointerDevType = e.Pointer.PointerDeviceType;
            //if (pointerDevType == PointerDeviceType.Pen || pointerDevType == PointerDeviceType.Touch ||
            //        pointerDevType == PointerDeviceType.Mouse &&
            //        pt.Properties.IsLeftButtonPressed)
            //{
            //    // Pass the pointer information to the InkManager.
            //    inkManager.ProcessPointerDown(pt);
                
            //    //Add new point to edatalia sign points
            //    ApiPointTime ptApi = new ApiPointTime(pt);
            //    lstApiPoints.Add(ptApi);
            //    //Add new segment to signature segments
            //    lstApiSegments.Add(new ApiSegment());
            //    lstApiSegments.Last().AddPoint(ptApi);
            //    penID = pt.PointerId;

            //    e.Handled = true;
            //}
           
        }
        #endregion //canvas inking area pointer navigation events

       

    }
}
