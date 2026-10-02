using System;
using System.IO;
using System.Linq;
using UnityEditor;
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

            File.WriteAllText(Path.Combine(outputPath, ".nojekyll"), string.Empty);
            NormalizeBuildFilenames(outputPath);
            MakePageTouchFriendly(Path.Combine(outputPath, "index.html"));
            Debug.Log($"WebGL build completed: {outputPath}");
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
            const string mobileStyle = @"<style>
      html, body, #unity-container, #unity-canvas { width: 100%; height: 100%; margin: 0; overflow: hidden; }
      body, #unity-canvas { touch-action: none; overscroll-behavior: none; }
      #unity-container { position: fixed; inset: 0; }
      #unity-canvas { display: block; width: 100% !important; height: 100% !important; }
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
