using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PimDeWitte.UnityMainThreadDispatcher;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class ImagePlayer : Singleton<ImagePlayer> {

    public RawImage VideoImage;

    private Queue<Texture2D> frameBuffer = new Queue<Texture2D>();
    private string[] imagePaths;

    private int currentFrame = 0;
    private bool isLoading = false;


    private Dictionary<int, Texture2D> textureCache = new Dictionary<int, Texture2D>(); // Cache
    private string imageFolderPath;
    private int totalFrames;
    private int preloadRange = 10; // Number of frames to preload

    public void LoadMission1() {
        imageFolderPath = Application.persistentDataPath + "/flightVideos/video1/";
        totalFrames = Directory.GetFiles(imageFolderPath, "*.jpg").Length;
    }

    public void LoadMission2() {
        imageFolderPath = Application.persistentDataPath + "/flightVideos/video2/";
        totalFrames = Directory.GetFiles(imageFolderPath, "*.jpg").Length;
    }

    public void OnSeekChanged(int value) {
        LoadFrame(value);
        PreloadFrames(value);
    }

    private async void LoadFrame(int frameIndex) {
        if (textureCache.TryGetValue(frameIndex, out Texture2D cachedTexture)) {
            VideoImage.texture = cachedTexture;
            return;
        }

        string path = Path.Combine(imageFolderPath, frameIndex.ToString("D6") + ".jpg");

        byte[] imageData = await ReadFileAsync(path);
        if (imageData == null)
            return;

        Texture2D tex = new Texture2D(2, 2);
        if (tex.LoadImage(imageData)) {
            textureCache[frameIndex] = tex;
            ApplyTexture(tex);
        }
    }

    private async void PreloadFrames(int centerFrame) {
        for (int i = -preloadRange; i <= preloadRange; i++) {
            int preloadFrame = centerFrame + i;
            if (preloadFrame >= 1 && preloadFrame <= totalFrames && !textureCache.ContainsKey(preloadFrame)) {
                string path = Path.Combine(imageFolderPath, preloadFrame.ToString("D6") + ".jpg");

                byte[] imageData = await ReadFileAsync(path);
                if (imageData == null)
                    continue;

                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(imageData)) {
                    textureCache[preloadFrame] = tex;
                }
            }
        }
    }

    private async Task<byte[]> ReadFileAsync(string path) {
        return await Task.Run(() => {
            if (File.Exists(path))
                return File.ReadAllBytes(path);
            return null;
        });
    }

    private void ApplyTexture(Texture2D texture) {
        // Apply texture on the main thread
        UnityMainThreadDispatcher.Instance().Enqueue(() => {
            VideoImage.texture = texture;
        });
    }
}
