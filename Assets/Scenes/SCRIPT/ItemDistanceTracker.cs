using System.Collections;
using UnityEngine;
using TMPro;

[System.Serializable]
public class TrackedItem
{
    [Tooltip("The scene object to track distance to and rotate")]
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

    [Tooltip("A Transform that marks the center/anchor of this country zone (e.g. the Japan or America parent GameObject)")]
    public Transform countryAnchor;

    [Tooltip("All items that belong to this country")]
    public TrackedItem[] items;
}

/// <summary>
/// 2D distance tracker.
/// - Auto-attaches ProximityCollector to every item at Start.
/// - Auto-detects active country by proximity to anchor.
/// - Fades distanceDisplay and countryDisplay alpha in sync with ScreenFade
///   so text never pops visibly during a transition.
/// </summary>
public class ItemDistanceTracker : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("Player character transform (e.g. PlayerArmature)")]
    public Transform character;

    [Header("Countries & Items")]
    [Tooltip("One entry per country. Set countryAnchor to the country's parent GameObject.")]
    public CountryGroup[] countries;

    [Header("Display")]
    [Tooltip("TMP text that lists item names and distances / found state")]
    public TextMeshProUGUI distanceDisplay;

    [Tooltip("TMP text that shows the current country name")]
    public TextMeshProUGUI countryDisplay;

    [Tooltip("Assign the ScreenFade in your scene — text will fade with the screen")]
    public ScreenFade screenFade;

    [Header("Collection")]
    [Tooltip("How close the player must get to collect an item (metres)")]
    public float collectRadius = 2f;

    [Header("Update Settings")]
    [Range(0f, 1f)]
    [Tooltip("Seconds between display refreshes. 0 = every frame.")]
    public float updateInterval = 0.1f;

    [Tooltip("Round distances to whole metres")]
    public bool roundDistance = true;

    [Header("Item Rotation")]
    [Tooltip("Rotate items in the active country")]
    public bool enableRotation = true;

    [Tooltip("Axis of rotation (0,1,0 = Y-axis spin)")]
    public Vector3 rotationAxis = Vector3.up;

    [Range(0f, 360f)]
    [Tooltip("Degrees per second")]
    public float rotationSpeed = 45f;

    [Tooltip("Ignore Time.timeScale (keeps spinning while paused)")]
    public bool useUnscaledTime = false;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private int _activeIndex = 0;
    private int _lastIndex = -1;
    private float _timer;
    private bool _isFading = false;   // block country swap while mid-fade

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Start()
    {
        ValidateReferences();
        AutoAttachCollectors();

        _activeIndex = FindClosestCountryIndex();
        _lastIndex = _activeIndex;

        SetTextAlpha(1f);
        RefreshCountryDisplay();
        RefreshDisplay();
    }

    private void Update()
    {
        // Only auto-switch country when not in the middle of a fade
        if (!_isFading)
        {
            int closest = FindClosestCountryIndex();
            if (closest != _lastIndex)
            {
                _lastIndex = closest;
                StartCoroutine(FadeSwitch(closest));
            }
        }

        if (enableRotation) RotateItems();

        if (updateInterval <= 0f) { RefreshDisplay(); return; }

        _timer += Time.deltaTime;
        if (_timer >= updateInterval) { _timer = 0f; RefreshDisplay(); }
    }

    // ── Fade helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Fades text OUT, swaps country data, fades text back IN.
    /// Mirrors ScreenFade.fadeDuration so they stay in sync.
    /// </summary>
    private IEnumerator FadeSwitch(int newIndex)
    {
        _isFading = true;

        float duration = (screenFade != null) ? screenFade.fadeDuration : 0.3f;

        // Fade text OUT
        yield return StartCoroutine(TweenTextAlpha(1f, 0f, duration));

        // Swap country
        _activeIndex = newIndex;
        AutoAttachCollectors();
        RefreshCountryDisplay();
        RefreshDisplay();

        // Fade text IN
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

    /// <summary>Sets alpha on both TMP texts at once.</summary>
    private void SetTextAlpha(float alpha)
    {
        if (distanceDisplay != null) distanceDisplay.alpha = alpha;
        if (countryDisplay != null) countryDisplay.alpha = alpha;
    }

    // ── Auto-attach ───────────────────────────────────────────────────────────

    private void AutoAttachCollectors()
    {
        if (countries == null) return;

        foreach (var group in countries)
        {
            if (group.items == null) continue;

            foreach (var item in group.items)
            {
                if (item.itemTransform == null || item.isFound) continue;

                ProximityCollector existing = item.itemTransform.GetComponent<ProximityCollector>();
                if (existing != null)
                {
                    existing.Setup(this, group.countryName, item.label, character, collectRadius);
                    continue;
                }

                ProximityCollector collector = item.itemTransform.gameObject.AddComponent<ProximityCollector>();
                collector.Setup(this, group.countryName, item.label, character, collectRadius);
            }
        }
    }

    // ── Country detection ─────────────────────────────────────────────────────

    private int FindClosestCountryIndex()
    {
        if (countries == null || countries.Length == 0 || character == null) return 0;

        int bestIndex = 0;
        float bestDist = float.MaxValue;

        for (int i = 0; i < countries.Length; i++)
        {
            if (countries[i].countryAnchor == null) continue;

            float d = Vector3.Distance(character.position, countries[i].countryAnchor.position);
            if (d < bestDist) { bestDist = d; bestIndex = i; }
        }

        return bestIndex;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Optional direct call from PortalTeleport.</summary>
    public void SwitchToCountry(string countryName)
    {
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

    /// <summary>Called by ProximityCollector. Marks item found and destroys its GameObject.</summary>
    public void MarkFound(string countryName, string label)
    {
        if (countries == null) return;

        foreach (var group in countries)
        {
            if (group.countryName != countryName) continue;
            if (group.items == null) continue;

            foreach (var item in group.items)
            {
                if (item.label != label || item.isFound) continue;

                item.isFound = true;

                if (item.itemTransform != null)
                {
                    Object.Destroy(item.itemTransform.gameObject);
                    item.itemTransform = null;
                }

                RefreshDisplay();
                return;
            }
        }
        Debug.LogWarning($"[ItemDistanceTracker] '{label}' not found in '{countryName}'.");
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private CountryGroup ActiveGroup =>
        (countries != null && countries.Length > 0) ? countries[_activeIndex] : null;

    private void RefreshDisplay()
    {
        if (character == null || distanceDisplay == null) return;

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
            }
            else
            {
                if (item.itemTransform == null) continue;

                float dist = Vector3.Distance(character.position, item.itemTransform.position);
                string distStr = roundDistance
                    ? Mathf.RoundToInt(dist) + "m"
                    : dist.ToString("F1") + "m";

                sb.AppendLine($"Object {i + 1} : {distStr}");
            }
        }

        distanceDisplay.text = sb.ToString().TrimEnd();
    }

    private void RefreshCountryDisplay()
    {
        if (countryDisplay == null) return;
        var group = ActiveGroup;
        countryDisplay.text = group != null ? group.countryName : "";
    }

    private void RotateItems()
    {
        var group = ActiveGroup;
        if (group == null || group.items == null) return;

        float delta = (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) * rotationSpeed;

        foreach (var item in group.items)
        {
            if (item.itemTransform == null) continue;
            item.itemTransform.Rotate(rotationAxis.normalized, delta, Space.World);
        }
    }

    private void ValidateReferences()
    {
        if (character == null)
            Debug.LogWarning("[ItemDistanceTracker] 'Character' not assigned!");
        if (distanceDisplay == null)
            Debug.LogWarning("[ItemDistanceTracker] 'Distance Display' not assigned!");
        if (countries == null || countries.Length == 0)
            Debug.LogWarning("[ItemDistanceTracker] No countries assigned!");
        if (screenFade == null)
            Debug.LogWarning("[ItemDistanceTracker] 'Screen Fade' not assigned — text fade will use default 0.3s.");
    }
}