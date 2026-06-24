using UnityEngine;

public abstract class BaseAnomaly : MonoBehaviour
{
    [Header("Настройки Режиссера")]
    [Tooltip("Название аномалии ")]
    public string anomalyName = "Новая Аномалия";

    [Tooltip("Вес/Вероятность выбора. Чем выше число, тем чаще режиссер будет выбирать именно эту аномалию среди остальных свободных")]
    public int selectionWeight = 10;

    [Tooltip("Сколько секунд длится это явление перед тем, как автоматически выключиться")]
    public float duration = 5f;


    public bool IsActive { protected set; get; }

    public virtual void TriggerAnomaly()
    {
        IsActive = true;
    }

    public virtual void ResetAnomaly()
    {
        IsActive = false;
    }
}