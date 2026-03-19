using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class GifCaptureMenu
{
    [MenuItem("Tools/GIF Capture/Start Collision Avoidance Capture")]
    static void StartCollisionAvoidanceCapture()
    {
        // Attach GifFrameCapture to the first camera found and start play mode
        SetupCapture("----Example(Basic)----", 60, 0.1f);
    }

    [MenuItem("Tools/GIF Capture/Convert Frames to GIF (collision_avoidance)")]
    static void ConvertCollisionAvoidanceGif()
    {
        ConvertToGif("collision_avoidance.gif");
    }

    [MenuItem("Tools/GIF Capture/Convert Frames to GIF (create_avatar)")]
    static void ConvertCreateAvatarGif()
    {
        ConvertToGif("create_avatar.gif");
    }

    [MenuItem("Tools/GIF Capture/Convert Frames to GIF (create_player)")]
    static void ConvertCreatePlayerGif()
    {
        ConvertToGif("create_player.gif");
    }

    static void SetupCapture(string cameraParentName, int frames, float interval)
    {
        // Find camera in scene
        Camera targetCam = null;
        var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var cam in cameras)
        {
            targetCam = cam;
            break;
        }

        if (targetCam == null)
        {
            Debug.LogError("No camera found in scene");
            return;
        }

        // Remove existing capture component
        var existing = targetCam.GetComponent<GifFrameCapture>();
        if (existing != null) Object.DestroyImmediate(existing);

        // Add and configure
        var capture = targetCam.gameObject.AddComponent<GifFrameCapture>();
        capture.targetFrameCount = frames;
        capture.captureInterval = interval;
        capture.captureWidth = 800;
        capture.captureHeight = 450;

        Debug.Log("GifFrameCapture attached to " + targetCam.name + ". Starting play mode...");

        // Save scene to avoid dialog
        EditorSceneManager.SaveOpenScenes();

        // Start play mode
        EditorApplication.isPlaying = true;
    }

    static void ConvertToGif(string outputName)
    {
        string framesDir = Application.dataPath + "/../Temp/GifFrames";
        string outputPath = Application.dataPath +
            "/com.reiya.socialcrowdsimulation/Documentation~/images/" + outputName;

        if (!System.IO.Directory.Exists(framesDir))
        {
            Debug.LogError("No frames directory found: " + framesDir);
            return;
        }

        var files = System.IO.Directory.GetFiles(framesDir, "frame_*.png");
        if (files.Length == 0)
        {
            Debug.LogError("No frames found in " + framesDir);
            return;
        }

        Debug.Log("Converting " + files.Length + " frames to " + outputName + "...");

        // Use ffmpeg to convert
        string ffmpegArgs = string.Format(
            "-y -framerate 10 -i \"{0}/frame_%04d.png\" -vf \"fps=10,scale=600:-1:flags=lanczos,split[s0][s1];[s0]palettegen[p];[s1][p]paletteuse\" \"{1}\"",
            framesDir.Replace("\\", "/"), outputPath.Replace("\\", "/"));

        var process = new System.Diagnostics.Process();
        process.StartInfo.FileName = "ffmpeg";
        process.StartInfo.Arguments = ffmpegArgs;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;

        process.Start();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode == 0)
        {
            var fileInfo = new System.IO.FileInfo(outputPath);
            Debug.Log("GIF created: " + outputPath + " (" + (fileInfo.Length / 1024) + " KB)");
        }
        else
        {
            Debug.LogError("ffmpeg failed: " + stderr);
        }
    }
}
