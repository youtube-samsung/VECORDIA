using UnityEngine;

[System.Serializable]
public struct SceneStep
{
    public string stepName;

    [Header("Камера: Поворот")]
    [Tooltip("Объект, на который нужно посмотреть. Если пусто — камера не вращается.")]
    public Transform lookTarget;
    public float rotationDuration;
    public AnimationCurve rotationCurve;

    [Header("Камера: зум")]
    public bool changeFOV;
    public float targetFOV;
    public float fovDuration;
    [Tooltip("Вернуть ли зум в стандартное значение сразу после этого шага?")]
    public bool resetFovAfter;

    [Header("События")]
    public ThoughtData thought;
    public SoundData sound;

    [Header("Тайминги и Контроль")]
    [Tooltip("Пауза ПОСЛЕ выполнения всех действий этого шага.")]
    public float waitTimeAfter;
    [Tooltip("Если true — управление вернется игроку сразу после этого шага.")]
    public bool restoreControl;

    [Header("Экран: Затемнение (Fade)")]
    [Tooltip("Включить изменение прозрачности экрана?")]
    public bool doFade;
    [Tooltip("0 - полностью прозрачно (игра), 1 - полностью черный экран")]
    [Range(0f, 1f)] public float targetAlpha;
    public float fadeDuration;
}