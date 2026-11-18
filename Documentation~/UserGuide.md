# User Guide

1. **Source image** – drag a PNG/JPG texture into the field, or click *Import / Replace Image...* (files outside the project are copied to `Assets/AndroidIconSource`). A transparent PNG of at least 512x512 (1024 recommended) works best.
2. **Configure** each icon type (enable switch, scale, horizontal/vertical position, Fit/Fill crop, background color). *Reset Image Controls* restores one panel; *Reset Configuration* (toolbar) restores everything except images and the output folder.
   - **Adaptive foreground**: scale 1.0 fits the artwork inside the 66dp safe zone. An optional separate foreground image can be assigned.
   - **Adaptive background**: solid color, gradient (two colors and an angle) or image.
3. **Preview** on the right updates instantly and never writes files. Toggle the safe-zone guides (green = 66dp safe circle, yellow = 72dp visible area). Combined previews show circle, squircle and rounded-square masks.
4. **Export**: choose the output directory (default `Assets/GeneratedAndroidIcons`) and click *Generate All Icons* or *Generate Selected Icons*. If files already exist you can overwrite, skip existing, or cancel.
5. **Player Settings**: choose which categories to assign and click *Assign To Player Settings*. You are asked to confirm, and each category is read back and verified. *Validate Current Assignments* compares Player Settings with the generated files.

## Output layout
```
GeneratedAndroidIcons/
  Legacy/<density>/ic_launcher_legacy_<density>_<px>x<px>.png
  Round/<density>/ic_launcher_round_<density>_<px>x<px>.png
  Adaptive/Foreground/ic_launcher_foreground_<density>_<px>x<px>.png
  Adaptive/Background/ic_launcher_background_<density>_<px>x<px>.png
```
Legacy/Round: 36 (ldpi), 48, 72, 96, 144, 192 px. Adaptive: 81 (ldpi), 108, 162, 216, 324, 432 px. Adaptive layers are never masked; the launcher applies the mask. Uncheck *Include LDPI* to skip ldpi (those Player Settings slots are then left unchanged).

## Supported Unity versions and limitations
- Developed and tested on Unity 6000.1.17f1 (compiles for Unity 6 and newer).
- Assignment needs Android Build Support and an output folder inside `Assets/`.
- Android 13 themed (monochrome) icons are not generated.
- Source formats: PNG, JPG/JPEG (other texture types are read through a render texture).
