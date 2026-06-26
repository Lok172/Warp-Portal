using System; // 必须引入 System 才能使用 Action
using System.Collections;
using UnityEngine;
using TMPro;

public class FactPanelManager : MonoBehaviour
{
    public static FactPanelManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject factPanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;

    [Header("Animation Settings")]
    public float animationDuration = 0.25f;

    private Coroutine _scaleCoroutine;
    private Action _onPanelClosedCallback; // 用于记录关掉面板后要执行的事情

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        factPanel.SetActive(false);
        LockCursor();
    }

    // 升级：增加了一个可选的 onClose 参数
    public void ShowFact(string title, string description, Action onClose = null)
    {
        titleText.text = title;
        descriptionText.text = description;
        _onPanelClosedCallback = onClose; // 把关闭后要做的事存起来

        factPanel.SetActive(true);
        UnlockCursor();

        if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
        _scaleCoroutine = StartCoroutine(ScalePanel(Vector3.zero, Vector3.one, false));
    }

    public void ClosePanel()
    {
        LockCursor();

        if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
        _scaleCoroutine = StartCoroutine(ScalePanel(factPanel.transform.localScale, Vector3.zero, true));

        // 触发存起来的任务（比如：开始答题）
        _onPanelClosedCallback?.Invoke();
        _onPanelClosedCallback = null; // 执行完清空
    }

    private IEnumerator ScalePanel(Vector3 startScale, Vector3 endScale, bool closeAtEnd)
    {
        float elapsedTime = 0f;
        factPanel.transform.localScale = startScale;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / animationDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            factPanel.transform.localScale = Vector3.Lerp(startScale, endScale, smoothT);
            yield return null;
        }

        factPanel.transform.localScale = endScale;

        if (closeAtEnd)
        {
            factPanel.SetActive(false);
        }
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