# Unity Android Icon Generator

[![Unity](https://img.shields.io/badge/Unity-6000.0%2B-blue.svg)](https://unity.com/)

An Editor-only Unity package that turns a single logo or icon asset into the best-fit Android launcher icon formats: **Legacy**, **Round**, and **Adaptive** (separate foreground and background layers). It provides live previews with mask guides, exports four optimized PNG assets, and assigns them to Unity's Android Player Settings.

Open it directly in Unity from: **Tools > Android Icon Generator**

---

## 🚀 Installation

### Option 1: Install via Git URL in Unity Package Manager (Recommended)

1. Open your Unity project.
2. In the top menu, go to **Window > Package Manager**.
3. In the Package Manager window, click the **`+`** (plus) icon in the top-left corner.
4. Select **Add package from git URL...**.
5. Paste the following URL:
   ```text
   https://github.com/mehul-gameyogi/AndroidIconGanerator.git
   ```
6. Click **Add**. Unity will automatically download, resolve, and install the package.

> **Tip:** You can target a specific branch or version by appending `#main` or `#1.0.0`:
> ```text
> https://github.com/mehul-gameyogi/AndroidIconGanerator.git#main
> ```

---

### Option 2: Install via `Packages/manifest.json`

You can also install the package by adding it directly to your project's `Packages/manifest.json` file:

```json
{
  "dependencies": {
    "com.tools.android-icon-generator": "https://github.com/mehul-gameyogi/AndroidIconGanerator.git",
    ...
  }
}
```
When you switch back to Unity, it will automatically download and resolve the dependency.

---

### Option 3: Local Installation (From Disk)

1. Clone or download this repository locally.
2. In Unity, open **Window > Package Manager**.
3. Click the **`+`** icon and select **Add package from disk...**.
4. Navigate to the folder and select the `package.json` file.

---

## ✨ Features

- **All Android Icon Formats**:
  - **Legacy Icons** (48dp standard square / shaped)
  - **Round Icons** (circular crop for devices using round icons)
  - **Adaptive Icons** (108dp separate foreground & background layers)
  - Best-fit output: **192x192** legacy/round and **432x432** adaptive foreground/background.
- **Live Interactive Previews**:
  - Instant real-time preview without writing temporary files.
  - Interactive mask overlays: Circle, Squircle, Rounded Square.
  - Visual guides for **66dp Safe Zone** (green) and **72dp Visible Area** (yellow).
- **Export & Asset Management**:
  - All four PNGs are saved together in one folder.
  - Optional save folder with a **Browse...** picker; disable it to save directly in `Assets`.
  - Safe overwrite verification with progress tracking.
- **1-Click Player Settings Integration**:
  - Automatically assigns generated icons directly via `PlayerSettings.SetPlatformIcons`.
  - Built-in validation that checks each slot and verifies consistency.
- **Zero Runtime Overhead**:
  - Strictly Editor-only code inside `AndroidIconGenerator.Editor` assembly definition — never bloats your final game build.

---

## 📋 Requirements

- **Unity 6** (`6000.0` or newer).
- **Android Build Support** module installed in Unity Hub (required for automatic Player Settings icon assignment).

---

## 🛠️ How to Use

1. **Launch Window**: Open **Tools > Android Icon Generator** from the top menu bar.
2. **Select Image**: Drag & drop your logo/icon texture (PNG or JPG) into the **Source Image** field, or click **Import / Replace Image...**. A transparent PNG of at least 432x432 works best.
3. **Generate Icons**: Choose whether to create the save folder, use **Browse...** to update its location, then click **Generate Icons**. Input and output validation runs automatically.
4. **Assign to Settings**: Click **Assign To Player Settings**. Assigned slots are verified automatically.

---

## 📦 Samples

The package includes a sample logo texture to help you test immediately:
1. In Unity, open **Window > Package Manager**.
2. Select **Unity Android Icon Generator** from the list.
3. Open the **Samples** section and click **Import** next to **Example Logo**.

---

## 📖 Documentation

- [Installation Guide](Documentation~/Installation.md)
- [User Guide](Documentation~/UserGuide.md)
- [Troubleshooting](Documentation~/Troubleshooting.md)
- [Changelog](CHANGELOG.md)
