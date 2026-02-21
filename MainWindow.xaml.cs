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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
using Emgu.CV.Structure;

namespace WebCamRecorderFree
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
      // The main video capture object that controls the capture process
      //private VideoCaptureCore videoCaptureCore;


      // Declare variables globally or in a class scope
      VideoCapture _capture;
      VideoWriter _writer;
      bool _recording = false;

      private int fileIndex = 0;
      private System.Timers.Timer splitTimer = new System.Timers.Timer();

      public MainWindow()
      {
          InitializeComponent();
      }

     private async void btnStartRecording_Click(object sender, RoutedEventArgs e)
     {
      ////////-------------------------------------------------------/////////
      RecordNextPart();
      splitTimer.Interval = TimeSpan.FromMinutes(30).TotalMilliseconds;

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
        Mat frame = new Mat();
        _capture.Retrieve(frame);

        if (!frame.IsEmpty)
        {
          // Display the frame in a PictureBox (optional, e.g., 'imageBox1')
          // imageBox1.Image = frame.ToBitmap(); 

          // Write the frame to the video file
          _writer.Write(frame);
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

    /*private async void RecordNextPart()
    {
      var videoCaptureCameraDevice = new VideoCaptureSource(videoCaptureCore.Video_CaptureDevices()[0].Name);

      //videoCaptureCameraDevice.Format = "1280x720";
      videoCaptureCameraDevice.Format = "640x480";
      videoCaptureCameraDevice.FrameRate = new VideoFrameRate(30);
      // Select the first available video device (webcam) from the system
      videoCaptureCore.Video_CaptureDevice = videoCaptureCameraDevice;

      // Select the first available audio device (microphone) from the system
      videoCaptureCore.Audio_CaptureDevice = new AudioCaptureSource(videoCaptureCore.Audio_CaptureDevices()[0].Name);

      // Set the output file path to the user's Videos folder with "output.mp4" filename
      videoCaptureCore.Output_Filename = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), 
        String.Format(@$"output_{DateTime.Now.ToString("dd_MM_yyyy_HH_mm")}_{fileIndex}.mp4"));

      var mp4Output = new VisioForge.Core.Types.Output.MP4Output();

      //mp4Output.Video_Encoder
      mp4Output.Video_Resize = new VideoResizeSettings(640, 480);


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
  }
}