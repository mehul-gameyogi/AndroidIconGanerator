# Installation Guide

## 1. Install via Git URL (Recommended)

You can add this package directly to your Unity project using Unity Package Manager:

1. In the Unity Editor, open **Window > Package Manager**.
2. Click the **`+`** (plus) button in the upper-left corner of the window.
3. Select **Add package from git URL...**.
4. Enter the repository URL:
   ```text
   https://github.com/mehul-gameyogi/AndroidIconGanerator.git
   ```
5. Click **Add**.

Unity will automatically download and install the package.

### Specifying a Version or Branch

To lock the installation to a specific branch or release tag:
- **Main branch**: `https://github.com/mehul-gameyogi/AndroidIconGanerator.git#main`
- **Specific release version**: `https://github.com/mehul-gameyogi/AndroidIconGanerator.git#1.0.0`

---

## 2. Install via `Packages/manifest.json`

You can manually register the dependency in your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.tools.android-icon-generator": "https://github.com/mehul-gameyogi/AndroidIconGanerator.git",
    "com.unity.modules.imageconversion": "1.0.0"
  }
}
```

Save the file and switch back to the Unity Editor; Unity will download and resolve the package.

---

## 3. Install from Local Disk

If you cloned or downloaded the repository locally:

1. Open **Window > Package Manager**.
2. Click **`+` > Add package from disk...**.
3. Select the `package.json` file inside the `com.tools.android-icon-generator` folder.

---

## 4. Embedded Installation

Copy the entire `com.tools.android-icon-generator` folder directly into your Unity project's `Packages/` directory.

---

## Verification

Once installed:
- Check the top menu bar in Unity: **Tools > Android Icon Generator** should now be visible.
- Open the window and start generating icons.

---

## Optional Sample

In Package Manager, select **Unity Android Icon Generator**, expand the **Samples** section, and click **Import** on **Example Logo** to import a test logo into your `Assets/` folder.
