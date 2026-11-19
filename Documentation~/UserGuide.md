# User Guide

1. **Source image** - drag a PNG/JPG texture into the field, or click *Import / Replace Image...* (files outside the project are copied to `Assets/AndroidIconSource`). A transparent PNG of at least 432x432 works best.
2. **Preview** - the preview on the right updates instantly and never writes files. Toggle the safe-zone guides (green = 66dp safe circle, yellow = 72dp visible area). Combined previews show circle, squircle and rounded-square masks.
3. **Generate** - enable *Create Save Folder* and use *Browse...* to choose a save location, or disable it to save directly in `Assets`. Click *Generate Icons*. The tool remembers the chosen folder, validates the source automatically, writes four best-fit PNG files into that single folder, and verifies their dimensions.
4. **Player Settings** - choose which categories to assign and click *Assign To Player Settings*. You are asked to confirm, and every assigned slot is read back and verified automatically.

## Output layout

```text
GeneratedAndroidIcons/
  ic_launcher_legacy_xxxhdpi_192x192.png
  ic_launcher_round_xxxhdpi_192x192.png
  ic_launcher_foreground_xxxhdpi_432x432.png
  ic_launcher_background_xxxhdpi_432x432.png
```

Legacy and round icons are generated at 192x192. Adaptive foreground and background layers are generated at 432x432 and are never masked; the launcher applies the mask. Smaller Player Settings slots are intentionally left unchanged.

## Supported Unity versions and limitations

- Developed and tested on Unity 6000.1.17f1 (compiles for Unity 6 and newer).
- Assignment needs Android Build Support and an output folder inside `Assets/`.
- Android 13 themed (monochrome) icons are not generated.
- Source formats: PNG, JPG/JPEG (other texture types are read through a render texture).
