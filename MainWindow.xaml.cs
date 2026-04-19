//using VisioForge.Core.MediaBlocks.Sinks;
//using VisioForge.Core.Types;
//using VisioForge.Core.Types.Output;
//using VisioForge.Core.Types.VideoCapture;
//using VisioForge.Core.Types.X.AudioEncoders;
//using VisioForge.Core.Types.X.Output;
//using VisioForge.Core.Types.X.Sinks;
//using VisioForge.Core.Types.X.VideoEncoders;

// Import VisioForge libraries for video capture functionality
//using VisioForge.Core.VideoCapture;
//using VisioForge.Core.VideoCaptureX;

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WebCamRecorderFree.Config;

namespace WebCamRecorderFree
{
    /// <summary>+
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
      // The main video capture object that controls the capture process
      //private VideoCaptureCore videoCaptureCore;

      private WriteableBitmap _wbmp;

      // Declare variables globally or in a class scope
      VideoCapture _capture;
      VideoWriter _writer;
      bool _recording = false;

      private int _minutesPerPart = 120; // Duration of each video part in minutes

    private int fileIndex = 0;
    private System.Timers.Timer splitTimer = new System.Timers.Timer();

    public string videoResolution { get; private set; }
    public string storagePath { get; private set; }
    public bool isUserSelection { get; private set; }

    public MainWindow()
    {
      //videoResolution = "640x480";
    }
    
