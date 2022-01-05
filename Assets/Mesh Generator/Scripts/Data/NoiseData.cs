using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu()]
public class NoiseData : UpdatableData
{
    public Noise.NormalizeMode normalizeMode;

    [Min(0.0001f)] public float noiseScale;
    [Min(1)] public int octaves;

    [Range(0, 1)] public float persistence;
    public float lacunarity;

    public int seed;
    public Vector2 offset;

    protected override void OnValidate()
    {
        if (lacunarity < 1) lacunarity = 1;

        if (octaves < 0) octaves = 0;
        
        base.OnValidate();
    }
    
    
}
