using System.Collections;
using UnityEngine;

public class CinematicController : MonoBehaviour
{
    public static CinematicController Instance { get; private set; }

    [Header("—сылки")]
    public Camera playerCamera;
    public MonoBehaviour movementScript;
    public CanvasGroup blackScreen;

    private float _defaultFOV;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (playerCamera != null) _defaultFOV = playerCamera.fieldOfView;
    }

    public void ToggleControl(bool state)
    {
        if (movementScript != null) movementScript.enabled = state;
    }

    // ”ниверсальный метод плавного поворота
    public IEnumerator LookAtRoutine(Transform target, float duration, AnimationCurve curve)
    {
        if (target == null) yield break;

        Quaternion startRot = playerCamera.transform.rotation;
        Quaternion targetRot = Quaternion.LookRotation(target.position - playerCamera.transform.position);

        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = curve.Evaluate(elapsed / duration);
            playerCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }
        playerCamera.transform.rotation = targetRot;
    }

    // ”ниверсальный метод изменени€ зума
    public IEnumerator FOVRoutine(float targetFOV, float duration)
    {
        float startFOV = playerCamera.fieldOfView;
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            playerCamera.fieldOfView = Mathf.Lerp(startFOV, targetFOV, elapsed / duration);
            yield return null;
        }
        playerCamera.fieldOfView = targetFOV;
    }

    public IEnumerator ResetFOVRoutine(float duration)
    {
        yield return StartCoroutine(FOVRoutine(_defaultFOV, duration));
    }

    public IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        float startAlpha = blackScreen.alpha;
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            blackScreen.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }
        blackScreen.alpha = targetAlpha;
    }
}