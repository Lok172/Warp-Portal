using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameProgressManager : MonoBehaviour
{
    public static GameProgressManager Instance { get; private set; }

    [Header("References")]
    public ItemDistanceTracker itemDistanceTracker;
    public EndSummaryManager endSummaryManager;

    [Header("Final Summary Settings")]
    public bool showFinalSummaryAutomatically = true;
    public float finalSummaryDelay = 1.8f;

    public string[] requiredCountriesForFinalSummary =
    {
        "Japan",
        "America"
    };

    [Header("Debug")]
    public bool showDebugLogs = true;

    private readonly HashSet<string> passedQuizzes = new HashSet<string>();
    private readonly Dictionary<string, QuizResult> quizResults = new Dictionary<string, QuizResult>();

    private bool summaryShown = false;

    private class QuizResult
    {
        public int score;
        public int total;

        public QuizResult(int score, int total)
        {
            this.score = score;
            this.total = total;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (itemDistanceTracker == null)
        {
            itemDistanceTracker = Object.FindFirstObjectByType<ItemDistanceTracker>();
        }

        if (endSummaryManager == null)
        {
            endSummaryManager = Object.FindFirstObjectByType<EndSummaryManager>();
        }
    }

    public void MarkQuizPassed(string countryName)
    {
        MarkQuizResult(countryName, 0, 0);
    }

    public void MarkQuizResult(string countryName, int score, int total)
    {
        if (string.IsNullOrEmpty(countryName))
        {
            return;
        }

        passedQuizzes.Add(countryName);

        if (total > 0)
        {
            quizResults[countryName] = new QuizResult(score, total);
        }

        if (showDebugLogs)
        {
            Debug.Log($"[GameProgressManager] Quiz passed: {countryName} ({score}/{total})");
        }

        TryShowFinalSummary();
    }

    public bool HasQuizPassed(string countryName)
    {
        if (string.IsNullOrEmpty(countryName))
        {
            return false;
        }

        return passedQuizzes.Contains(countryName);
    }

    public bool AreAllItemsFound(string countryName)
    {
        if (itemDistanceTracker == null)
        {
            itemDistanceTracker = Object.FindFirstObjectByType<ItemDistanceTracker>();
        }

        if (itemDistanceTracker == null || itemDistanceTracker.countries == null)
        {
            return false;
        }

        foreach (var country in itemDistanceTracker.countries)
        {
            if (country.countryName != countryName)
            {
                continue;
            }

            if (country.items == null || country.items.Length == 0)
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

    public bool IsCountryCompleted(string countryName)
    {
        return AreAllItemsFound(countryName) && HasQuizPassed(countryName);
    }

    public string GetMissingRequirementMessage(string countryName)
    {
        bool allItemsFound = AreAllItemsFound(countryName);
        bool quizPassed = HasQuizPassed(countryName);

        if (!allItemsFound)
        {
            return $"You must collect all cultural items in {countryName} before entering this portal.";
        }

        if (!quizPassed)
        {
            return $"You must complete the {countryName} quiz correctly before entering this portal.";
        }

        return "";
    }

    private void TryShowFinalSummary()
    {
        if (!showFinalSummaryAutomatically)
        {
            return;
        }

        if (summaryShown)
        {
            return;
        }

        if (!AreRequiredCountriesCompleted())
        {
            return;
        }

        summaryShown = true;
        StartCoroutine(ShowFinalSummaryAfterDelay());
    }

    private bool AreRequiredCountriesCompleted()
    {
        if (requiredCountriesForFinalSummary == null || requiredCountriesForFinalSummary.Length == 0)
        {
            return false;
        }

        foreach (string countryName in requiredCountriesForFinalSummary)
        {
            if (!IsCountryCompleted(countryName))
            {
                return false;
            }
        }

        return true;
    }

    private IEnumerator ShowFinalSummaryAfterDelay()
    {
        yield return new WaitForSeconds(finalSummaryDelay);

        if (endSummaryManager == null)
        {
            endSummaryManager = Object.FindFirstObjectByType<EndSummaryManager>();
        }

        if (endSummaryManager != null)
        {
            endSummaryManager.ShowSummary();
        }
        else
        {
            Debug.LogWarning("[GameProgressManager] EndSummaryManager not found.");
        }
    }

    public string GetCompletedCountriesText()
    {
        if (requiredCountriesForFinalSummary == null || requiredCountriesForFinalSummary.Length == 0)
        {
            return "Japan\nAmerica";
        }

        List<string> lines = new List<string>();

        foreach (string countryName in requiredCountriesForFinalSummary)
        {
            if (IsCountryCompleted(countryName))
            {
                lines.Add("✓ " + countryName);
            }
            else
            {
                lines.Add("- " + countryName);
            }
        }

        return string.Join("\n", lines);
    }

    public string GetCollectedObjectsText()
    {
        if (itemDistanceTracker == null)
        {
            itemDistanceTracker = Object.FindFirstObjectByType<ItemDistanceTracker>();
        }

        if (itemDistanceTracker == null || itemDistanceTracker.countries == null)
        {
            return "No item data found.";
        }

        List<string> lines = new List<string>();

        foreach (var country in itemDistanceTracker.countries)
        {
            if (country.items == null)
            {
                continue;
            }

            foreach (var item in country.items)
            {
                if (item.isFound)
                {
                    lines.Add("✓ " + item.label);
                }
            }
        }

        if (lines.Count == 0)
        {
            return "No objects collected.";
        }

        return string.Join("\n", lines);
    }

    public string GetQuizPerformanceText()
    {
        List<string> lines = new List<string>();

        foreach (string countryName in passedQuizzes)
        {
            if (quizResults.ContainsKey(countryName))
            {
                QuizResult result = quizResults[countryName];
                lines.Add($"{countryName}: {result.score}/{result.total}");
            }
            else
            {
                lines.Add($"{countryName}: Completed");
            }
        }

        if (lines.Count == 0)
        {
            return "No quiz completed.";
        }

        return string.Join("\n", lines);
    }
}