    private void InitializeBitmap(int width, int height)
    {
      // On initialise le bitmap sur le thread UI
      _wbmp = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr24, null);
      WebcamPreview.Source = _wbmp;
    }

    private async void btnStartRecording_Click(object sender, RoutedEventArgs e)
     {
      ////////-------------------------------------------------------/////////
      RecordNextPart();
      splitTimer.Interval = TimeSpan.FromMinutes(_minutesPerPart).TotalMilliseconds;

      splitTimer.Elapsed += async (s, e) =>
      {
        //await videoCaptureCore.StopAsync();
        if (_recording)
        {
          _recording = false;
          _capture.Stop();
          _capture.Dispose();
          _writer.Dispose(); // Important: release the writer to finalize the file
        }

        RecordNextPart();
      };

      splitTimer.Start();

      /////////------------------------------------------------------//////////

      //split video by parts:
      /*var h264 = new OpenH264EncoderSettings();
      var aac = new VOAACEncoderSettings();

      // Create split sink settings with filename pattern
      var splitSettings = new MP4SplitSinkSettings("recording_%05d.mp4");

      // Split when file reaches 100 MB (104857600 bytes)
      //splitSettings.SplitFileSize = 104857600;
      splitSettings.SplitFileSize = 2147483648;

      // Disable duration-based splitting (default is 1 minute)
      splitSettings.SplitDuration = TimeSpan.Zero;

      // Create output block
      var mp4OutputBlock = new MP4OutputBlock(splitSettings, h264, aac);
                                 
      videoCaptureCore.Output_Format = mp4OutputBlock;

      // Set the mode to VideoCapture for capturing both video and audio
      videoCaptureCore.Mode = VideoCaptureMode.VideoCapture;

      // Start the capture process asynchronously
      await videoCaptureCore.StartAsync();  */
    }

    private void ProcessFrame(object sender, EventArgs e)
    {
      if (_capture != null && _recording)
      {
        using Mat frame = new Mat();
        if(_capture.Retrieve(frame))
        {
          if (!frame.IsEmpty)
          {
            // Display the frame in a PictureBox (optional, e.g., 'imageBox1')
            // imageBox1.Image = frame.ToBitmap(); 
            // show the preview in the UI
            /*Dispatcher.Invoke(() =>
            {
              // Conversion directe grâce au package Emgu.CV.Wpf
              WebcamPreview.Source = frame.ToBitmapSource();
            });*/

            // 2. ÉCRITURE DANS LE FICHIER (Priorité haute)
            // On écrit dans le fichier sur le thread de capture pour éviter 
            // tout décalage lié aux ralentissements de l'interface graphique.

            // 1. Préparer le texte (Date et Heure actuelle)
            string timestamp = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");

            // 2. Dessiner le texte sur le 'Mat'
            // Paramètres : image, texte, position (x,y), police, échelle, couleur, épaisseur
            CvInvoke.PutText(
                frame,
                timestamp,
                new System.Drawing.Point(10, 30), // Position en haut à gauche
                FontFace.HersheySimplex,
                0.6,                             // Taille du texte
                new MCvScalar(77, 77, 255),        // Couleur Rouge (BGR)
                2                                // Épaisseur
            );

            // Write the frame to the video file
            if (_writer != null && _recording)
            {
              // 3. AFFICHAGE (Priorité secondaire)
              // On envoie une COPIE ou on accède aux données sur le thread UI
              Dispatcher.Invoke(new Action(() =>
              {
                UpdateDisplay(frame);
              }));

              _writer.Write(frame);
            }

            //_writer.Write(frame);
          }
        }          
      }
    }

    private async void RecordNextPart()
    {
      // Initialize capture (0 for default camera)
      _capture = new VideoCapture(0);

      // Get the frame width and height from the capture device
      int frameWidth = _capture.Width;
      int frameHeight = _capture.Height;
      int fps = (int)_capture.Get(Emgu.CV.CvEnum.CapProp.Fps);

      this.InitializeBitmap(frameWidth, frameHeight);

      if (fps == 0) fps = 30; // Default to 30 FPS if the property is not available

      // Define the output file path and codec (e.g., "output.avi", MP4V or XVID codec)
      string outputPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), 
        String.Format(@$"output_{DateTime.Now.ToString("dd_MM_yyyy_HH_mm")}_{fileIndex}.mp4"));
      //string outputPath = "webcam_output.avi"; // Ensure directory exists

      // Use CvInvoke.CV_FOURCC to specify the codec
      // 'M', 'P', '4', 'V' for .mp4, 'X', 'V', 'I', 'D' for .avi are common options
      int fourCC = VideoWriter.Fourcc('M', 'P', '4', 'V');

      // Initialize VideoWriter
      _writer = new VideoWriter(outputPath, fourCC, fps, new System.Drawing.Size(frameWidth, frameHeight), true);

      // Start capturing frames and hook up the frame processing event
      _capture.ImageGrabbed += ProcessFrame;
      _capture.Start();
      _recording = true;
    }

    private void UpdateDisplay(Mat frame)
    {
      if (_wbmp == null || _wbmp.PixelWidth != frame.Width)
      {
        _wbmp = new WriteableBitmap(frame.Width, frame.Height, 96, 96, PixelFormats.Bgr24, null);
        WebcamPreview.Source = _wbmp;
      }

      _wbmp.Lock();
      _wbmp.WritePixels(
          new Int32Rect(0, 0, frame.Width, frame.Height),
          frame.DataPointer,
          frame.Step * frame.Height,
          frame.Step);
      _wbmp.Unlock();
    }

    /*private async void RecordNextPart()
    {
      var videoCaptureCameraDevice = new VideoCaptureSource(videoCaptureCore.Video_CaptureDevices()[0].Name);

      //videoCaptureCameraDevice.Format = "1280x720";
      videoCaptureCameraDevice.Format = videoResolution;
      videoCaptureCameraDevice.FrameRate = new VideoFrameRate(30);
      // Select the first available video device (webcam) from the system
      videoCaptureCore.Video_CaptureDevice = videoCaptureCameraDevice;

      // Select the first available audio device (microphone) from the system
      videoCaptureCore.Audio_CaptureDevice = new AudioCaptureSource(videoCaptureCore.Audio_CaptureDevices()[0].Name);

      // Set the output file path to the user's Videos folder with "output.mp4" filename
      videoCaptureCore.Output_Filename = System.IO.Path.Combine(storagePath, 
        String.Format(@$"output_{DateTime.Now.ToString("dd_MM_yyyy_HH_mm")}_{fileIndex}.mp4"));

      var mp4Output = new VisioForge.Core.Types.Output.MP4Output();

      //mp4Output.Video_Encoder
      string[] resolution = videoResolution.Split('x');
      mp4Output.Video_Resize = new VideoResizeSettings(Convert.ToInt32(resolution[0]),
        Convert.ToInt32(resolution[1]));

      //if (mp4Output.Video. is H264EncoderSettings h264)
      //{
      //  h264.Width = 640;
      //  h264.Height = 480;
      // Adjust other settings like Bitrate if needed
      //}

      // Configure output format as MP4 with default settings (H.264 video, AAC audio)
      videoCaptureCore.Output_Format = mp4Output;


      // Set the mode to VideoCapture for capturing both video and audio
      videoCaptureCore.Mode = VideoCaptureMode.VideoCapture;

      // Start the capture process asynchronously
      await videoCaptureCore.StartAsync();

      fileIndex++;
    } */

    private void Grid_Loaded(object sender, RoutedEventArgs e)
    {           
      // Initialize the VideoCaptureCore object, connecting it to the VideoView control on the form
      ////videoCaptureCore = new VideoCaptureCore(WebCamStreamView as IVideoView);
      // Enable resizing and specify new dimensions
      ////videoCaptureCore.Video_Resize = new VideoResizeSettings(640, 480);
    }

    private async void StopRecording_Click(object sender, RoutedEventArgs e)
    {
      // Stop the capture process asynchronously and finalize the output file
      /////await videoCaptureCore.StopAsync();
      
      if (_recording)
      {
        _recording = false;
        _capture.Stop();
        _capture.Dispose();
        _writer.Dispose(); // Important: release the writer to finalize the file
      }

      splitTimer.Stop();
    }

    private void btnStoragePath_Click(object sender, RoutedEventArgs e)
    {
      /*var selectCCTVStorageDirectoryDialog = new System.Windows.FolderBrowserDialog();
      // Optional: Set a description at the top of the dialog
      selectCCTVStorageDirectoryDialog.Description = "Select the destination folder to save CCTV videos.";
      // Optional: Set the initial directory
      selectCCTVStorageDirectoryDialog.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

      DialogResult result = selectCCTVStorageDirectoryDialog.ShowDialog();

      if (result == System.Windows.Forms.DialogResult.OK)
      {
        // Get the selected folder path
        storagePath = selectCCTVStorageDirectoryDialog.SelectedPath;
        UpdateAppSettingSection(section =>
        {
          section.StoragePath = storagePath;
          section.VideoResolution = videoResolution;
        });
        System.Windows.Forms.MessageBox.Show($"Selected folder: {storagePath}");
        // Use the folder path for your application logic

        lblVideoStoragePath.Content = $"Video Storage Path: {storagePath}";
      }    */
    }

    public void UpdateAppSettingSection(Action<SurvellianceSystemConfig> updateAction)
    {
      var jsonFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

      try
      {
        // 1. Load and deserialize the entire file
        var json = File.ReadAllText(jsonFile);
        var appSettings = JsonConvert.DeserializeObject<Configuration>(json);

        if (appSettings?.SurvellianceSystemConfig != null)
        {
          // 2. Apply the specific updates to the object
          updateAction(appSettings.SurvellianceSystemConfig);

          // 3. Serialize the updated object and overwrite the file
          var updatedJson = JsonConvert.SerializeObject(appSettings, Newtonsoft.Json.Formatting.Indented);
          File.WriteAllText(jsonFile, updatedJson);
        }
      }
      catch (Exception ex)
      {
        // Handle exceptions (e.g., file not found, JSON parsing error)
        System.Windows.MessageBox.Show($"Error updating app settings: {ex.Message}");
      }
    }

    private void cmbxTargetResolution_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (!isUserSelection)
      {
        return;
      }
      
      if (cmbxTargetResolution.SelectedItem != null)
      {
        // Get the selected folder path
        videoResolution = (cmbxTargetResolution.SelectedItem as ComboBoxItem).Content as string;
        UpdateAppSettingSection(section =>
        {
          section.StoragePath = storagePath;
          section.VideoResolution = videoResolution;
        });
      }
    }
  }
}