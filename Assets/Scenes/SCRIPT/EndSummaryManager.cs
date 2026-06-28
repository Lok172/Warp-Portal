using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EndSummaryManager : MonoBehaviour
{
    public static EndSummaryManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject summaryPanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI contentText;
    public TextMeshProUGUI badgeText;
    public Button closeButton;

    [Header("Summary Content")]
    public string title = "Congratulations!";

    [TextArea(2, 4)]
    public string badgeMessage = "Reward Unlocked:\nCulture Explorer Badge";

    private void Awake()
    {
        Instance = this;
        ResolveReferences();
        BindCloseButton();
        ApplyLayoutAndStyle();
    }

    private void Start()
    {
        ResolveReferences();
        BindCloseButton();
        ApplyLayoutAndStyle();

        if (summaryPanel != null)
        {
            summaryPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (summaryPanel == null)
            return;

        if (!summaryPanel.activeSelf)
            return;

        if (Input.GetKeyDown(KeyCode.Escape) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.Space))
        {
            CloseSummary();
        }
    }

    public void ShowSummary()
    {
        ResolveReferences();
        BindCloseButton();
        ApplyLayoutAndStyle();

        if (summaryPanel == null || titleText == null || contentText == null)
        {
            Debug.LogError("[EndSummaryManager] Summary UI references are missing.");
            return;
        }

        titleText.text = title;

        if (badgeText != null)
        {
            badgeText.text = badgeMessage;
        }

        contentText.text =
            "You completed the Travel & Culture Explorer!\n\n" +
            "Countries Visited:\n" +
            "Japan, America\n\n" +
            "Objects Collected:\n" +
            "Torii Gate, Sakura Tree, Sushi\n" +
            "Bald Eagle, Baseball, Hamburger\n\n" +
            "Landmarks Visited:\n" +
            "Tokyo Tower, Statue of Liberty\n\n" +
            "Quiz Performance:\n" +
            "Japan: Completed\n" +
            "America: Completed";

        summaryPanel.SetActive(true);
        summaryPanel.transform.SetAsLastSibling();

        if (closeButton != null)
        {
            closeButton.transform.SetAsLastSibling();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseSummary()
    {
        if (summaryPanel != null)
        {
            summaryPanel.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ResolveReferences()
    {
        if (summaryPanel == null)
        {
            summaryPanel = FindSceneObject("EndSummaryPanel");
        }

        if (summaryPanel != null)
        {
            if (titleText == null)
            {
                Transform t = FindChildRecursive(summaryPanel.transform, "SummaryTitleText");
                if (t != null)
                    titleText = t.GetComponent<TextMeshProUGUI>();
            }

            if (contentText == null)
            {
                Transform t = FindChildRecursive(summaryPanel.transform, "SummaryContentText");
                if (t != null)
                    contentText = t.GetComponent<TextMeshProUGUI>();
            }

            if (badgeText == null)
            {
                Transform t = FindChildRecursive(summaryPanel.transform, "BadgeText");
                if (t != null)
                    badgeText = t.GetComponent<TextMeshProUGUI>();
            }

            if (closeButton == null)
            {
                Transform t = FindChildRecursive(summaryPanel.transform, "CloseButton");
                if (t != null)
                    closeButton = t.GetComponent<Button>();
            }
        }
    }

    private void BindCloseButton()
    {
        if (closeButton == null)
            return;

        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(CloseSummary);
    }

    private void ApplyLayoutAndStyle()
    {
        if (summaryPanel == null)
            return;

        RectTransform panelRect = summaryPanel.GetComponent<RectTransform>();

        if (panelRect != null)
        {
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(900f, 600f);
        }

        Image panelImage = summaryPanel.GetComponent<Image>();

        if (panelImage != null)
        {
            panelImage.color = new Color(0.04f, 0.12f, 0.22f, 0.88f);
            panelImage.raycastTarget = true;
        }

        if (titleText != null)
        {
            RectTransform rect = titleText.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 230f);
            rect.sizeDelta = new Vector2(760f, 60f);

            titleText.fontSize = 38f;
            titleText.color = Color.white;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.enableWordWrapping = true;
        }

        if (badgeText != null)
        {
            RectTransform rect = badgeText.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 155f);
            rect.sizeDelta = new Vector2(760f, 80f);

            badgeText.fontSize = 28f;
            badgeText.color = new Color(1f, 0.88f, 0.25f, 1f);
            badgeText.alignment = TextAlignmentOptions.Center;
            badgeText.enableWordWrapping = true;
        }

        if (contentText != null)
        {
            RectTransform rect = contentText.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -75f);
            rect.sizeDelta = new Vector2(800f, 350f);

            contentText.fontSize = 23f;
            contentText.color = Color.white;
            contentText.alignment = TextAlignmentOptions.Center;
            contentText.enableWordWrapping = true;
        }

        if (closeButton != null)
        {
            RectTransform rect = closeButton.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-18f, -18f);
            rect.sizeDelta = new Vector2(48f, 48f);

            closeButton.interactable = true;
            closeButton.transform.SetAsLastSibling();
        }
    }

    private GameObject FindSceneObject(string objectName)
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            if (obj == null)
                continue;

            if (!obj.scene.IsValid())
                continue;

            if (obj.name == objectName)
                return obj;
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
}