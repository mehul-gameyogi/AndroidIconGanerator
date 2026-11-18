# Unity Android Icon Generator

An Editor-only Unity package that turns one logo into every Android launcher icon: **Legacy**, **Round** and **Adaptive** (separate foreground and background layers). It previews them live, exports organized PNGs and assigns them to Android Player Settings.

Open it from **Tools > Android Icon Generator**.

## Features
- Legacy (48dp), Round (circular crop) and Adaptive (108dp foreground and background) icons, ldpi to xxxhdpi.
- Scale, position, crop (Fit/Fill) and background controls per icon type.
- Solid, gradient or image adaptive background.
- Live preview with circle, squircle and rounded-square masks plus 66dp safe-zone and 72dp visible-area guides.
- Overwrite confirmation, progress bar, verification of every exported PNG.
- Assigns icons through `PlayerSettings.SetPlatformIcons` and verifies the result.
- Validation with Success / Warning / Error messages and suggested fixes.
- No runtime code; everything lives in an Editor-only assembly.

## Requirements
Unity 6 (6000.0 or newer). Android Build Support is only needed for the Player Settings assignment.

## Docs
- [Installation](Documentation~/Installation.md)
- [User Guide](Documentation~/UserGuide.md)
- [Troubleshooting](Documentation~/Troubleshooting.md)
- [Changelog](CHANGELOG.md)
