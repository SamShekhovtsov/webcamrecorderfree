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
using System.Xml;
using VisioForge.Core.MediaBlocks.Sinks;
using VisioForge.Core.Types;
using VisioForge.Core.Types.Output;
using VisioForge.Core.Types.VideoCapture;
using VisioForge.Core.Types.X.AudioEncoders;
using VisioForge.Core.Types.X.Output;
using VisioForge.Core.Types.X.Sinks;
using VisioForge.Core.Types.X.VideoEncoders;
// Import VisioForge libraries for video capture functionality
using VisioForge.Core.VideoCapture;
using VisioForge.Core.VideoCaptureX;
using WebCamRecorderFree.Config;

namespace WebCamRecorderFree
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    private bool isUserSelection = false;
    private string storagePath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

    // (SD) 480p (640x480)
    // (HD) 720p(1280x720)
    // Full HD (FHD) 1080p (1920x1080)
    // Ultra HD (UHD) 4K (3840x2160)
    // 8K (7680x4320)
    private string videoResolution = "640x480";

    private readonly IConfiguration configurationSettings;
    // The main video capture object that controls the capture process
    private VideoCaptureCore videoCaptureCore;

    private int fileIndex = 0;
    private System.Timers.Timer splitTimer = new System.Timers.Timer();

    public MainWindow()
    {
      InitializeComponent();

      var builder = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

      configurationSettings = builder.Build();

      var cctvConfig = new SurvellianceSystemConfig();
      configurationSettings.GetSection("SurvellianceSystemConfig").Bind(cctvConfig);

      if(!string.IsNullOrWhiteSpace(cctvConfig.StoragePath))
      {
        storagePath = cctvConfig.StoragePath;
      } 

      if(!string.IsNullOrWhiteSpace(cctvConfig.VideoResolution))
      {
        isUserSelection = false;
        videoResolution = cctvConfig.VideoResolution;
        //cmbxTargetResolution.SelectedItem = videoResolution;
        //cmbxTargetResolution.Text = videoResolution;
        //cmbxTargetResolution.SelectedValue = videoResolution;

        foreach(var itm in cmbxTargetResolution.Items)
        {
          if ((itm as ComboBoxItem).Content as string == videoResolution)
          {
            cmbxTargetResolution.SelectedItem = itm;
          }
        }
        isUserSelection = true;
      }

      if(string.IsNullOrEmpty(videoResolution))
      {
        videoResolution = "640x480";
      }

      lblVideoStoragePath.Content = $"Video Storage Path: {storagePath}";
    }

      private async void btnStartRecording_Click(object sender, RoutedEventArgs e)
      {
        RecordNextPart();
        splitTimer.Interval = TimeSpan.FromMinutes(30).TotalMilliseconds;

        splitTimer.Elapsed += async (s, e) =>
        {
          await videoCaptureCore.StopAsync();
          RecordNextPart();
        };

        splitTimer.Start();

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

    private async void RecordNextPart()
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
    }

    private void Grid_Loaded(object sender, RoutedEventArgs e)
    {           
      // Initialize the VideoCaptureCore object, connecting it to the VideoView control on the form
      videoCaptureCore = new VideoCaptureCore(WebCamStreamView as IVideoView);
      // Enable resizing and specify new dimensions
      string[] resolution = videoResolution.Split('x');
      videoCaptureCore.Video_Resize = new VideoResizeSettings(Convert.ToInt32(resolution[0]),
        Convert.ToInt32(resolution[1]));
    }

    private async void StopRecording_Click(object sender, RoutedEventArgs e)
    {
      // Stop the capture process asynchronously and finalize the output file
      await videoCaptureCore.StopAsync();
      splitTimer.Stop();
    }

    private void btnStoragePath_Click(object sender, RoutedEventArgs e)
    {
      var selectCCTVStorageDirectoryDialog = new FolderBrowserDialog();
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
      }
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