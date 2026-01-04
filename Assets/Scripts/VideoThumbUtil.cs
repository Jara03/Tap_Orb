using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public static class VideoThumbUtil
{
    // Génère un thumbnail et le met en cache en PNG (pour éviter de régénérer à chaque ouverture UI)
    public static IEnumerator GetOrCreateThumbnailSprite(
        string videoFullPath,
        int maxSize,
        double captureTimeSeconds,
        Action<Sprite> onDone)
    {
        if (string.IsNullOrEmpty(videoFullPath) || !File.Exists(videoFullPath))
        {
            onDone?.Invoke(null);
            yield break;
        }

        string cachePath = Path.ChangeExtension(videoFullPath, ".thumb.png");

        // 1) Cache hit -> charge PNG
        if (File.Exists(cachePath))
        {
            var png = File.ReadAllBytes(cachePath);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (tex.LoadImage(png))
            {
                onDone?.Invoke(ToSprite(tex));
                yield break;
            }
            UnityEngine.Object.Destroy(tex);
        }

        // 2) Cache miss -> génère via NativeGallery (async pour éviter un hitch)
        Task<Texture2D> task = NativeGallery.GetVideoThumbnailAsync(
            videoFullPath,
            maxSize: maxSize,
            captureTimeInSeconds: captureTimeSeconds,
            markTextureNonReadable: false // on veut encoder en PNG -> doit être readable
        );

        while (!task.IsCompleted) yield return null;

        Texture2D thumb = task.Result;
        if (thumb == null)
        {
            onDone?.Invoke(null);
            yield break;
        }

        // 3) Sauvegarde en cache
        try
        {
            var png = thumb.EncodeToPNG();
            File.WriteAllBytes(cachePath, png);
        }
        catch { /* si ça fail, pas grave */ }

        onDone?.Invoke(ToSprite(thumb));
    }

    private static Sprite ToSprite(Texture2D tex)
    {
        return Sprite.Create(tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f);
    }
}
