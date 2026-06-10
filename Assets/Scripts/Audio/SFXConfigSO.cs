using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Audio/SFX Config")]
public class SFXConfigSO : ScriptableObject
{
    public List<SFXEntry> entries = new List<SFXEntry>();
}

[System.Serializable]
public class SFXEntry
{
    public SFXKey key;
    public AudioClip clip;

    [Header("Volume & Spatial")]
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0f, 1f)] public float spatialBlend = 0f; // 0=2D, 1=3D

    [Header("Pitch Randomization")]
    [Range(-3f, 3f)] public float pitchMin = 1f;
    [Range(-3f, 3f)] public float pitchMax = 1f;

    [Header("Priority (0=Highest, 256=Lowest)")]
    [Range(0, 256)] public int priority = 128;

#if UNITY_EDITOR
    public void Validate()
    {
        if (pitchMin > pitchMax)
        {
            pitchMax = pitchMin;
            Debug.LogWarning($"SFXEntry {key}: pitchMin > pitchMax, 已自动修正。");
        }
        if (clip == null)
            Debug.LogWarning($"SFXEntry {key}: AudioClip 缺失!");
    }
#endif
}
