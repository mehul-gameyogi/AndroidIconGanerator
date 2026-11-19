using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace AndroidIconGenerator
{
    public enum ExportStatus { Pending, Written, Skipped, Failed }

    public sealed class GeneratedIconFile
    {
        public IconType Type;
        public AndroidDensity Density;
        public int Size;
        public string Path;
        public bool Exists;
        public ExportStatus Status;
        public string Error;
    }

    public enum ExistingFilePolicy { Overwrite, SkipExisting }

    public sealed class ExportResult
    {
        public List<GeneratedIconFile> Files = new List<GeneratedIconFile>();
        public ValidationReport Report = new ValidationReport();
        public int Written => Files.Count(f => f.Status == ExportStatus.Written);
        public int Skipped => Files.Count(f => f.Status == ExportStatus.Skipped);
        public int Failed => Files.Count(f => f.Status == ExportStatus.Failed);
        public bool Success => Failed == 0 && !Report.HasErrors;
        public bool Cancelled;
    }

    /// <summary>Plans and performs the export of all selected icon types.</summary>
    public static class AndroidIconExporter
    {
        /// <summary>Builds the list of files that would be written (does not touch the disk).</summary>
        public static List<GeneratedIconFile> BuildPlan(AndroidIconSettings s, IEnumerable<IconType> types)
        {
            var plan = new List<GeneratedIconFile>();
            foreach (IconType type in types)
            {
                AndroidDensity d = IconSpecs.BestFitDensity;
                string path = IconFolderManager.GetFilePath(s.outputDirectory, type, d);
                plan.Add(new GeneratedIconFile
                {
                    Type = type,
                    Density = d,
                    Size = IconSpecs.BestFitSize(type),
                    Path = path,
                    Exists = File.Exists(IconFolderManager.ToAbsolute(path)),
                });
            }
            return plan;
        }

        public static IEnumerable<IconType> SelectedTypes(AndroidIconSettings s)
        {
            foreach (IconType t in Enum.GetValues(typeof(IconType)))
                if (s.IsTypeEnabled(t)) yield return t;
        }

        public static IEnumerable<IconType> AllTypes()
        {
            foreach (IconType t in Enum.GetValues(typeof(IconType))) yield return t;
        }

        public static ImageBuffer Render(AndroidIconSettings s, IconSourceSet set, IconType type, int size)
        {
            switch (type)
            {
                case IconType.Legacy: return IconImageProcessor.RenderLegacy(s, set, size);
                case IconType.Round: return IconImageProcessor.RenderRound(s, set, size);
                case IconType.AdaptiveForeground: return AdaptiveIconGenerator.RenderForeground(s, set, size);
                default: return AdaptiveIconGenerator.RenderBackground(s, set, size);
            }
        }

        /// <summary>Renders, writes and verifies every file in the plan. Shows a cancellable progress bar.</summary>
        public static ExportResult Export(AndroidIconSettings s, IconSourceSet set, List<GeneratedIconFile> plan,
            ExistingFilePolicy policy, bool showProgress = true)
        {
            var result = new ExportResult { Files = plan };
            var written = new List<string>();
            try
            {
                for (int i = 0; i < plan.Count; i++)
                {
                    GeneratedIconFile file = plan[i];
                    if (showProgress && EditorUtility.DisplayCancelableProgressBar("Generating Android icons",
                            $"{IconSpecs.DisplayName(file.Type)} {IconSpecs.FolderName(file.Density)} ({file.Size}px)",
                            (i + 1f) / plan.Count))
                    {
                        result.Cancelled = true;
                        result.Report.Warning("Export", "Generation cancelled by the user.", "Run Generate again to finish the remaining icons.");
                        break;
                    }

                    if (file.Exists && policy == ExistingFilePolicy.SkipExisting)
                    {
                        file.Status = ExportStatus.Skipped;
                        continue;
                    }

                    try
                    {
                        ImageBuffer buffer = Render(s, set, file.Type, file.Size);
                        if (buffer == null)
                        {
                            Fail(file, "Nothing to render. Is a source image assigned?");
                            continue;
                        }
                        if (IconFileWriter.WritePng(buffer, file.Path, out string error))
                        {
                            file.Status = ExportStatus.Written;
                            written.Add(file.Path);
                        }
                        else Fail(file, error);
                    }
                    catch (Exception e)
                    {
                        Fail(file, e.Message);
                    }
                }

                if (written.Count > 0 && IconFolderManager.IsInsideProjectAssets(s.outputDirectory))
                {
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    IconFileWriter.ConfigureImportSettings(written);
                }
            }
            finally
            {
                if (showProgress) EditorUtility.ClearProgressBar();
            }

            result.Report.AddRange(IconValidator.ValidateExported(s, result));
            return result;
        }

        private static void Fail(GeneratedIconFile file, string error)
        {
            file.Status = ExportStatus.Failed;
            file.Error = error;
        }
    }
}
