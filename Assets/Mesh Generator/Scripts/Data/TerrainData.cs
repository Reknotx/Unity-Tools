using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class TerrainData : UpdatableData
{
    public float uniformScale = 5f;

    public bool useFlatShading;
    
    public bool useFalloff;

    [Min(1)] public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

    public float minHeight => uniformScale * meshHeightMultiplier * meshHeightCurve.Evaluate(0f);

    public float maxHeight => uniformScale * meshHeightMultiplier * meshHeightCurve.Evaluate(1f);
}
