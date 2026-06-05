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

    public float typingSpeed = 0.05f;

    private int currentMessage = 0;
    private Coroutine typingCoroutine;
    public float messageDisplayTime = 3f;

    private static Dictionary<GameObject, float> cooldownMap = new Dictionary<GameObject, float>();

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        GameObject obj = other.gameObject;

        if (cooldownMap.TryGetValue(obj, out float t) && Time.time < t)
        {
            return;
        }

        StartCoroutine(TeleportRoutine(obj));
    }

    private void PlayNextMessage()
    {
        if (portalMessages.Length == 0 || textDisplay == null)
            return;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(portalMessages[currentMessage]));

        currentMessage++;

        if (currentMessage >= portalMessages.Length)
            currentMessage = 0;
    }

    private IEnumerator TypeText(string message)
    {
        // Start invisible
        textDisplay.text = "";
        textDisplay.alpha = 0f;

        // =========================
        // 1. FADE IN
        // =========================
        float fadeInTime = 0.5f;
        float t = 0f;

        while (t < fadeInTime)
        {
            t += Time.deltaTime;
            textDisplay.alpha = Mathf.Lerp(0f, 1f, t / fadeInTime);
            yield return null;
        }

        textDisplay.alpha = 1f;

        // =========================
        // 2. TYPE TEXT
        // =========================
        foreach (char letter in message)
        {
            textDisplay.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        // =========================
        // 3. HOLD TEXT
        // =========================
        yield return new WaitForSeconds(messageDisplayTime);

        // =========================
        // 4. FADE OUT
        // =========================
        float fadeOutTime = 0.5f;
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

    private IEnumerator TeleportRoutine(GameObject obj)
    {
        // Lock cooldown to prevent repeated trigger
        cooldownMap[obj] = Time.time + cooldown;

        // Play portal sound
        if (portalAudio != null)
        {
            portalAudio.Play();
        }

        // Fade out before teleport
        if (screenFade != null)
        {
            yield return screenFade.FadeOut();
        }
        else
        {
            yield return new WaitForSeconds(teleportDelay);
        }

        if (obj == null) yield break;

        CharacterController cc = obj.GetComponent<CharacterController>();

        // ===============================
        // 1. Calculate exit direction
        // ===============================
        Vector3 flatForward = Vector3.ProjectOnPlane(targetPortal.forward, Vector3.up).normalized;

        if (flatForward == Vector3.zero)
        {
            flatForward = targetPortal.forward;
        }

        Vector3 basePos = targetPortal.position + flatForward * exitDistance;

        // ===============================
        // 2. Ground correction
        // ===============================
        Vector3 rayOrigin = basePos + Vector3.up * groundRayHeight;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundRayDistance, groundMask))
        {
            basePos = hit.point;
        }

        // Slightly lift player to avoid clipping
        basePos += Vector3.up * 0.1f;

        // ===============================
        // 3. Teleport player
        // ===============================
        if (cc != null)
        {
            cc.enabled = false;

            obj.transform.position = basePos;
            obj.transform.rotation = Quaternion.LookRotation(flatForward, Vector3.up);

            cc.enabled = true;
        }
        else
        {
            obj.transform.position = basePos;
            obj.transform.rotation = Quaternion.LookRotation(flatForward, Vector3.up);
        }

        // ===============================
        // 4. Change background music
        // ===============================
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

        PlayNextMessage();

        // Fade in after teleport
        if (screenFade != null)
        {
            yield return screenFade.FadeIn();
        }
    }
}