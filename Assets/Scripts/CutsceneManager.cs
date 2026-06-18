using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class CutsceneManager : MonoBehaviour
{
    [Header("Настройки")]
    public bool playOnStart;
    public SceneStep[] steps;

    [Header("События по завершении")]
    public UnityEvent OnFinished;

    private void Start()
    {
        if (playOnStart) Play();
    }

    public void Play()
    {
        StartCoroutine(ExecuteRoutine());
    }
    public IEnumerator PlayRoutine()
    {
        // Позволяет другим корутинам ждать, пока катсцена не завершится полностью
        yield return StartCoroutine(ExecuteRoutine());
    }

    private IEnumerator ExecuteRoutine()
    {
        CinematicController.Instance.ToggleControl(false);

        foreach (var step in steps)
        {

            if (step.sound != null)
                AudioManager.Instance.PlaySound2D(step.sound);
            if (step.doFade)
            {
               yield return StartCoroutine(CinematicController.Instance.FadeRoutine(step.targetAlpha, step.fadeDuration));
            }

            if (step.lookTarget != null)
            {
                var curve = step.rotationCurve.keys.Length > 0 ? step.rotationCurve : AnimationCurve.EaseInOut(0, 0, 1, 1);
                yield return StartCoroutine(CinematicController.Instance.LookAtRoutine(step.lookTarget, step.rotationDuration, curve));
            }

            if (step.changeFOV)
            {
                yield return StartCoroutine(CinematicController.Instance.FOVRoutine(step.targetFOV, step.fovDuration));
                if (step.resetFovAfter)
                {
                    yield return new WaitForSeconds(step.waitTimeAfter);
                    yield return StartCoroutine(CinematicController.Instance.ResetFOVRoutine(step.fovDuration));
                }
            }

            if (step.thought != null)
                SubtitleManager.Instance.ShowThought(step.thought);

            if (step.waitTimeAfter > 0) yield return new WaitForSeconds(step.waitTimeAfter);

            if (step.restoreControl) CinematicController.Instance.ToggleControl(true);
        }

        CinematicController.Instance.ToggleControl(true);
        OnFinished?.Invoke();
    }
}