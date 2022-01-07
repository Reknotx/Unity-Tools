using UnityEngine;

public static class Noise
{
    public enum NormalizeMode{ Local, Global }

    /// <summary>
    /// Generates a noise map for the terrain.
    /// </summary>
    /// <param name="mapWidth">The width of the noise map.</param>
    /// <param name="mapHeight">The height of the noise map.</param>
    /// <param name="settings"></param>
    /// <returns>A 2D array noise map.</returns>
    /// <remarks>The values in the array range from 0 to 1.</remarks>
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, NoiseSettings settings, Vector2 sampleCenter)

    {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        System.Random prng = new System.Random(settings.seed);
        Vector2[] octaveOffsets = new Vector2[settings.octaves];

        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;

        for (int i = 0; i < settings.octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + settings.offset.x + sampleCenter.x;
            float offsetY = prng.Next(-100000, 100000) - settings.offset.y - sampleCenter.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= settings.persistence;
        }

        if (settings.scale <= 0)
            settings.scale = 0.0001f;

        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        float halfWidth = mapWidth / 2;
        float halfHeight = mapHeight / 2;

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < settings.octaves; i++)
                {
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / settings.scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / settings.scale * frequency;
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= settings.persistence;
                    frequency *= settings.lacunarity;

                }

                if (noiseHeight > maxLocalNoiseHeight)
                    maxLocalNoiseHeight = noiseHeight;
                if (noiseHeight < minLocalNoiseHeight)
                    minLocalNoiseHeight = noiseHeight;

                noiseMap[x, y] = noiseHeight;

                if (settings.normalizeMode == NormalizeMode.Global)
                {
                    float normalizedHeight = (noiseMap[x, y] + 1) / (maxPossibleHeight);
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                }
            }
        }
        

        if (settings.normalizeMode == NormalizeMode.Local)
        {
            for (int y = 0; y < mapHeight; y++)
                for (int x = 0; x < mapWidth; x++)
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
        }

        return noiseMap;
    }

}

[System.Serializable]
public class NoiseSettings
{
    public Noise.NormalizeMode normalizeMode;

    [Min(0.0001f)] public float scale = 50;
    [Range(1, 15)] public int octaves = 6;

    [Range(0, 1)] public float persistence = 0.5f;
    public float lacunarity = 2;

    public int seed;
    public Vector2 offset;

    public void ValidateValues()
    {
        scale = Mathf.Max(scale, 0.01f);

        octaves = Mathf.Max(octaves, 1);
        lacunarity = Mathf.Max(lacunarity, 1);
    }
}
