using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

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

namespace WebCamRecorderFree
{
  // The main video capture object that controls the capture process
  //private VideoCaptureCore videoCaptureCore;

  //private BackgroundSubtractorMOG2 _diffDetector = new BackgroundSubtractorMOG2();

  internal class PrevLegacyCode
  {
    private async void btnStartRecording_Click(object sender, RoutedEventArgs e)
    {
      ////////-------------------------------------------------------/////////
      //RecordNextPart();
      //splitTimer.Interval = TimeSpan.FromMinutes(_minutesPerPart).TotalMilliseconds;

      //splitTimer.Elapsed += async (s, e) =>
      //{
      //  //await videoCaptureCore.StopAsync();
      //  if (_recording)
      //  {
      //    _recording = false;
      //    _capture.Stop();
      //    _capture.Dispose();
      //    _writer.Dispose(); // Important: release the writer to finalize the file
      //  }

      //  RecordNextPart();
      //};

      //splitTimer.Start();

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

    private async void RecordNextPart()
    {
      // experimental : on pourrait faire du motion detection pour n'enregistrer que les parties avec mouvement, mais ça risque de faire rater des événements importants (ex: un intrus qui se fige pour ne pas être détecté)
      //_prevFrame = null; // Reset previous frame for motion detection

      // Initialize capture (0 for default camera)
      //_capture = new VideoCapture(0);

      //// Get the frame width and height from the capture device
      //int frameWidth = _capture.Width;
      //int frameHeight = _capture.Height;
      //int fps = (int)_capture.Get(Emgu.CV.CvEnum.CapProp.Fps);

      //this.InitializeBitmap(frameWidth, frameHeight);

      //if (fps == 0) fps = 30; // Default to 30 FPS if the property is not available

      // Define the output file path and codec (e.g., "output.avi", MP4V or XVID codec)
      //string outputPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
      //  String.Format(@$"output_{DateTime.Now.ToString("dd_MM_yyyy_HH_mm")}_{fileIndex}.mp4"));
      //string outputPath = "webcam_output.avi"; // Ensure directory exists

      // Use CvInvoke.CV_FOURCC to specify the codec
      // 'M', 'P', '4', 'V' for .mp4, 'X', 'V', 'I', 'D' for .avi are common options
      //int fourCC = VideoWriter.Fourcc('M', 'P', '4', 'V');

      // Initialize VideoWriter
      //_writer = new VideoWriter(outputPath, fourCC, fps, new System.Drawing.Size(frameWidth, frameHeight), true);

      // Start capturing frames and hook up the frame processing event
      //_capture.ImageGrabbed += ProcessFrame;
      //_capture.Start();
      //_recording = true;
    }


    private void ProcessFrame(object sender, EventArgs e)
    {
      //if (_capture != null && _recording)
      //{
      //  using Mat frame = new Mat();
      //  if (_capture.Retrieve(frame))
      //  {
      //    if (!frame.IsEmpty)
      //    {
      //      // Display the frame in a PictureBox (optional, e.g., 'imageBox1')
      //      // imageBox1.Image = frame.ToBitmap(); 
      //      // show the preview in the UI
      //      /*Dispatcher.Invoke(() =>
      //      {
      //        // Conversion directe grâce au package Emgu.CV.Wpf
      //        WebcamPreview.Source = frame.ToBitmapSource();
      //      });*/

      //      // 2. ÉCRITURE DANS LE FICHIER (Priorité haute)
      //      // On écrit dans le fichier sur le thread de capture pour éviter 
      //      // tout décalage lié aux ralentissements de l'interface graphique.

      //      this.ApplyOverlays(frame);

      //      // Write the frame to the video file
      //      if (_writer != null && _recording)
      //      {
      //        // 3. AFFICHAGE (Priorité secondaire)
      //        // On envoie une COPIE ou on accède aux données sur le thread UI
      //        Dispatcher.Invoke(new Action(() =>
      //        {
      //          UpdateDisplay(frame);
      //        }));

      //        // C'est l'étape la plus légère (quelques millisecondes)
      //        if (HasMotion(frame))
      //        {
      //          Console.WriteLine("-------Mouvement détecté !-------");

      //          List<System.Drawing.Rectangle> personBoxes = DetectPersons(frame);

      //          // if personBoxes are more than 0, \
      //          // it means at least one person is detected in the frame
      //          if (personBoxes.Count > 0)
      //          {
      //            Console.WriteLine("----------Personne détectée !------");

      //            DrawPersonBoxes(frame, personBoxes);

      //            SaveDetectedPersonFrame(frame);

      //            // Optional AWS recognition later:
      //            // Task.Run(() => IdentifyPersonWithAWS(frame.Clone()));
      //          }
      //        }

      //        _writer.Write(frame);
      //      }

      //      //_writer.Write(frame);
      //    }
      //  }
      //}
    }

    /*private bool HasMotionV2Simple(Mat frame)
    {
      using (Mat fgMask = new Mat())
      {
        _diffDetector.Apply(frame, fgMask);
        // Compte les pixels blancs (mouvement)
        int nonZeroPixels = CvInvoke.CountNonZero(fgMask);
        return nonZeroPixels > 5000; // Seuil à ajuster selon la sensibilité voulue
      }
    } */

    /*bool HasMotion_V3(Mat currentFrame)
    {
      if (_prevFrame == null)
      {
        _prevFrame = currentFrame.Clone();
        return false;
      }

      using (Mat grayCurrent = new Mat())
      using (Mat grayPrev = new Mat())
      using (Mat diff = new Mat())
      using (Mat thresh = new Mat())
      {
        CvInvoke.CvtColor(currentFrame, grayCurrent, ColorConversion.Bgr2Gray);
        CvInvoke.CvtColor(_prevFrame, grayPrev, ColorConversion.Bgr2Gray);

        CvInvoke.AbsDiff(grayCurrent, grayPrev, diff);
        CvInvoke.Threshold(diff, thresh, 25, 255, ThresholdType.Binary);

        int count = CvInvoke.CountNonZero(thresh);

        _prevFrame.Dispose();
        _prevFrame = currentFrame.Clone();

        return count > 1000; // Ajustez ce seuil selon vos besoins
      }
    }  */

    /*bool IsPersonDetected(Mat frame)
    {
      //return false; // Placeholder, à remplacer par le vrai résultat de la détection
      // Préparation de l'image pour l'IA
      using (Mat blob = DnnInvoke.BlobFromImage(frame, 1 / 255.0, new System.Drawing.Size(416, 416), new MCvScalar(), true, false))
      {
        _yoloNet.SetInput(blob);
        VectorOfMat output = new VectorOfMat();
        _yoloNet.Forward(output, _yoloNet.UnconnectedOutLayersNames);

        // Logique pour parcourir les résultats et vérifier si la classe "person" (ID 0) 
        // a un score de confiance > 0.5
        //return false; // Placeholder, à remplacer par le vrai résultat de la détection
        return ProcessYoloOutput(output);
      }
    } */

    private bool ProcessYoloOutput_prev_V01(VectorOfMat output)
    {
      float confidenceThreshold = 0.5f; // On ignore en dessous de 50% de certitude

      for (int i = 0; i < output.Size; i++)
      {
        Mat outItem = output[i];
        // outItem est une matrice où chaque ligne est une détection
        // Les colonnes : [0-3] = position/taille, [4] = confiance, [5...] = scores par classe
        float[] data = (float[])outItem.GetData();

        for (int j = 0; j < outItem.Rows; j++)
        {
          int rowOffset = j * outItem.Cols;
          float confidence = data[rowOffset + 4];

          if (confidence > confidenceThreshold)
          {
            // La classe "Person" est l'index 0 dans le fichier coco.names
            float personScore = data[rowOffset + 5];
            if (personScore > confidenceThreshold)
            {
              return true; // Une personne est détectée !
            }
          }
        }
      }
      return false;
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
  }
}
