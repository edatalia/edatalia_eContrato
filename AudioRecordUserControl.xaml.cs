using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.MediaProperties;
using Windows.Storage;
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
    public sealed partial class AudioRecordUserControl : UserControl
    {
        public delegate void AudioRecordEventHandler(StorageFile file);
        public event AudioRecordEventHandler AudioRecordEvent;

        private Windows.Storage.StorageFile m_recordStorageFile;
        Windows.Media.Capture.MediaCapture m_mediaCaptureMgr;

        private StorageFolder storageFolder = null;
        private string contractID = "NoName";

        public AudioRecordUserControl()
        {
            this.InitializeComponent();
        }

        public AudioRecordUserControl(string folderID, string contractText)
        {
            contractID = folderID;
           
            this.InitializeComponent();
            tbContractData.Text = contractText;
        }


        private async void AppBarButtonStartRecording_Click(object sender, RoutedEventArgs e)
        {
            btStartRecording.Background = new SolidColorBrush(Windows.UI.Colors.Red);
            //btStartRecording.IsEnabled = false;
           // btStopRecording.Foreground = new SolidColorBrush(Windows.UI.Colors.White);
            stopRectangle.Fill = new SolidColorBrush(Windows.UI.Colors.White);
            btStopRecording.IsEnabled = true;
            
            m_mediaCaptureMgr = new Windows.Media.Capture.MediaCapture();
            var settings = new Windows.Media.Capture.MediaCaptureInitializationSettings();
            settings.StreamingCaptureMode = Windows.Media.Capture.StreamingCaptureMode.Audio;
            settings.MediaCategory = Windows.Media.Capture.MediaCategory.Other;
            //settings.AudioProcessing = (m_bRawAudioSupported && m_bUserRequestedRaw) ? Windows.Media.AudioProcessing.Raw : Windows.Media.AudioProcessing.Default;
            settings.AudioProcessing = Windows.Media.AudioProcessing.Default;
            await m_mediaCaptureMgr.InitializeAsync(settings);

            string fileName = contractID + ".mp4";

            try
            {
                storageFolder = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFolderAsync(contractID);
            }
            catch { }
            if (storageFolder!=null)
                m_recordStorageFile = await storageFolder.CreateFileAsync(fileName, Windows.Storage.CreationCollisionOption.GenerateUniqueName);
            else m_recordStorageFile = await Windows.Storage.KnownFolders.VideosLibrary.CreateFileAsync(fileName, Windows.Storage.CreationCollisionOption.GenerateUniqueName);

            //ShowStatusMessage("Create record file successful");

            MediaEncodingProfile recordProfile = null;
            recordProfile = MediaEncodingProfile.CreateM4a(Windows.Media.MediaProperties.AudioEncodingQuality.Auto);

            await m_mediaCaptureMgr.StartRecordToStorageFileAsync(recordProfile, this.m_recordStorageFile);

            pgrRecording.Visibility = Windows.UI.Xaml.Visibility.Visible;
            pgrRecording.IsActive = true;
        }

        private void AppBarButtonAcceptRecording_Click(object sender, RoutedEventArgs e)
        {
            if (AudioRecordEvent != null) AudioRecordEvent(m_recordStorageFile);

        }

       
        private async void AppBarButtonRejectRecording_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await m_mediaCaptureMgr.StopRecordAsync();
            }
            catch { }

            pgrRecording.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            pgrRecording.IsActive = false;

            btStartRecording.IsEnabled = true;
            btStartRecording.Background = new SolidColorBrush(Windows.UI.Colors.Black);
          //  btStopRecording.Foreground = new SolidColorBrush(Windows.UI.Colors.Gray);
            stopRectangle.Fill = new SolidColorBrush(Windows.UI.Colors.Gray);
            btStopRecording.IsEnabled = false;
            btAcceptRecording.IsEnabled = false;
            this.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            
            if (AudioRecordEvent != null) AudioRecordEvent(null);
        }

        private async void AppBarButtonStopRecording_Click(object sender, RoutedEventArgs e)
        {
            await m_mediaCaptureMgr.StopRecordAsync();

            pgrRecording.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            pgrRecording.IsActive = false;

            btStartRecording.IsEnabled = true;
            btStartRecording.Background = new SolidColorBrush(Windows.UI.Colors.Black);
            stopRectangle.Fill = new SolidColorBrush(Windows.UI.Colors.Gray);
           // btStopRecording.Foreground = new SolidColorBrush(Windows.UI.Colors.Gray);
            btStopRecording.IsEnabled = false;
            btAcceptRecording.IsEnabled = true;

            var stream = await m_recordStorageFile.OpenAsync(Windows.Storage.FileAccessMode.Read);
            playbackElement.AutoPlay = true;
            playbackElement.SetSource(stream, this.m_recordStorageFile.FileType);
            playbackElement.Play();
        }
    }
}
