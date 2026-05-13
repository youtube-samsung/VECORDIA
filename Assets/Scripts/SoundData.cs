using UnityEngine;

[CreateAssetMenu(fileName = "New Sound", menuName = "Audio/Sound Data")]
public class SoundData : ScriptableObject
{
    [Header("Аудиоклипы")]
    [Tooltip("Если добавить несколько, звук будет выбираться случайно (убирает эффект однообразия)")]
    public AudioClip[] clips;

    [Header("Настройки громкости")]
    [Range(0f, 1f)] public float minVolume = 0.8f;
    [Range(0f, 1f)] public float maxVolume = 1f;

    [Header("Настройки высоты тона (Pitch)")]
    [Tooltip("Меньше 1 - звук ниже и медленнее. Больше 1 - выше и быстрее.")]
    [Range(0.1f, 3f)] public float minPitch = 0.9f;
    [Range(0.1f, 3f)] public float maxPitch = 1.1f;
    public AudioClip GetRandomClip()
    {
        if (clips == null || clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }

    public float GetRandomVolume()
    {
        return Random.Range(minVolume, maxVolume);
    }

    public float GetRandomPitch()
    {
        return Random.Range(minPitch, maxPitch);
    }
}