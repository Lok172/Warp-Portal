using System.Collections;
using UnityEngine;
using TMPro;

[System.Serializable]
public class TrackedItem
{
    [Tooltip("The scene object to track distance to")]
    public Transform itemTransform;

    [Tooltip("Display name shown in the UI, e.g. 'Torii Gate'")]
    public string label = "Item";

    [HideInInspector] public bool isFound = false;
}

[System.Serializable]
public class CountryGroup
{
    [Tooltip("Country identifier, e.g. 'Japan'")]
    public string countryName = "Country";

    [Tooltip("A Transform that marks the center/anchor of this country zone")]
    public Transform countryAnchor;

    [Tooltip("All collectible items that belong to this country. Do NOT put landmarks here.")]
    public TrackedItem[] items;
}

public class ItemDistanceTracker : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("Player character transform, e.g. PlayerArmature")]
    public Transform character;

    [Header("Countries & Items")]
    public CountryGroup[] countries;

    [Header("Display")]
    public TextMeshProUGUI distanceDisplay;
    public TextMeshProUGUI countryDisplay;
    public ScreenFade screenFade;

    [Header("Collection")]
    [Tooltip("Kept for inspector compatibility. Collection is handled by InteractableObject.")]
    public float collectRadius = 2f;

    [Header("Update Settings")]
    [Range(0f, 1f)]
    public float updateInterval = 0.1f;

    public bool roundDistance = true;

    [Header("Item Rotation")]
    public bool enableRotation = true;
    public Vector3 rotationAxis = Vector3.up;

    [Range(0f, 360f)]
    public float rotationSpeed = 45f;

    public bool useUnscaledTime = false;

    private int _activeIndex = 0;
    private int _lastIndex = -1;
    private float _timer;
    private bool _isFading = false;

    private void Start()
    {
        ValidateReferences();

        _activeIndex = FindClosestCountryIndex();
        _lastIndex = _activeIndex;

        SetTextAlpha(1f);
        RefreshCountryDisplay();
        RefreshDisplay();
    }

    private void Update()
    {
        if (!_isFading)
        {
            int closest = FindClosestCountryIndex();

            if (closest != _lastIndex)
            {
                _lastIndex = closest;
                StartCoroutine(FadeSwitch(closest));
            }
        }

        if (enableRotation)
        {
            RotateItems();
        }

        if (updateInterval <= 0f)
        {
            RefreshDisplay();
            return;
        }

        _timer += Time.deltaTime;

        if (_timer >= updateInterval)
        {
            _timer = 0f;
            RefreshDisplay();
        }
    }

    private IEnumerator FadeSwitch(int newIndex)
    {
        _isFading = true;

        float duration = screenFade != null ? screenFade.fadeDuration : 0.3f;

        yield return StartCoroutine(TweenTextAlpha(1f, 0f, duration));

        _activeIndex = newIndex;
        RefreshCountryDisplay();
        RefreshDisplay();

        yield return StartCoroutine(TweenTextAlpha(0f, 1f, duration));

        _isFading = false;
    }

    private IEnumerator TweenTextAlpha(float from, float to, float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            SetTextAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(t / duration)));
            yield return null;
        }

        SetTextAlpha(to);
    }

    private void SetTextAlpha(float alpha)
    {
        if (distanceDisplay != null)
        {
            distanceDisplay.alpha = alpha;
        }

        if (countryDisplay != null)
        {
            countryDisplay.alpha = alpha;
        }
    }

    private int FindClosestCountryIndex()
    {
        if (countries == null || countries.Length == 0 || character == null)
        {
            return 0;
        }

        int bestIndex = 0;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < countries.Length; i++)
        {
            if (countries[i].countryAnchor == null)
            {
                continue;
            }

            float distance = Vector3.Distance(character.position, countries[i].countryAnchor.position);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    public void SwitchToCountry(string countryName)
    {
        if (countries == null)
        {
            return;
        }

        for (int i = 0; i < countries.Length; i++)
        {
            if (countries[i].countryName == countryName)
            {
                _lastIndex = i;
                StartCoroutine(FadeSwitch(i));
                return;
            }
        }

        Debug.LogWarning($"[ItemDistanceTracker] Country '{countryName}' not found.");
    }

    public void MarkFound(string countryName, string label)
    {
        if (countries == null)
        {
            return;
        }

        foreach (var group in countries)
        {
            if (group.countryName != countryName)
            {
                continue;
            }

            if (group.items == null)
            {
                continue;
            }

            foreach (var item in group.items)
            {
                if (item.label != label)
                {
                    continue;
                }

                item.isFound = true;

                // IMPORTANT:
                // Do NOT destroy the item.
                // Player must be able to return and review the fact board again.

                RefreshDisplay();
                return;
            }
        }

        Debug.LogWarning($"[ItemDistanceTracker] Item '{label}' not found in '{countryName}'.");
    }

    public bool IsItemFound(Transform targetTransform)
    {
        if (countries == null || targetTransform == null)
        {
            return false;
        }

        foreach (var group in countries)
        {
            if (group.items == null)
            {
                continue;
            }

            foreach (var item in group.items)
            {
                if (item.itemTransform == targetTransform)
                {
                    return item.isFound;
                }
            }
        }

        return false;
    }

    private CountryGroup ActiveGroup
    {
        get
        {
            if (countries == null || countries.Length == 0)
            {
                return null;
            }

            if (_activeIndex < 0 || _activeIndex >= countries.Length)
            {
                return null;
            }

            return countries[_activeIndex];
        }
    }

    private void RefreshDisplay()
    {
        if (character == null || distanceDisplay == null)
        {
            return;
        }

        var group = ActiveGroup;

        if (group == null || group.items == null || group.items.Length == 0)
        {
            distanceDisplay.text = "";
            return;
        }

        var sb = new System.Text.StringBuilder();

        for (int i = 0; i < group.items.Length; i++)
        {
            var item = group.items[i];

            if (item.isFound)
            {
                sb.AppendLine($"{item.label}: <color=#00FF99>Found</color>");
                continue;
            }

            if (item.itemTransform == null)
            {
                sb.AppendLine($"{item.label}: <color=#FFCC00>Missing</color>");
                continue;
            }

            float distance = Vector3.Distance(character.position, item.itemTransform.position);

            string distanceText = roundDistance
                ? Mathf.RoundToInt(distance) + "m"
                : distance.ToString("F1") + "m";

            sb.AppendLine($"{item.label}: {distanceText}");
        }

        distanceDisplay.text = sb.ToString().TrimEnd();
    }

    private void RefreshCountryDisplay()
    {
        if (countryDisplay == null)
        {
            return;
        }

        var group = ActiveGroup;
        countryDisplay.text = group != null ? group.countryName : "";
    }

    private void RotateItems()
    {
        var group = ActiveGroup;

        if (group == null || group.items == null)
        {
            return;
        }

        float delta = (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) * rotationSpeed;

        foreach (var item in group.items)
        {
            if (item == null)
            {
                continue;
            }

            if (item.itemTransform == null)
            {
                continue;
            }

            item.itemTransform.Rotate(rotationAxis.normalized, delta, Space.World);
        }
    }

    private void ValidateReferences()
    {
        if (character == null)
        {
            Debug.LogWarning("[ItemDistanceTracker] Character is not assigned.");
        }

        if (distanceDisplay == null)
        {
            Debug.LogWarning("[ItemDistanceTracker] Distance Display is not assigned.");
        }

        if (countryDisplay == null)
        {
            Debug.LogWarning("[ItemDistanceTracker] Country Display is not assigned.");
        }

        if (countries == null || countries.Length == 0)
        {
            Debug.LogWarning("[ItemDistanceTracker] No countries assigned.");
        }

        if (screenFade == null)
        {
            Debug.LogWarning("[ItemDistanceTracker] Screen Fade is not assigned.");
        }
    }
}