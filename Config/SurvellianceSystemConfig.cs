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
  }
}
