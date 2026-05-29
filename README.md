# 📹 WebCam Recorder Free — Laptop Surveillance System

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](LICENSE)
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows-informational.svg)](https://dotnet.microsoft.com/)
[![Framework: .NET WPF](https://img.shields.io/badge/Framework-.NET%20WPF-purple.svg)](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/)
[![Free to Use](https://img.shields.io/badge/Free-Yes-brightgreen.svg)]()

> **A free, open-source laptop webcam surveillance recorder — built to protect travellers' safety and privacy.**

---

## 🌍 The Story Behind It

This software was born from a real-life experience. While travelling through Asia and Africa, the author noticed signs of physical harm that appeared to happen during the night in hotels and hostels. With no way to prove what happened, the idea came: *why not build a tool that silently records the laptop camera while you sleep?*

**WebCam Recorder Free** is the result — a lightweight, portable Windows application that continuously records your laptop's built-in webcam throughout the night, saving footage to compact split video files so you always have a record of what happened.

---

## Look and feel of the free webcam recorder
![home screen](/SurvellianceSystem/public/webcam_recorder_screen.jpg)
![recording...](/SurvellianceSystem/public/webcam_recorder_inaction.jpg)

---

## ✨ Key Features

| Feature | Description |
|---|---|
| 🎥 **Continuous Night Recording** | Records all night (10+ hours) with no manual intervention |
| 🗂️ **Split File Recording** | Automatically splits recordings into configurable chunks (e.g., 2-hour parts) to keep files manageable |
| 🗜️ **Compact Compression** | Uses the `MP4V` codec by default for a strong balance between file size and video quality |
| 🏃 **Motion Detection** | Detects movement in the video feed and can trigger alerts |
| 🧑 **Human Recognition** | Uses YOLOv4-Tiny to detect whether the moving object is a person |
| 👨‍👩‍👧 **Family Member Recognition** | Integrates AWS Rekognition to identify known faces and raise an alarm for unrecognised individuals |
| 🕒 **Timestamp Overlay** | Optionally burns a live timestamp into the video for forensic accuracy |
| ⚙️ **Highly Configurable** | Resolution, codec, split duration, frame rate, motion sensitivity, and storage path are all adjustable |

---

## 🖥️ Supported Video Resolutions

| Label | Resolution | Notes |
|---|---|---|
| **SD 480p** | 640 × 480 | ✅ Default — smallest file size, good for overnight recording |
| **HD 720p** | 1280 × 720 | Balanced quality and size |
| **Full HD 1080p** | 1920 × 1080 | High quality, larger files |
| **Ultra HD 4K** | 3840 × 2160 | Very large files; powerful machine recommended |
| **8K** | 7680 × 4320 | Maximum supported resolution; very large files |

> **Tip:** For long overnight recordings, **SD 480p** is recommended. It produces the most compact files while still being clearly recognisable.

---

## 🤖 AI & Detection Modes

The application supports three detection modes, selectable from the UI:

| Mode | What It Does |
|---|---|
| **Motion only** | Uses frame-differencing to detect any movement in the scene |
| **People only** | Runs YOLOv4-Tiny (bundled `yolov4-tiny.weights` / `.cfg`) to identify human figures |
| **Motion and people** | Combines both — triggers on movement and then confirms whether a person is present |

When a person is detected, the system can optionally pass the frame to **Amazon Rekognition** to check whether the detected face belongs to a registered family member. If the face is unknown, an alarm is raised.

### Motion Sensitivity Options

| Sensitivity | Behaviour |
|---|---|
| Low | Only reacts to significant movement |
| Normal | Default balanced sensitivity |
| High | Reacts to subtle changes in the scene |

---

## ⚙️ Configuration

All settings are stored in `appsettings.json` under the `SurvellianceSystemConfig` section and persist between sessions:

```json
"SurvellianceSystemConfig": {
  "StoragePath": "",               // Where video files are saved (defaults to My Videos)
  "VideoResolution": "640x480",    // Target recording resolution
  "SplitDurationMinutes": 120,     // Split recordings every N minutes (default: 2 hours)
  "CompressionCodec": "MP4V",      // Video codec for compression
  "DetectionMode": "Motion and people",
  "TargetFrameRate": 30,           // Frames per second
  "DrawTimestampOverlay": true,    // Burn timestamp into the video
  "MotionSensitivity": "Normal"    // Low / Normal / High
}
```

Settings are automatically saved whenever you change them in the UI — there is no need to edit the JSON file manually.

---

## 🚀 How to Use

1. **Download or clone** the repository and build the solution in Visual Studio (requires .NET WPF).
2. **Launch** `WebCamRecorderFree.exe` on your laptop.
3. **Choose your storage folder** — click the storage path button and pick a directory with enough free space.
4. **Select your preferred resolution** from the dropdown (SD 480p is recommended for overnight use).
5. **Set the split duration** — how many minutes each video chunk should be (default: 120 minutes).
6. **Choose a detection mode** if you want motion/person alerts in addition to recording.
7. **Press Start Recording** — the application will begin capturing from the built-in webcam.
8. **Go to sleep.** The software records silently in the background.
9. In the morning, **review the footage** in your chosen storage folder. If anything unusual is detected, the relevant clip is already split and easy to locate.

> **If you feel pain or discomfort in the morning**, review the recordings. If something unusual is visible, consider reporting it to local police or making the footage public to raise awareness and protect others.

---

## 🔄 Recording Flow (with AI Features)

```
Camera Feed
    │
    ├─► Motion Detection (frame-differencing)
    │       └─► If motion detected...
    │               └─► YOLOv4-Tiny (person detection)
    │                       └─► If person detected...
    │                               └─► AWS Rekognition (face match)
    │                                       ├─► Known family member → continue recording
    │                                       └─► Unknown person → 🚨 Alarm triggered
    │
    └─► Video Writer (split chunks, compressed, timestamped)
```

---

## 🛠️ Technical Stack

| Component | Technology |
|---|---|
| UI Framework | WPF (.NET, C#) |
| Video Capture & Processing | [Emgu CV](https://www.emgu.com/) (OpenCV wrapper for .NET) |
| Object Detection Model | YOLOv4-Tiny (`yolov4-tiny.cfg` + `yolov4-tiny.weights`) |
| Face Recognition | [Amazon Rekognition](https://aws.amazon.com/rekognition/) |
| Configuration | `appsettings.json` via `Microsoft.Extensions.Configuration` |
| Video Codec | MP4V (configurable) |

---

## 📄 License

This project is licensed under the **GNU General Public License v3.0**.  
See the [LICENSE](LICENSE) file for full details.

You are free to use, modify, and distribute this software under the terms of GPL v3.  
**This software is free to use.** It was built to protect people, not to generate profit.

