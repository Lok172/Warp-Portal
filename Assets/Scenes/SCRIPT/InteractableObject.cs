using UnityEngine;

[RequireComponent(typeof(Collider))]
public class InteractableObject : MonoBehaviour
{
    [Header("Fact Board Info")]
    public string objectTitle;

    [TextArea(3, 5)]
    public string objectDescription;

    [Header("Settings")]
    public bool isLandmark = false;

    [Tooltip("Only required for landmark. Must match country name in ItemDistanceTracker and QuizManager.")]
    public string countryName;

    private bool _hasBeenTriggered = false;

    [Header("物体互动声音")]
    public AudioClip collisionSound;  // 在Inspector里拖入你想要的声音文件
    private AudioSource audioSource;

    void Start()
    {
        // 获取或添加一个AudioSource组件
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        // 设置这个声音为3D音效，这样听起来会从物体位置发出
        audioSource.spatialBlend = 100f;
    }

    private void Awake()
    {
        Collider collider = GetComponent<Collider>();

        if (collider != null && !collider.isTrigger)
        {
            Debug.LogWarning($"{gameObject.name}: Collider should be set to Is Trigger.");
        }

        ProximityCollector proximityCollector = GetComponent<ProximityCollector>();

        if (proximityCollector != null)
        {
            proximityCollector.enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (_hasBeenTriggered)
        {
            return;
        }

        if (other.gameObject.CompareTag("Player") && collisionSound != null)
        {
            audioSource.PlayOneShot(collisionSound);
        }

        _hasBeenTriggered = true;

        if (FactPanelManager.Instance == null)
        {
            Debug.LogError("FactPanelManager not found in scene.");
            _hasBeenTriggered = false;
            return;
        }

        if (isLandmark)
        {
            HandleLandmark();
        }
        else
        {
            HandleCollectibleItem();
        }
    }

    private void HandleCollectibleItem()
    {
        FactPanelManager.Instance.ShowFact(objectTitle, objectDescription, () =>
        {
            MarkItemAsFoundOnly();
            _hasBeenTriggered = false;
        });
    }

    private void HandleLandmark()
    {
        bool allItemsFound = CheckAllItemsFound();

        if (allItemsFound)
        {
            FactPanelManager.Instance.ShowFact(objectTitle, objectDescription, () =>
            {
                if (QuizManager.Instance != null)
                {
                    QuizManager.Instance.StartQuiz(countryName);
                }
                else
                {
                    Debug.LogError("QuizManager not found in scene.");
                }

                _hasBeenTriggered = false;
            });
        }
        else
        {
            FactPanelManager.Instance.ShowFact(objectTitle, objectDescription, () =>
            {
                _hasBeenTriggered = false;
            });
        }
    }

    private bool CheckAllItemsFound()
    {
        if (string.IsNullOrEmpty(countryName))
        {
            return false;
        }

        ItemDistanceTracker tracker = Object.FindFirstObjectByType<ItemDistanceTracker>();

        if (tracker == null)
        {
            return false;
        }

        foreach (var country in tracker.countries)
        {
            if (country.countryName != countryName)
            {
                continue;
            }

            if (country.items == null)
            {
                return false;
            }

            foreach (var item in country.items)
            {
                if (!item.isFound)
                {
                    return false;
                }
            }

            return true;
        }

        return false;
    }

    private void MarkItemAsFoundOnly()
    {
        ItemDistanceTracker tracker = Object.FindFirstObjectByType<ItemDistanceTracker>();

        if (tracker == null)
        {
            return;
        }

        foreach (var country in tracker.countries)
        {
            if (country.items == null)
            {
                continue;
            }

            foreach (var item in country.items)
            {
                if (item.itemTransform == this.transform)
                {
                    tracker.MarkFound(country.countryName, item.label);
                    return;
                }
            }
        }
    }
}