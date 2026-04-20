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
using Emgu.CV.Dnn;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
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

    private Mat _prevFrame = null;
    //private BackgroundSubtractorMOG2 _diffDetector = new BackgroundSubtractorMOG2();

    // Vérifiez bien les chemins des fichiers
    private const string cfgPath = "yolov4-tiny.cfg";
    private const string weightsPath = "yolov4-tiny.weights";

    //Net _yoloNet = DnnInvoke.ReadNetFromDarknet(cfgPath, weightsPath);


    private List<string> _classNames = new List<string>();

    private int fileIndex = 0;
    private System.Timers.Timer splitTimer = new System.Timers.Timer();

    public string videoResolution { get; private set; }
    public string storagePath { get; private set; }
    public bool isUserSelection { get; private set; }

    public MainWindow()
    {
      //videoResolution = "640x480";
      this.LoadClassNames();
    }

    private void LoadClassNames()
    {
      string namesPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "coco.names");
      _classNames = File.ReadAllLines(namesPath).ToList();
    }

    private void InitializeBitmap(int width, int height)
    {
      // On initialise le bitmap sur le thread UI
      // On force l'exécution sur le thread UI
      Application.Current.Dispatcher.Invoke(() =>
      {
        _wbmp = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr24, null);
        WebcamPreview.Source = _wbmp;
      });
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

    private async void RecordNextPart()
    {
      // experimental : on pourrait faire du motion detection pour n'enregistrer que les parties avec mouvement, mais ça risque de faire rater des événements importants (ex: un intrus qui se fige pour ne pas être détecté)
      //_prevFrame = null; // Reset previous frame for motion detection

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

    private void ProcessFrame(object sender, EventArgs e)
    {
      if (_capture != null && _recording)
      {
        using Mat frame = new Mat();
        if (_capture.Retrieve(frame))
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

            this.ApplyOverlays(frame);

            // Write the frame to the video file
            if (_writer != null && _recording)
            {
              // 3. AFFICHAGE (Priorité secondaire)
              // On envoie une COPIE ou on accède aux données sur le thread UI
              Dispatcher.Invoke(new Action(() =>
              {
                UpdateDisplay(frame);
              }));

              // C'est l'étape la plus légère (quelques millisecondes)
              if (HasMotion(frame))
              {
                Console.WriteLine("-------Mouvement détecté !-------");
                // 3. IA LOCALE : Est-ce une personne ?
                // On ne lance YOLO que si quelque chose a bougé
                if (IsPersonDetected(frame))
                {
                  Console.WriteLine("----------Personne détectée !------");
                  // 4. IA CLOUD : Qui est-ce ? (Asynchrone pour ne pas bloquer)
                  // On ne l'appelle que si YOLO confirme un humain
                  //Task.Run(() => IdentifyPersonWithAWS(frame.Clone()));

                  // 5. ENREGISTREMENT
                  //if (_recording) _writer.Write(frame);
                }
              }

              _writer.Write(frame);
            }

            //_writer.Write(frame);
          }
        }
      }
    }

    private void ApplyOverlays(Mat frame)
    {
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

    bool HasMotion_V3(Mat currentFrame)
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
    }

    bool HasMotion(Mat currentFrame)
    {
      // 1. Si c'est la toute première image, on l'enregistre et on quitte
      if (_prevFrame == null)
      {
        _prevFrame = currentFrame.Clone();
        return false;
      }

      using (Mat diff = new Mat())
      using (Mat grayCurrent = new Mat())
      using (Mat grayPrev = new Mat())
      using (Mat thresholded = new Mat())
      {
        // 2. Conversion en niveaux de gris (plus rapide et précis pour le mouvement)
        CvInvoke.CvtColor(currentFrame, grayCurrent, ColorConversion.Bgr2Gray);
        CvInvoke.CvtColor(_prevFrame, grayPrev, ColorConversion.Bgr2Gray);

        // 3. Calcul de la différence absolue entre l'image actuelle et la précédente
        CvInvoke.AbsDiff(grayCurrent, grayPrev, diff);

        // 4. Seuil (Threshold) : on ne garde que les pixels ayant beaucoup changé
        CvInvoke.Threshold(diff, thresholded, 25, 255, ThresholdType.Binary);

        // 5. Compter les pixels blancs (le mouvement)
        int movementAmount = CvInvoke.CountNonZero(thresholded);

        // 6. Mise à jour de l'image précédente pour le prochain cycle
        _prevFrame.Dispose(); // Libère l'ancienne
        _prevFrame = currentFrame.Clone();

        // 7. Seuil de détection (ex: 500 pixels ont changé)
        return movementAmount > 500;
      }
    }

    bool IsPersonDetected(Mat frame)
    {
      return false; // Placeholder, à remplacer par le vrai résultat de la détection
      // Préparation de l'image pour l'IA
      using (Mat blob = DnnInvoke.BlobFromImage(frame, 1 / 255.0, new System.Drawing.Size(416, 416), new MCvScalar(), true, false))
      {
        //_yoloNet.SetInput(blob);
        VectorOfMat output = new VectorOfMat();
        //_yoloNet.Forward(output, _yoloNet.UnconnectedOutLayersNames);

        // Logique pour parcourir les résultats et vérifier si la classe "person" (ID 0) 
        // a un score de confiance > 0.5
        return false; // Placeholder, à remplacer par le vrai résultat de la détection
        //return ProcessYoloOutput(output);
      }
    }

    private bool ProcessYoloOutput(VectorOfMat output)
    {
      float confidenceThreshold = 0.5f;

      for (int i = 0; i < output.Size; i++)
      {
        float[] data = (float[])output[i].GetData();
        for (int j = 0; j < output[i].Rows; j++)
        {
          int rowOffset = j * output[i].Cols;
          float confidence = data[rowOffset + 4];

          if (confidence > confidenceThreshold)
          {
            // Trouver l'index de la classe avec le score le plus élevé
            int classId = 0;
            float maxClassScore = 0;
            for (int k = 5; k < output[i].Cols; k++)
            {
              if (data[rowOffset + k] > maxClassScore)
              {
                maxClassScore = data[rowOffset + k];
                classId = k - 5;
              }
            }

            // Utilisation de coco.names pour vérifier le nom
            if (maxClassScore > confidenceThreshold)
            {
              string label = _classNames[classId];

              if (label == "person")
              {
                return true; // Humain confirmé
              }
            }
          }
        }
      }
      return false;
    }

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

    async Task IdentifyPerson(Mat frame)
    {
      var client = new AmazonRekognitionClient("VOTRE_KEY", "VOTRE_SECRET", Amazon.RegionEndpoint.EUWest1);

      // Conversion Mat -> Bytes pour l'API
      byte[] imageBytes = frame.ToImage<Bgr, byte>().ToJpegData();

      var request = new SearchFacesByImageRequest
      {
        CollectionId = "votre_collection_famille",
        Image = new Amazon.Rekognition.Model.Image { Bytes = new MemoryStream(imageBytes) },
        MaxFaces = 1,
        FaceMatchThreshold = 70F
      };

      var response = await client.SearchFacesByImageAsync(request);
      if (response.FaceMatches.Count > 0)
      {
        string name = response.FaceMatches[0].Face.ExternalImageId;
        Console.WriteLine($"Personne reconnue : {name}");
      }
      else
      {
        Console.WriteLine("ALERTE : Inconnu détecté !");
      }
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