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
    [Tooltip("如果是地标，必须填入所属国家的名字（例如 Japan），用于核对物品是否找齐")]
    public string countryName;

    private bool _hasBeenTriggered = false;

    private void Update()
    {
        var autoCollector = GetComponent("ProximityCollector") as MonoBehaviour;
        if (autoCollector != null && autoCollector.enabled)
        {
            autoCollector.enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !_hasBeenTriggered)
        {
            _hasBeenTriggered = true;

            if (isLandmark)
            {
                // 如果是地标，先去查一下这个国家的物品是不是全收集了
                bool allItemsFound = CheckAllItemsFound();

                if (allItemsFound)
                {
                    // 全收集了：显示地标科普，并在玩家点击"Close"的瞬间，触发 Quiz！
                    FactPanelManager.Instance.ShowFact(objectTitle, objectDescription, () =>
                    {
                        QuizManager.Instance.StartQuiz(countryName);
                        _hasBeenTriggered = false; // 答错后还可以回来重新触发
                    });
                }
                else
                {
                    // 没收集完：只显示科普，关闭后无事发生
                    FactPanelManager.Instance.ShowFact(objectTitle, objectDescription);
                    _hasBeenTriggered = false; // 允许玩家反复查看
                }
            }
            else
            {
                // 如果是普通收集物，显示科普并销毁
                FactPanelManager.Instance.ShowFact(objectTitle, objectDescription);
                DestroyAndNotifyTracker();
            }
        }
    }

    // 核心逻辑：去距离追踪器里查数据
    private bool CheckAllItemsFound()
    {
        if (string.IsNullOrEmpty(countryName)) return false;

        ItemDistanceTracker tracker = Object.FindFirstObjectByType<ItemDistanceTracker>();
        if (tracker != null)
        {
            foreach (var country in tracker.countries)
            {
                if (country.countryName == this.countryName)
                {
                    foreach (var item in country.items)
                    {
                        // 只要有任何一个物品没找到，就不达标
                        if (!item.isFound) return false;
                    }
                    return true; // 循环结束还没 return false，说明全找到了！
                }
            }
        }
        return false;
    }

    private void DestroyAndNotifyTracker()
    {
        ItemDistanceTracker tracker = Object.FindFirstObjectByType<ItemDistanceTracker>();
        if (tracker != null)
        {
            foreach (var country in tracker.countries)
            {
                if (country.items == null) continue;
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
        Destroy(gameObject);
    }
}