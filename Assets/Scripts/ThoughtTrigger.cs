using UnityEngine;

public class ThoughtTrigger : MonoBehaviour
{
    [Header("Какую мысль показать?")]
    public ThoughtData thoughtToShow;

    [Header("Настройки срабатывания")]
/*    public bool triggerOnStart = false; */    // Показать сразу при запуске сцены
    public bool triggerOnEnter = true;      // когда  войдет в триггер
    public bool triggerOnlyOnce = true;     // только один раз за игру

    private bool hasTriggered = false;

    //private void Start()
    //{
    //    if (triggerOnStart)
    //    {
    //        TriggerThought();
    //    }
    //}

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnEnter && other.CompareTag("Player"))
        {
            TriggerThought();
        }
    }


    public void TriggerThought()
    {
        if (triggerOnlyOnce && hasTriggered) return;
        if (thoughtToShow == null) return;

        SubtitleManager.Instance.ShowThought(thoughtToShow);
        hasTriggered = true;
    }
}