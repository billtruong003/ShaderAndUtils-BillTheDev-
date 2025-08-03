// Assets/Editor/SaliencyProcessor.cs
using UnityEngine;

public static class SaliencyProcessor
{
    // Generates a grayscale saliency map for a texture.
    // White pixels are important, black pixels are not.
    public static float[,] GenerateSaliencyMap(Texture2D sourceTexture)
    {
        int width = sourceTexture.width;
        int height = sourceTexture.height;
        float[,] saliencyMap = new float[width, height];
        Color[] pixels = sourceTexture.GetPixels();

        // Pass 1: Luminance contribution
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                saliencyMap[x, y] = pixels[y * width + x].grayscale;
            }
        }

        // Pass 2: Edge detection contribution using a simplified Sobel filter
        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                float gx = GetGray(pixels, x - 1, y + 1, width) + 2 * GetGray(pixels, x, y + 1, width) + GetGray(pixels, x + 1, y + 1, width) -
                           (GetGray(pixels, x - 1, y - 1, width) + 2 * GetGray(pixels, x, y - 1, width) + GetGray(pixels, x + 1, y - 1, width));

                float gy = GetGray(pixels, x + 1, y - 1, width) + 2 * GetGray(pixels, x + 1, y, width) + GetGray(pixels, x + 1, y + 1, width) -
                           (GetGray(pixels, x - 1, y - 1, width) + 2 * GetGray(pixels, x - 1, y, width) + GetGray(pixels, x - 1, y + 1, width));

                float edgeMagnitude = Mathf.Sqrt(gx * gx + gy * gy);
                saliencyMap[x, y] = Mathf.Clamp01(saliencyMap[x, y] + edgeMagnitude);
            }
        }

        return NormalizeMap(saliencyMap, width, height);
    }

    private static float GetGray(Color[] pixels, int x, int y, int width)
    {
        return pixels[y * width + x].grayscale;
    }

    private static float[,] NormalizeMap(float[,] map, int width, int height)
    {
        float maxVal = 0f;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (map[x, y] > maxVal)
                {
                    maxVal = map[x, y];
                }
            }
        }

        if (maxVal > 0)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    map[x, y] /= maxVal;
                }
            }
        }
        return map;
    }
}