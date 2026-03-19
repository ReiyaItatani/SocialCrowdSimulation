using UnityEngine;
using System.IO;
using System.Collections;

public class GifFrameCapture : MonoBehaviour
{
    public int targetFrameCount = 60;
    public float captureInterval = 0.1f;
    public int captureWidth = 800;
    public int captureHeight = 450;

    private int framesCaptured = 0;
    private string outputDir;
    private Camera cam;

    void Start()
    {
        outputDir = Application.dataPath + "/../Temp/GifFrames";
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        foreach (var file in Directory.GetFiles(outputDir, "*.png"))
            File.Delete(file);

        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        if (cam == null)
        {
            var cameras = Camera.allCameras;
            if (cameras.Length > 0) cam = cameras[0];
        }

        if (cam == null)
        {
            Debug.LogError("GifFrameCapture: No camera found!");
            return;
        }

        Debug.Log("GifFrameCapture: Starting capture from " + cam.name + " -> " + outputDir);
        StartCoroutine(CaptureFrames());
    }

    IEnumerator CaptureFrames()
    {
        yield return new WaitForSeconds(2f);

        while (framesCaptured < targetFrameCount)
        {
            CaptureFrame();
            yield return new WaitForSeconds(captureInterval);
        }

        Debug.Log("GIF capture complete: " + framesCaptured + " frames saved to " + outputDir);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    void CaptureFrame()
    {
        RenderTexture rt = new RenderTexture(captureWidth, captureHeight, 24);
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);
        tex.Apply();

        string filename = Path.Combine(outputDir, string.Format("frame_{0:D4}.png", framesCaptured));
        File.WriteAllBytes(filename, tex.EncodeToPNG());

        cam.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);
        Destroy(tex);

        framesCaptured++;
        if (framesCaptured % 10 == 0)
            Debug.Log("Captured " + framesCaptured + "/" + targetFrameCount);
    }
}
