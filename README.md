## Visualizing Functional Scene Graphs (Mixed Reality Project)

Brief README for the Mixed Reality Unity project in this repository.

### Overview

This repository contains a Unity Mixed Reality (MR) project used for Functional scene graphs in VR scenes (Kitchen, Bathroom and Living Room).

### Contents

- `Assets/` - Unity project assets (scenes, prefabs, scripts, materials)
- `ProjectSettings/` - Unity project settings
- `Library/`, `Temp/`, etc. - local/generated folders (ignored)

### Prerequisites

- Unity editor 2022.3.62f1
- git-lfs
- Meta XR All-in-one SDK

### Build & Deploy to Meta Quest 3 (basic)

Follow these minimal steps to get the project running on a Meta Quest 3.

1. Prerequisites
	- Enable Developer Mode for your Meta account and device (Meta Quest mobile app > Menu > Devices > Developer Mode).
	- A USB-C cable to connect the headset to your machine.
	- Unity installed with Android Build Support (SDK/NDK & OpenJDK via Unity Hub).

2. Switch platform to Android
	- Open the project in Unity.
	- Go to File > Build Settings.
	- Select Android and click “Switch Platform”.

3. XR and Player settings (typical defaults)
	- Edit > Project Settings > XR Plug-in Management: enable Oculus/Meta XR for Android.
	- Player > Other Settings:
	  - Scripting Backend: IL2CPP
	  - Target Architectures: ARM64 (required for Quest)
	  - Minimum API Level: Android 10 (API 29) or higher
	- Player > Identification:
	  - Set Company Name and a unique Package Name (e.g., com.example.fsg).

4. Build & Run
	- Put the headset on and accept the USB debugging prompts.
	- In File > Build Settings, click “Build And Run”. Unity will build an APK and install it on the headset.
	- On the headset, find the app under Library > Unknown Sources (or Apps list, depending on OS version).

Optional: Sideload with ADB
```bash
adb devices             # verify the headset is detected
adb install -r <path-to-built-apk>   # replace with your APK path
```

If you encounter XR initialization or plugin issues, verify XR Plug-in Management settings and ensure the Meta/Oculus provider is enabled for Android.

### Authors

- Kaixian Qu
- Manthan Patel
- Junze He 