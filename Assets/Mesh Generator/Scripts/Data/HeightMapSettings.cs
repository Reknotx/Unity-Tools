using UnityEngine;

[CreateAssetMenu()]
public class HeightMapSettings : UpdatableData
{
    public NoiseSettings noiseSettings;
    
    public bool useFalloff;

    [Min(1)] public float heightMultiplier;
    public AnimationCurve heightCurve;

    public float minHeight => heightMultiplier * heightCurve.Evaluate(0f);

    public float maxHeight => heightMultiplier * heightCurve.Evaluate(1f);

    #if UNITY_EDITOR
    protected override void OnValidate()
    {
        noiseSettings.ValidateValues();
        base.OnValidate();
    }
    #endif
    
}
