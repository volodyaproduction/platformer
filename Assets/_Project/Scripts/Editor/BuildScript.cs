using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Скрипт сборки: WebGL (для веб-деплоя) и Windows-Standalone (.exe).
// Запускается из CLI через -executeMethod BuildScript.BuildWebGL / BuildWindows.
public static class BuildScript
{
    const string ScenePath = "Assets/_Project/Scenes/Main.unity";
    const string WebOutDir = "web";
    const string WinOutDir = "build/Windows";
    const string WinExeName = "Platformer.exe";

    public static void BuildAll()
    {
        BuildWindows();
        BuildWebGL();
    }

    public static void BuildWindows()
    {
        // Регенерируем сцену, чтобы сборка была одношаговой
        SceneBuilder.Build();

        EnsureDirectory(WinOutDir);

        // 1. Настройки качества и компании
        PlayerSettings.companyName = "Platformer";
        PlayerSettings.productName = "Platformer";

        var options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = Path.Combine(WinOutDir, WinExeName),
            target = BuildTarget.StandaloneWindows64,
            targetGroup = BuildTargetGroup.Standalone,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        ReportResult("Windows", report);
    }

    public static void BuildWebGL()
    {
        // Регенерируем сцену, чтобы сборка была одношаговой
        SceneBuilder.Build();

        EnsureDirectory(WebOutDir);

        // 2. WebGL-настройки: Brotli, Fallback ON, потоки выкл,
        // Quality Low — всё для минимизации размера и совместимости с любым
        // хостингом (Vercel без vercel.json).
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.threadsSupport = false;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.runInBackground = false;
        PlayerSettings.companyName = "Platformer";
        PlayerSettings.productName = "Platformer";

        // 3. WebGL Memory — обязательное в Unity 6 (иначе билд может упасть)
        PlayerSettings.WebGL.initialMemorySize = 64;

        // 4. Выставляем Low Quality, чтобы убрать тяжёлые шейдеры
        var qualityLevels = QualitySettings.names;
        for (int i = 0; i < qualityLevels.Length; i++)
        {
            if (qualityLevels[i].Equals("Low", System.StringComparison.OrdinalIgnoreCase))
            {
                QualitySettings.SetQualityLevel(i, true);
                break;
            }
        }

        var options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = WebOutDir,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        ReportResult("WebGL", report);
    }

    static void ReportResult(string label, BuildReport report)
    {
        var summary = report.summary;
        Debug.Log($"[BuildScript] {label}: {summary.result}, "
                  + $"размер {summary.totalSize / 1024 / 1024} МБ, "
                  + $"время {summary.totalTime}");
        if (summary.result != BuildResult.Succeeded)
        {
            EditorApplication.Exit(1);
        }
    }

    static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
    }
}
