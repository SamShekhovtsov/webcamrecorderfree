using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebCamRecorderFree.Config
{
  public class SurvellianceSystemConfig
  {
    public string StoragePath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
    public string VideoResolution { get; set; }  = "640x480";
    public int SplitDurationMinutes { get; set; } = 120;
    public string CompressionCodec { get; set; } = "MP4V";
    public string DetectionMode { get; set; } = "Motion and people";
    public int TargetFrameRate { get; set; } = 30;
    public bool DrawTimestampOverlay { get; set; } = true;
    public string MotionSensitivity { get; set; } = "Normal";
  }
}
