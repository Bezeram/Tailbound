using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class RuntimeResourceLoader
{
    public static string CustomContentDirectory => Path.Combine(Application.persistentDataPath, "Resources");

    // Matches the project's own pixel-art convention (16px tiles = 1 world unit).
    private const float PixelsPerUnit = 16f;

    private static readonly string[] SpriteExtensions = { ".png", ".jpg", ".jpeg" };

    // Caches hits and misses - avoids hitting disk every frame from update().
    private static readonly Dictionary<string, Sprite> _SpriteCache = new();
    private static readonly Dictionary<string, AudioClip> _AudioCache = new();

    public static Sprite LoadSprite(string resourcesPath)
    {
        if (string.IsNullOrEmpty(resourcesPath))
            return null;

        if (_SpriteCache.TryGetValue(resourcesPath, out var cached))
            return cached;

        Sprite sprite = LoadCustomSprite(resourcesPath) ?? Resources.Load<Sprite>(resourcesPath);
        _SpriteCache[resourcesPath] = sprite;
        return sprite;
    }

    public static AudioClip LoadAudioClip(string resourcesPath)
    {
        if (string.IsNullOrEmpty(resourcesPath))
            return null;

        if (_AudioCache.TryGetValue(resourcesPath, out var cached))
            return cached;

        AudioClip clip = LoadCustomAudioClip(resourcesPath) ?? Resources.Load<AudioClip>(resourcesPath);
        _AudioCache[resourcesPath] = clip;
        return clip;
    }

    public static void ClearCache()
    {
        _SpriteCache.Clear();
        _AudioCache.Clear();
    }

    private static Sprite LoadCustomSprite(string resourcesPath)
    {
        string basePath = Path.Combine(CustomContentDirectory, resourcesPath);
        foreach (string extension in SpriteExtensions)
        {
            string filePath = basePath + extension;
            if (!File.Exists(filePath))
                continue;

            try
            {
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(File.ReadAllBytes(filePath)))
                {
                    Debug.LogWarning($"[RuntimeResourceLoader] '{filePath}' isn't a decodable PNG/JPG.");
                    return null;
                }

                // Point, not the Texture2D default of Bilinear - crisp pixel art, no edge blur.
                texture.filterMode = FilterMode.Point;
                texture.name = Path.GetFileNameWithoutExtension(filePath);
                return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f), PixelsPerUnit, 0, SpriteMeshType.FullRect);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[RuntimeResourceLoader] Failed to load '{filePath}': {exception}");
                return null;
            }
        }

        return null;
    }

    private static AudioClip LoadCustomAudioClip(string resourcesPath)
    {
        string filePath = Path.Combine(CustomContentDirectory, resourcesPath + ".wav");
        if (!File.Exists(filePath))
            return null;

        try
        {
            return ParseWav(File.ReadAllBytes(filePath), Path.GetFileNameWithoutExtension(filePath));
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[RuntimeResourceLoader] Failed to load '{filePath}': {exception}");
            return null;
        }
    }

    private static AudioClip ParseWav(byte[] data, string clipName)
    {
        if (data.Length < 12 || data[0] != 'R' || data[1] != 'I' || data[2] != 'F' || data[3] != 'F'
            || data[8] != 'W' || data[9] != 'A' || data[10] != 'V' || data[11] != 'E')
        {
            Debug.LogWarning($"[RuntimeResourceLoader] '{clipName}.wav' isn't a valid RIFF/WAVE file.");
            return null;
        }

        int pos = 12;
        short audioFormat = 0, numChannels = 0, bitsPerSample = 0;
        int sampleRate = 0;
        byte[] pcmData = null;

        while (pos + 8 <= data.Length)
        {
            int chunkSize = BitConverter.ToInt32(data, pos + 4);
            int chunkStart = pos + 8;
            if (chunkSize < 0 || chunkStart + chunkSize > data.Length)
                break; // truncated/corrupt file - stop rather than read out of bounds

            if (data[pos] == 'f' && data[pos + 1] == 'm' && data[pos + 2] == 't' && data[pos + 3] == ' ')
            {
                audioFormat = BitConverter.ToInt16(data, chunkStart);
                numChannels = BitConverter.ToInt16(data, chunkStart + 2);
                sampleRate = BitConverter.ToInt32(data, chunkStart + 4);
                bitsPerSample = BitConverter.ToInt16(data, chunkStart + 14);
            }
            else if (data[pos] == 'd' && data[pos + 1] == 'a' && data[pos + 2] == 't' && data[pos + 3] == 'a')
            {
                pcmData = new byte[chunkSize];
                Array.Copy(data, chunkStart, pcmData, 0, chunkSize);
            }

            pos = chunkStart + chunkSize + (chunkSize % 2); // chunks are word-aligned
        }

        if (pcmData == null || numChannels == 0 || bitsPerSample == 0)
        {
            Debug.LogWarning($"[RuntimeResourceLoader] '{clipName}.wav': missing fmt/data chunk.");
            return null;
        }

        float[] samples = ConvertToFloatSamples(pcmData, audioFormat, bitsPerSample);
        if (samples == null)
        {
            Debug.LogWarning($"[RuntimeResourceLoader] '{clipName}.wav': unsupported format " +
                              $"(audioFormat={audioFormat}, bitsPerSample={bitsPerSample}) - only PCM 8/16/24/32-bit and 32-bit float are supported.");
            return null;
        }

        var clip = AudioClip.Create(clipName, samples.Length / numChannels, numChannels, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static float[] ConvertToFloatSamples(byte[] pcmData, short audioFormat, short bitsPerSample)
    {
        const short FormatPcm = 1;
        const short FormatFloat = 3;

        if (audioFormat == FormatPcm && bitsPerSample == 8)
        {
            // 8-bit PCM is unsigned (0..255, midpoint 128); everything else here is signed.
            var samples = new float[pcmData.Length];
            for (int i = 0; i < pcmData.Length; i++)
                samples[i] = (pcmData[i] - 128) / 128f;
            return samples;
        }

        if (audioFormat == FormatPcm && bitsPerSample == 16)
        {
            int count = pcmData.Length / 2;
            var samples = new float[count];
            for (int i = 0; i < count; i++)
                samples[i] = BitConverter.ToInt16(pcmData, i * 2) / 32768f;
            return samples;
        }

        if (audioFormat == FormatPcm && bitsPerSample == 24)
        {
            int count = pcmData.Length / 3;
            var samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                int byteIndex = i * 3;
                int sample = pcmData[byteIndex] | (pcmData[byteIndex + 1] << 8) | (pcmData[byteIndex + 2] << 16);
                if ((sample & 0x800000) != 0) // sign-extend the 24-bit value into a 32-bit int
                    sample |= unchecked((int)0xFF000000);
                samples[i] = sample / 8388608f;
            }
            return samples;
        }

        if (audioFormat == FormatPcm && bitsPerSample == 32)
        {
            int count = pcmData.Length / 4;
            var samples = new float[count];
            for (int i = 0; i < count; i++)
                samples[i] = BitConverter.ToInt32(pcmData, i * 4) / 2147483648f;
            return samples;
        }

        if (audioFormat == FormatFloat && bitsPerSample == 32)
        {
            int count = pcmData.Length / 4;
            var samples = new float[count];
            for (int i = 0; i < count; i++)
                samples[i] = BitConverter.ToSingle(pcmData, i * 4);
            return samples;
        }

        return null;
    }
}
