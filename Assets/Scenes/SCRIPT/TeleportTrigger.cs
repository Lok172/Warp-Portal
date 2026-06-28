using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PortalTeleport : MonoBehaviour
{
    public enum DestinationEnvironment
    {
        America,
        Japan
    }

    [Header("Target Portal")]
    public Transform targetPortal;

    [Header("Destination Music")]
    public DestinationEnvironment destinationEnvironment;
    public EnvironmentMusicManager musicManager;

    [Header("Portal Sound")]
    public AudioSource portalAudio;

    [Header("Fade Transition")]
    public ScreenFade screenFade;

    [Header("Teleport Settings")]
    public float teleportDelay = 0.5f;
    public float cooldown = 1.0f;
    public float exitDistance = 1.5f;

    [Header("Ground Check")]
    public float groundRayHeight = 2f;
    public float groundRayDistance = 5f;
    public LayerMask groundMask;

    [Header("Portal Messages")]
    public TextMeshProUGUI textDisplay;

    [TextArea]
    public string[] portalMessages;

    public float typingSpeed = 0.02f;
    public float messageDisplayTime = 2.5f;

    [Header("Distance Tracker")]
    public ItemDistanceTracker distanceTracker;
    public string destinationCountryName = "Japan";

    [Header("Progress Gate")]
    public bool requireCountryCompletionBeforeTeleport = false;
    public string requiredCountryName = "Japan";

    [TextArea(2, 4)]
    public string lockedMessage = "Please complete Japan first: collect all cultural items and pass the quiz before entering America.";

    public bool useAutoRequirementMessage = true;

    [Header("Locked Notification")]
    public bool useFactPanelForLockedNotification = true;
    public string lockedPanelTitle = "Portal Locked";

    private int currentMessage = 0;
    private Coroutine typingCoroutine;

    private static Dictionary<GameObject, float> cooldownMap = new Dictionary<GameObject, float>();

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        GameObject playerObject = other.gameObject;

        if (cooldownMap.TryGetValue(playerObject, out float cooldownEndTime) && Time.time < cooldownEndTime)
        {
            return;
        }

        cooldownMap[playerObject] = Time.time + cooldown;

        if (!CanUsePortal())
        {
            ShowLockedNotification();
            return;
        }

        StartCoroutine(TeleportRoutine(playerObject));
    }

    private bool CanUsePortal()
    {
        if (!requireCountryCompletionBeforeTeleport)
        {
            return true;
        }

        GameProgressManager progressManager = GameProgressManager.Instance;

        if (progressManager == null)
        {
            progressManager = Object.FindFirstObjectByType<GameProgressManager>();
        }

        if (progressManager == null)
        {
            Debug.LogWarning("[PortalTeleport] GameProgressManager not found. Portal is locked.");
            return false;
        }

        return progressManager.IsCountryCompleted(requiredCountryName);
    }

    private void ShowLockedNotification()
    {
        string messageToShow = lockedMessage;

        if (useAutoRequirementMessage)
        {
            GameProgressManager progressManager = GameProgressManager.Instance;

            if (progressManager == null)
            {
                progressManager = Object.FindFirstObjectByType<GameProgressManager>();
            }

            if (progressManager != null)
            {
                string autoMessage = progressManager.GetMissingRequirementMessage(requiredCountryName);

                if (!string.IsNullOrEmpty(autoMessage))
                {
                    messageToShow = autoMessage;
                }
            }
        }

        if (useFactPanelForLockedNotification && FactPanelManager.Instance != null)
        {
            FactPanelManager.Instance.ShowFact(lockedPanelTitle, messageToShow);
            return;
        }

        ShowMessage(messageToShow);
    }

    private void PlayNextMessage()
    {
        if (portalMessages == null || portalMessages.Length == 0)
        {
            return;
        }

        ShowMessage(portalMessages[currentMessage]);

        currentMessage = (currentMessage + 1) % portalMessages.Length;
    }

    private void ShowMessage(string message)
    {
        if (textDisplay == null)
        {
            Debug.Log(message);
            return;
        }

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(TypeText(message));
    }

    private IEnumerator TypeText(string message)
    {
        textDisplay.text = "";
        textDisplay.alpha = 0f;

        float fadeInTime = 0.3f;
        float t = 0f;

        while (t < fadeInTime)
        {
            t += Time.deltaTime;
            textDisplay.alpha = Mathf.Lerp(0f, 1f, t / fadeInTime);
            yield return null;
        }

        textDisplay.alpha = 1f;

        if (typingSpeed <= 0f)
        {
            textDisplay.text = message;
        }
        else
        {
            foreach (char letter in message)
            {
                textDisplay.text += letter;
                yield return new WaitForSeconds(typingSpeed);
            }
        }

        yield return new WaitForSeconds(messageDisplayTime);

        float fadeOutTime = 0.3f;
        t = 0f;

        while (t < fadeOutTime)
        {
            t += Time.deltaTime;
            textDisplay.alpha = Mathf.Lerp(1f, 0f, t / fadeOutTime);
            yield return null;
        }

        textDisplay.alpha = 0f;
        textDisplay.text = "";
    }

    private IEnumerator TeleportRoutine(GameObject playerObject)
    {
        if (portalAudio != null)
        {
            portalAudio.Play();
        }

        if (screenFade != null)
        {
            yield return screenFade.FadeOut();
        }
        else
        {
            yield return new WaitForSeconds(teleportDelay);
        }

        if (playerObject == null)
        {
            yield break;
        }

        if (targetPortal == null)
        {
            Debug.LogError("[PortalTeleport] Target Portal is not assigned.");
            yield break;
        }

        CharacterController characterController = playerObject.GetComponent<CharacterController>();

        Vector3 flatForward = Vector3.ProjectOnPlane(targetPortal.forward, Vector3.up).normalized;

        if (flatForward == Vector3.zero)
        {
            flatForward = targetPortal.forward;
        }

        Vector3 basePosition = targetPortal.position + flatForward * exitDistance;
        Vector3 rayOrigin = basePosition + Vector3.up * groundRayHeight;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundRayDistance, groundMask))
        {
            basePosition = hit.point;
        }

        basePosition += Vector3.up * 0.1f;

        if (characterController != null)
        {
            characterController.enabled = false;
        }

        playerObject.transform.position = basePosition;
        playerObject.transform.rotation = Quaternion.LookRotation(flatForward, Vector3.up);

        if (characterController != null)
        {
            characterController.enabled = true;
        }

        if (musicManager != null)
        {
            if (destinationEnvironment == DestinationEnvironment.America)
            {
                musicManager.PlayAmericaMusic();
            }
            else if (destinationEnvironment == DestinationEnvironment.Japan)
            {
                musicManager.PlayJapanMusic();
            }
        }

        if (distanceTracker != null && !string.IsNullOrEmpty(destinationCountryName))
        {
            distanceTracker.SwitchToCountry(destinationCountryName);
        }

        PlayNextMessage();

        if (screenFade != null)
        {
            yield return screenFade.FadeIn();
        }
    }
}