using UnityEngine;
using System.Collections;

public class RitualCameraHandler : MonoBehaviour
{
    [Header("Ссылки")]
    public InputReader inputReader;
    public Camera playerCamera;
    public MonoBehaviour playerController; 

    [Header("Настройки")]
    public float cameraMoveSpeed = 2f;

    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private bool isHandlingCamera = false;

    public void EnterRitualMode(Transform ritualCameraTarget)
    {
        if (isHandlingCamera) return;
        isHandlingCamera = true;

        if (playerController != null) playerController.enabled = false;
        if (inputReader != null) inputReader.SwitchToRitual();


        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        originalCameraPosition = playerCamera.transform.position;
        originalCameraRotation = playerCamera.transform.rotation;
        StartCoroutine(MoveCameraToTarget(ritualCameraTarget.position, ritualCameraTarget.rotation, false));
    }

    public void ExitRitualMode()
    {
        if (!isHandlingCamera) return;
        isHandlingCamera = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        StartCoroutine(MoveCameraToTarget(originalCameraPosition, originalCameraRotation, true));
    }

    IEnumerator MoveCameraToTarget(Vector3 targetPosition, Quaternion targetRotation, bool isReturning)
    {
        float time = 0;
        Vector3 startPosition = playerCamera.transform.position;
        Quaternion startRotation = playerCamera.transform.rotation;
        while (time < 1)
        {
            playerCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, time);
            playerCamera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, time);
            time += Time.deltaTime * cameraMoveSpeed;
            yield return null;
        }
        playerCamera.transform.position = targetPosition;
        playerCamera.transform.rotation = targetRotation;
        if (isReturning)
        {
            if (playerController != null) playerController.enabled = true;
            if (inputReader != null) inputReader.SwitchToGameplay();
        }
    }
}