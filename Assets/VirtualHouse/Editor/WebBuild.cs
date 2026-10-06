using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace VirtualHouse.Editor
{
    public static class WebBuild
    {
        private const string BuildRequestFile = "Temp/VirtualHouseWebBuild.request";

        static WebBuild()
        {
            if (!Application.isBatchMode)
                EditorApplication.delayCall += BuildIfRequested;
        }

        [MenuItem("Virtual House/Build WebGL")]
        public static void BuildWebGL()
        {
            string outputPath = GetCommandLineValue("-webBuildPath") ?? "docs";
            outputPath = Path.GetFullPath(outputPath);
            Directory.CreateDirectory(outputPath);

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
                throw new InvalidOperationException("No enabled scenes. Add HouseBlockout.unity to the build scene list.");

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.dataCaching = true;

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"WebGL build failed: {report.summary.result}");

            Debug.Log($"WebGL build completed: {outputPath}");
        }

        // Also run for File > Build Profiles builds, not only our custom menu.
        public sealed class WebBuildPostprocessor : IPostprocessBuildWithReport
        {
            public int callbackOrder => 100;
            public void OnPostprocessBuild(BuildReport report)
            {
                if (report.summary.platform != BuildTarget.WebGL) return;
                FinalizeWebBuild(report.summary.outputPath);
            }
        }

        public static void FinalizeWebBuild(string outputPath)
        {
            File.WriteAllText(Path.Combine(outputPath, ".nojekyll"), string.Empty);
            NormalizeBuildFilenames(outputPath);
            MakePageTouchFriendly(Path.Combine(outputPath, "index.html"));
            string html = File.ReadAllText(Path.Combine(outputPath, "index.html"));
            foreach (string extension in new[] { "data", "framework.js", "loader.js", "wasm" })
            {
                string name = "WebGL." + extension;
                if (!html.Contains("/" + name) || !File.Exists(Path.Combine(outputPath, "Build", name)))
                    throw new BuildFailedException("Missing Web build file or HTML reference: " + name);
            }
            Debug.Log("Web build structure verified: " + outputPath);
        }

        public sealed class WebBuildPreprocessor : IPreprocessBuildWithReport
        {
            public int callbackOrder => 0;
            public void OnPreprocessBuild(BuildReport report)
            {
                if (report.summary.platform != BuildTarget.WebGL) return;
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
                PlayerSettings.WebGL.dataCaching = true;
            }
        }

        private static void BuildIfRequested()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += BuildIfRequested;
                return;
            }

            if (!File.Exists(BuildRequestFile))
                return;

            File.Delete(BuildRequestFile);
            BuildWebGL();
        }

        private static void MakePageTouchFriendly(string indexPath)
        {
            string html = File.ReadAllText(indexPath);
            // Replace our previous style too, so repairing an existing output is idempotent.
            html = Regex.Replace(html, "<style id=\"virtual-house-fullscreen\">.*?</style>",
                string.Empty, RegexOptions.Singleline);
            const string mobileStyle = @"<style id=""virtual-house-fullscreen"">
      html, body, #unity-container, #unity-canvas { width: 100%; height: 100%; margin: 0; overflow: hidden; }
      body, #unity-canvas { touch-action: none; overscroll-behavior: none; }
      #unity-container, #unity-container.unity-desktop, #unity-container.unity-mobile { position: fixed; inset: 0; transform: none; }
      #unity-canvas { display: block; width: 100% !important; height: 100% !important; }
      #unity-footer { display: none; }
    </style>";
            html = html.Replace("</head>", mobileStyle + Environment.NewLine + "  </head>");
            File.WriteAllText(indexPath, html);
        }

        private static void NormalizeBuildFilenames(string outputPath)
        {
            string generatedName = new DirectoryInfo(outputPath).Name;
            if (generatedName == "WebGL")
                return;

            string buildFolder = Path.Combine(outputPath, "Build");
            string[] extensions = { "data", "framework.js", "loader.js", "wasm" };
            // Do not silently combine part of a new build with stale WebGL.* files.
            bool hasGeneratedFiles = extensions.Any(extension => File.Exists(Path.Combine(buildFolder, $"{generatedName}.{extension}")));
            if (!hasGeneratedFiles) return; // An already normalized output is checked by FinalizeWebBuild.
            foreach (string extension in extensions)
                if (!File.Exists(Path.Combine(buildFolder, $"{generatedName}.{extension}")))
                    throw new BuildFailedException("Incomplete Web build; rebuild the entire output: " + generatedName + "." + extension);
            foreach (string extension in extensions)
            {
                string source = Path.Combine(buildFolder, $"{generatedName}.{extension}");
                string destination = Path.Combine(buildFolder, $"WebGL.{extension}");
                if (!File.Exists(source))
                    continue;
                if (File.Exists(destination))
                    File.Delete(destination);
                File.Move(source, destination);
            }

            string indexPath = Path.Combine(outputPath, "index.html");
            string html = File.ReadAllText(indexPath);
            html = html.Replace($"/{generatedName}.loader.js", "/WebGL.loader.js")
                .Replace($"/{generatedName}.data", "/WebGL.data")
                .Replace($"/{generatedName}.framework.js", "/WebGL.framework.js")
                .Replace($"/{generatedName}.wasm", "/WebGL.wasm");
            File.WriteAllText(indexPath, html);
        }

        private static string GetCommandLineValue(string key)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, key);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
