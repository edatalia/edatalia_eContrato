using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.ShareTarget;


using System.Net.Http;
using Windows.UI.ApplicationSettings;
using Edatalia_signplyRT.Ayuntamiento;
using Windows.Storage;
using Windows.ApplicationModel.Resources;
using Edatalia_signplyRT.Model;
using Newtonsoft.Json;
using System.Threading.Tasks;

// The Blank Application template is documented at http://go.microsoft.com/fwlink/?LinkId=234227

namespace Edatalia_signplyRT
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {

        public static Uri uri = new Uri("https://editaliawebapi.azurewebsites.net/");
        // Uri uri = new Uri("http://172.16.31.58:44300/");
        public static HttpClient httpClient = new HttpClient();

        public static Guid appID;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }


        public static bool PostActivityLog(ActivityLog log)
        {
            string jsonClient = JsonConvert.SerializeObject(log);
            var content = new StringContent(jsonClient, System.Text.Encoding.UTF8, "application/json");
            
            try
            {
                var res = App.httpClient.PostAsync(App.uri + "api/ActivityLogs", content);
            }
            catch (Exception ex)
            {
                string s = ex.ToString();
            }
            return true;
        }

        private bool AppMonitorUpdate(Guid appId)
        {
            AppMonitor appMonitor = new AppMonitor();
            appMonitor.AppMonitorGuid = appId;

            string jsonClient = JsonConvert.SerializeObject(appMonitor);
            var content = new StringContent(jsonClient, System.Text.Encoding.UTF8, "application/json");


            try
            {
                var res = App.httpClient.PostAsync(App.uri + "api/AppMonitors", content);
               // res.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                string s = ex.ToString();
            }
            return true;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override  void OnLaunched(LaunchActivatedEventArgs e)
        {
            appID = Guid.Empty;
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("AppID"))
            {
                appID = new Guid(ApplicationData.Current.LocalSettings.Values["AppID"].ToString());
                 AppMonitorUpdate(appID);
            }
            else 
            {
                appID = Guid.NewGuid();
                ApplicationData.Current.LocalSettings.Values["AppID"] = appID.ToString();
                AppMonitorUpdate(appID);
            }

            ActivityLog log = new ActivityLog(){ActivityLogDescription="App OnLaunched", AppGuid= appID};
            PostActivityLog(log);

            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("Language"))
            {
                string language = (string)ApplicationData.Current.LocalSettings.Values["Language"];
                if (language == "Español")
                {
                    Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "es-ES";
                }
                else if (language == "English")
                {
                    Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "en-US";
                }

                else Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "eu-ES";
               
            }
            else
            {
                ApplicationData.Current.LocalSettings.Values["Language"] = "Euskera";
                Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "eu-ES";

            }

#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();
                // Set the default language
                rootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter

                if (ApplicationData.Current.LocalSettings.Values.ContainsKey("SelectedDemo"))
                {
                    if (ApplicationData.Current.LocalSettings.Values["SelectedDemo"].ToString() == "Ayuntamiento")
                        rootFrame.Navigate(typeof(InitPageAyuntamiento), e.Arguments);
                    else rootFrame.Navigate(typeof(InitPage), e.Arguments);
                }
                else //por defecto, ejecuta la demo aseguradora!
                {
                    ApplicationData.Current.LocalSettings.Values["SelectedDemo"] = "Aseguradora";
                    rootFrame.Navigate(typeof(InitPage), e.Arguments);
                }
                //rootFrame.Navigate(typeof(InitPage), e.Arguments);
                //rootFrame.Navigate(typeof(InitPageAyuntamiento), e.Arguments);
            }
            // Ensure the current window is active
            Window.Current.Activate();
        }


        protected override void OnShareTargetActivated(ShareTargetActivatedEventArgs args)
        {
            // Code to handle activation goes here.	
            ShareOperation shareOperation = args.ShareOperation;

            //if (shareOperation.Data.Contains(StandardDataFormats.))
            //{
            //    // Code to process HTML goes here.
            //}

            var rootFrame = new Frame();
            rootFrame.Navigate(typeof(MainPage), args.ShareOperation);
            Window.Current.Content = rootFrame;
            Window.Current.Activate();

        }


        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        protected override void OnWindowCreated(WindowCreatedEventArgs args)
        {
            SettingsPane.GetForCurrentView().CommandsRequested += OnCommandsRequested;
            base.OnWindowCreated(args);
        }

        private void OnCommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            ResourceLoader rloader = new ResourceLoader();
            string strSettings = rloader.GetString("strSettings");
            string strLanguageSettings = rloader.GetString("strLanguageSettings");
            args.Request.ApplicationCommands.Add(new SettingsCommand(strSettings, strSettings, (handler) => ShowLogoSettingFlyout()));
            args.Request.ApplicationCommands.Add(new SettingsCommand(strLanguageSettings, strLanguageSettings, (handler) => ShowLanguageSettingFlyout()));
        }

        private void ShowLogoSettingFlyout()
        {
            var flyout = new LogoSettingsFlyout();
            flyout.Show();
        }

        private void ShowLanguageSettingFlyout()
        {
            var flyout = new LanguageSettingsFlyout();
            flyout.Show();
        }
    }
}
