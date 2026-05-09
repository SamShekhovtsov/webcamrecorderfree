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
using System.Drawing;
using WebCamRecorderFree.Config;

namespace WebCamRecorderFree
{
  /// <summary>+
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    private WriteableBitmap _wbmp;

    #region ---------------- fields for video capture and recording ----------------
    
    // Declare variables globally or in a class scope
    VideoCapture _capture;
    VideoWriter _writer;
    bool _recording = false;

    private int _minutesPerPart = 120; // Duration of each video part in minutes

    private Mat _prevFrame = null;

    // Vérifiez bien les chemins des fichiers
    private const string cfgPath = "yolov4-tiny.cfg";
    private const string weightsPath = "yolov4-tiny.weights";

    Net _yoloNet = DnnInvoke.ReadNetFromDarknet(cfgPath, weightsPath);

    private List<string> _classNames = new List<string>();

    private int fileIndex = 0;
    private System.Timers.Timer splitTimer = new System.Timers.Timer();

    public string videoResolution { get; private set; }
    public string storagePath { get; private set; }
    public bool isUserSelection { get; private set; }

    private DateTime _nextDetectionAllowedAt = DateTime.MinValue;
    private readonly TimeSpan _detectionCooldown = TimeSpan.FromSeconds(7);

    #endregion

    public MainWindow()
    {
      _yoloNet.SetPreferableBackend(Emgu.CV.Dnn.Backend.OpenCV);
      _yoloNet.SetPreferableTarget(Emgu.CV.Dnn.Target.Cpu);
      //videoResolution = "640x480";
      this.LoadClassNames();
    }

    #region ---------------- Event Handlers for UI interactions ----------------
    
    private void Grid_Loaded(object sender, RoutedEventArgs e)
    {
      // Initialize the VideoCaptureCore object, connecting it to the VideoView control on the form
      ////videoCaptureCore = new VideoCaptureCore(WebCamStreamView as IVideoView);
      // Enable resizing and specify new dimensions
      ////videoCaptureCore.Video_Resize = new VideoResizeSettings(640, 480);
    }

