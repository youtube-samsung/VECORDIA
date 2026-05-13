using UnityEngine;

[CreateAssetMenu(fileName = "New Thought", menuName = "Story/Thought Data")]
public class ThoughtData : ScriptableObject
{
    [Header("Текст реплики")]
    [TextArea(3, 5)]
    public string thoughtText;

    [Tooltip("Сколько секунд текст будет висеть на экране после появления")]
    public float displayDuration = 3f;

    [Header("Стилизация")]
    public Color textColor = Color.white;
    public float fontSize = 36f;

    [Header("Эффект печати (Typewriter)")]
    public bool useTypewriterEffect = true;
    public float typewriterSpeed = 0.05f; 

    [Header("Звуковое сопровождение (Необязательно)")]
    [Tooltip("Вставь сюда SoundData (например, звук шепота или стук сердца)")]
    public SoundData thoughtSound;
}