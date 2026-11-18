# Troubleshooting

| Message | Cause | Fix |
|---|---|---|
| No source image is assigned | Nothing in the Source Image field | Import or drag a PNG/JPG |
| Image is smaller than 432px | xxxhdpi adaptive icons will look soft | Use 512px or larger |
| Foreground is fully opaque | JPG or PNG without transparency covers the background | Use a transparent PNG |
| Content outside the safe zone / visible area | Scale or offset too large | Lower the foreground scale, reset offsets |
| Background mode is Image but no image | Missing background image | Assign one or switch to Solid/Gradient |
| Output folder is outside Assets/ | Files are not Unity assets | Choose a folder inside `Assets/` |
| Android Build Support is not installed | Module missing | Unity Hub > Installs > Add Modules > Android |
| Assignment verification failed | Unity rejected a slot | Assign manually in Project Settings > Player > Icon |
| Export failed / access denied | Read-only or locked file | Close programs using the file, check permissions |
| Some slots left unchanged | No generated file for that size (e.g. ldpi excluded) | Enable *Include LDPI*, generate again |

Warnings and errors are also written to the Console with the prefix `[Android Icon Generator]`.
