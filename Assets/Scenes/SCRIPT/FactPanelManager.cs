using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FactPanelManager : MonoBehaviour
{
    public static FactPanelManager Instance { get; private set; }

    [Header("UI References - can be empty, script will auto find")]
    public GameObject factPanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public Button closeButton;

    [Header("Animation Settings")]
    public float animationDuration = 0.25f;

    private Coroutine _scaleCoroutine;
    private Action _onPanelClosedCallback;

    private void Awake()
    {
        Instance = this;
        ResolveReferences(true);
    }

    private void Start()
    {
        ResolveReferences(true);

        if (factPanel != null)
            factPanel.SetActive(false);

        LockCursor();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ShowFact(string title, string description, Action onClose = null)
    {
        if (!ResolveReferences(true))
        {
            Debug.LogError("[FactPanelManager] Cannot show fact. UI references are missing.");
            return;
        }

        titleText.text = title;
        descriptionText.text = description;
        _onPanelClosedCallback = onClose;

        factPanel.SetActive(true);
        factPanel.transform.SetAsLastSibling();

        UnlockCursor();

        if (_scaleCoroutine != null)
            StopCoroutine(_scaleCoroutine);

        _scaleCoroutine = StartCoroutine(ScalePanel(Vector3.zero, Vector3.one, false));
    }

    public void ClosePanel()
    {
        if (!ResolveReferences(true))
        {
            Debug.LogError("[FactPanelManager] Cannot close fact panel. UI references are missing.");
            return;
        }

        LockCursor();

        if (_scaleCoroutine != null)
            StopCoroutine(_scaleCoroutine);

        _scaleCoroutine = StartCoroutine(ClosePanelRoutine());
    }

    private IEnumerator ClosePanelRoutine()
    {
        yield return StartCoroutine(ScalePanel(factPanel.transform.localScale, Vector3.zero, true));

        Action callback = _onPanelClosedCallback;
        _onPanelClosedCallback = null;

        callback?.Invoke();
    }

    private IEnumerator ScalePanel(Vector3 startScale, Vector3 endScale, bool closeAtEnd)
    {
        if (factPanel == null)
            yield break;

        float elapsedTime = 0f;
        factPanel.transform.localScale = startScale;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / animationDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            if (factPanel == null)
                yield break;

            factPanel.transform.localScale = Vector3.Lerp(startScale, endScale, smoothT);
            yield return null;
        }

        if (factPanel == null)
            yield break;

        factPanel.transform.localScale = endScale;

        if (closeAtEnd)
            factPanel.SetActive(false);
    }

    private bool ResolveReferences(bool showLog)
    {
        // Always repair broken references
        if (factPanel == null)
        {
            RectTransform panelRect = FindFactPanelInScene();
            if (panelRect != null)
                factPanel = panelRect.gameObject;
        }

        if (factPanel != null)
        {
            if (titleText == null)
            {
                Transform t = FindChildRecursive(factPanel.transform, "titleText");
                if (t != null)
                    titleText = t.GetComponent<TextMeshProUGUI>();
            }

            if (descriptionText == null)
            {
                Transform t = FindChildRecursive(factPanel.transform, "DescriptionText");
                if (t != null)
                    descriptionText = t.GetComponent<TextMeshProUGUI>();
            }

            if (closeButton == null)
            {
                Transform t = FindChildRecursive(factPanel.transform, "CloseButton");
                if (t != null)
                    closeButton = t.GetComponent<Button>();
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(ClosePanel);
            }
        }

        bool ok = factPanel != null && titleText != null && descriptionText != null;

        if (!ok && showLog)
        {
            Debug.LogError(
                "[FactPanelManager] Auto find failed. Make sure hierarchy is: Canvas > FactPanel > titleText / DescriptionText / CloseButton"
            );
        }

        return ok;
    }

    private RectTransform FindFactPanelInScene()
    {
        RectTransform[] allRects = Resources.FindObjectsOfTypeAll<RectTransform>();

        foreach (RectTransform rect in allRects)
        {
            if (rect == null)
                continue;

            if (!rect.gameObject.scene.IsValid())
                continue;

            if (rect.name != "FactPanel")
                continue;

            Transform title = FindChildRecursive(rect.transform, "titleText");
            Transform desc = FindChildRecursive(rect.transform, "DescriptionText");

            if (title != null && desc != null)
                return rect;
        }

        return null;
    }

    private Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            Transform result = FindChildRecursive(child, childName);
            if (result != null)
                return result;
        }

        return null;
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}