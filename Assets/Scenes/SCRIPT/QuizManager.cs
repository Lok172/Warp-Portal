using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// --- 数据结构 ---
[System.Serializable]
public class QuizQuestion
{
    [TextArea(2, 4)]
    public string question;            // 题目内容
    public string[] options;           // 选项文字
    public int correctAnswerIndex;     // 正确答案的序号（从0开始）
}

[System.Serializable]
public class CountryQuizData
{
    public string countryName;         // 国家名字 
    public List<QuizQuestion> questions; // 这个国家的题库
}

// --- 管理器核心 ---
public class QuizManager : MonoBehaviour
{
    public static QuizManager Instance { get; private set; }

    [Header("Quiz Database")]
    public List<CountryQuizData> allQuizzes;

    [Header("UI References")]
    public GameObject quizPanel;
    public TextMeshProUGUI questionText;
    public Button[] optionButtons;

    [Header("Audio (SFX)")]
    public AudioSource audioSource;
    public AudioClip correctSFX;
    public AudioClip incorrectSFX;

    [Header("Animation Settings")]
    [Tooltip("animation duration")]
    public float animationDuration = 0.25f;

    private Coroutine _scaleCoroutine;
    private CountryQuizData _currentQuiz;
    private int _currentQuestionIndex = 0;
    private int _score = 0;
    private string _currentCountryName;

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
        quizPanel.SetActive(false);
    }

    public void StartQuiz(string countryName)
    {
        _currentQuiz = allQuizzes.Find(q => q.countryName == countryName);

        if (_currentQuiz == null || _currentQuiz.questions.Count == 0)
        {
            Debug.LogWarning($"[QuizManager] unable to find questions of {countryName}!");
            return;
        }

        _currentCountryName = countryName;
        _currentQuestionIndex = 0;
        _score = 0;

        // 打开面板，解锁鼠标，并显示第一题
        quizPanel.SetActive(true);
        UnlockCursor();
        ShowQuestion(_currentQuestionIndex);

        // 播放"从小变大"的动画
        if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
        _scaleCoroutine = StartCoroutine(ScalePanel(Vector3.zero, Vector3.one, false));
    }

    private void ShowQuestion(int index)
    {
        QuizQuestion q = _currentQuiz.questions[index];
        questionText.text = q.question;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < q.options.Length)
            {
                optionButtons[i].gameObject.SetActive(true);
                optionButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = q.options[i];

                optionButtons[i].onClick.RemoveAllListeners();

                int choiceIndex = i;
                optionButtons[i].onClick.AddListener(() => OnOptionSelected(choiceIndex));
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void OnOptionSelected(int choiceIndex)
    {
        QuizQuestion q = _currentQuiz.questions[_currentQuestionIndex];

        if (choiceIndex == q.correctAnswerIndex)
        {
            _score++;
            if (audioSource != null && correctSFX != null) audioSource.PlayOneShot(correctSFX);
            Debug.Log("Correct!");
            NextQuestion();
        }
        else
        {
            if (audioSource != null && incorrectSFX != null) audioSource.PlayOneShot(incorrectSFX);
            Debug.Log("Wrong answer, please try again!");
            CloseQuizPanel();
        }
    }

    private void NextQuestion()
    {
        _currentQuestionIndex++;

        if (_currentQuestionIndex < _currentQuiz.questions.Count)
        {
            ShowQuestion(_currentQuestionIndex);
        }
        else
        {
            Debug.Log($"You pass all questions of {_currentCountryName}. Your score is {_score}/{_currentQuiz.questions.Count}");
            CloseQuizPanel();
        }
    }

    private void CloseQuizPanel()
    {
        LockCursor();

        // 播放"从大变小"的动画，动画结束后关闭物体
        if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
        _scaleCoroutine = StartCoroutine(ScalePanel(quizPanel.transform.localScale, Vector3.zero, true));
    }

    private IEnumerator ScalePanel(Vector3 startScale, Vector3 endScale, bool closeAtEnd)
    {
        float elapsedTime = 0f;
        quizPanel.transform.localScale = startScale;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / animationDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            quizPanel.transform.localScale = Vector3.Lerp(startScale, endScale, smoothT);
            yield return null;
        }

        quizPanel.transform.localScale = endScale;

        if (closeAtEnd)
        {
            quizPanel.SetActive(false);
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