    private async void btnStartRecording_Click(object sender, RoutedEventArgs e)
    {
      RecordNextPart();
      splitTimer.Interval = TimeSpan.FromMinutes(_minutesPerPart).TotalMilliseconds;

      splitTimer.Elapsed += async (s, e) =>
      {
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

    #endregion

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

            // Write the frame to the video file
            if (_writer != null && _recording)
            {
              this.ApplyOverlays(frame);

              bool canRunDetection = DateTime.Now >= _nextDetectionAllowedAt;

              if (canRunDetection)
              {
                // C'est l'étape la plus légère (quelques millisecondes)
                if (HasMotion(frame))
                {
                  Console.WriteLine("-------Mouvement détecté !-------");

                  List<System.Drawing.Rectangle> personBoxes = DetectPersons(frame);

                  // if personBoxes are more than 0, \
                  // it means at least one person is detected in the frame
                  if (personBoxes.Count > 0)
                  {
                    Console.WriteLine("----------Personne détectée !------");

                    DrawPersonBoxes(frame, personBoxes);

                    SaveDetectedPersonFrame(frame);

                    // Skip motion/person detection for the next 7 seconds
                    _nextDetectionAllowedAt = DateTime.Now.Add(_detectionCooldown);

                    // Optional AWS recognition later:
                    Mat frameCopyForAws = frame.Clone();
                    Task.Run(() => IdentifyPersonWithAWS(frameCopyForAws));
                  }
                }
              }
              else
              {
                Console.WriteLine("Detection skipped due to cooldown.");
                // Optional debug only
                Console.WriteLine("Detection cooldown active...");
              }

              // 3. AFFICHAGE (Priorité secondaire)
              // On envoie une COPIE ou on accède aux données sur le thread UI
              Dispatcher.Invoke(new Action(() =>
              {
                UpdateDisplay(frame);
              }));

              _writer.Write(frame);
            }
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

    private bool HasMotion(Mat currentFrame)
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

    private List<System.Drawing.Rectangle> DetectPersons(Mat frame)
    {
      using Mat blob = DnnInvoke.BlobFromImage(
          frame,
          1 / 255.0,
          new System.Drawing.Size(416, 416),
          new MCvScalar(),
          true,
          false);

      _yoloNet.SetInput(blob);

      using VectorOfMat output = new VectorOfMat();
      _yoloNet.Forward(output, _yoloNet.UnconnectedOutLayersNames);

      return ProcessYoloOutput(output, frame.Width, frame.Height);
    }

    private List<System.Drawing.Rectangle> ProcessYoloOutput(VectorOfMat output, int frameWidth, int frameHeight)
    {
      float confidenceThreshold = 0.5f;
      List<System.Drawing.Rectangle> personBoxes = new List<System.Drawing.Rectangle>();

      for (int i = 0; i < output.Size; i++)
      {
        using Mat detectionMat = output[i];

        float[,] data = (float[,])detectionMat.GetData();

        int rows = detectionMat.Rows;
        int cols = detectionMat.Cols;

        for (int j = 0; j < rows; j++)
        {
          float objectConfidence = data[j, 4];

          if (objectConfidence < confidenceThreshold)
            continue;

          int classId = -1;
          float maxClassScore = 0;

          for (int k = 5; k < cols; k++)
          {
            float classScore = data[j, k];

            if (classScore > maxClassScore)
            {
              maxClassScore = classScore;
              classId = k - 5;
            }
          }

          float finalConfidence = objectConfidence * maxClassScore;

          if (finalConfidence > confidenceThreshold && classId >= 0)
          {
            string label = _classNames[classId];

            if (label == "person")
            {
              float centerX = data[j, 0] * frameWidth;
              float centerY = data[j, 1] * frameHeight;
              float width = data[j, 2] * frameWidth;
              float height = data[j, 3] * frameHeight;

              int x = (int)(centerX - width / 2);
              int y = (int)(centerY - height / 2);

              System.Drawing.Rectangle box = new System.Drawing.Rectangle(
                  x,
                  y,
                  (int)width,
                  (int)height);

              box.Intersect(new System.Drawing.Rectangle(0, 0, frameWidth, frameHeight));

              if (box.Width > 0 && box.Height > 0)
              {
                personBoxes.Add(box);
              }
            }
          }
        }
      }

      return personBoxes;
    }

    async Task IdentifyPersonWithAWS(Mat frame)
    {
      AmazonRekognitionClient rekognitionClient = new AmazonRekognitionClient();

      // Conversion Mat -> Bytes pour l'API
      byte[] imageBytes = frame.ToImage<Bgr, byte>().ToJpegData();

      var request = new SearchFacesByImageRequest
      {
        CollectionId = "closest-family-collection",
        Image = new Amazon.Rekognition.Model.Image { Bytes = new MemoryStream(imageBytes) },
        MaxFaces = 1,
        FaceMatchThreshold = 70F
      };

      var response = await rekognitionClient.SearchFacesByImageAsync(request);
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

    private void DrawPersonBoxes(Mat frame, List<System.Drawing.Rectangle> personBoxes)
    {
      foreach (System.Drawing.Rectangle box in personBoxes)
      {
        CvInvoke.Rectangle(
            frame,
            box,
            new MCvScalar(0, 0, 255), // Red in BGR
            2);                       // Thin border
      }
    }

    private void SaveDetectedPersonFrame(Mat frame)
    {
      string folder = System.IO.Path.Combine(
          Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
          "WebCamRecorderFree",
          "DetectedPersons");

      Directory.CreateDirectory(folder);

      string filePath = System.IO.Path.Combine(
          folder,
          $"person_{DateTime.Now:yyyy_MM_dd_HH_mm_ss_fff}.jpg");

      frame.Save(filePath);
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
  }
}