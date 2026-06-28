using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class QuizQuestion
{
    [TextArea(2, 4)]
    public string question;

    public string[] options;

    public int correctAnswerIndex;
}

[System.Serializable]
public class CountryQuizData
{
    public string countryName;

    public List<QuizQuestion> questions;
}

public class QuizManager : MonoBehaviour
{
    public static QuizManager Instance { get; private set; }

    [Header("Quiz Database")]
    public List<CountryQuizData> allQuizzes;

    [Header("UI References")]
    public GameObject quizPanel;
    public TextMeshProUGUI questionText;
    public Button[] optionButtons;

    [Header("Audio SFX")]
    public AudioSource audioSource;
    public AudioClip correctSFX;
    public AudioClip incorrectSFX;

    [Header("Animation Settings")]
    public float animationDuration = 0.25f;

    [Header("Feedback Settings")]
    [TextArea(2, 3)]
    public string wrongAnswerMessage = "Incorrect! Please review the fact board and try again.";

    public float wrongAnswerDisplayTime = 2f;

    [TextArea(2, 3)]
    public string passQuizMessage = "Great job! You passed this country's quiz.";

    public float passMessageDisplayTime = 1.5f;

    private Coroutine _scaleCoroutine;
    private Coroutine _feedbackCoroutine;

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
        if (quizPanel != null)
        {
            quizPanel.SetActive(false);
        }
    }

    public void StartQuiz(string countryName)
    {
        _currentQuiz = allQuizzes.Find(q => q.countryName == countryName);

        if (_currentQuiz == null || _currentQuiz.questions == null || _currentQuiz.questions.Count == 0)
        {
            Debug.LogWarning($"[QuizManager] Unable to find questions for {countryName}!");
            return;
        }

        _currentCountryName = countryName;
        _currentQuestionIndex = 0;
        _score = 0;

        if (_feedbackCoroutine != null)
        {
            StopCoroutine(_feedbackCoroutine);
            _feedbackCoroutine = null;
        }

        quizPanel.SetActive(true);
        UnlockCursor();

        ShowQuestion(_currentQuestionIndex);

        if (_scaleCoroutine != null)
        {
            StopCoroutine(_scaleCoroutine);
        }

        _scaleCoroutine = StartCoroutine(ScalePanel(Vector3.zero, Vector3.one, false));
    }

    private void ShowQuestion(int index)
    {
        if (_currentQuiz == null)
        {
            return;
        }

        if (index < 0 || index >= _currentQuiz.questions.Count)
        {
            return;
        }

        QuizQuestion question = _currentQuiz.questions[index];

        questionText.text = question.question;

        SetButtonsInteractable(true);

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < question.options.Length)
            {
                optionButtons[i].gameObject.SetActive(true);
                optionButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = question.options[i];

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
        if (_currentQuiz == null)
        {
            return;
        }

        if (_currentQuestionIndex < 0 || _currentQuestionIndex >= _currentQuiz.questions.Count)
        {
            return;
        }

        QuizQuestion question = _currentQuiz.questions[_currentQuestionIndex];

        if (choiceIndex == question.correctAnswerIndex)
        {
            _score++;

            if (audioSource != null && correctSFX != null)
            {
                audioSource.PlayOneShot(correctSFX);
            }

            Debug.Log("Correct!");
            NextQuestion();
        }
        else
        {
            if (audioSource != null && incorrectSFX != null)
            {
                audioSource.PlayOneShot(incorrectSFX);
            }

            Debug.Log("Wrong answer, please try again!");

            if (_feedbackCoroutine != null)
            {
                StopCoroutine(_feedbackCoroutine);
            }

            _feedbackCoroutine = StartCoroutine(ShowWrongAnswerFeedback());
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
            Debug.Log($"You passed all questions of {_currentCountryName}. Your score is {_score}/{_currentQuiz.questions.Count}");

            if (GameProgressManager.Instance != null)
            {
                GameProgressManager.Instance.MarkQuizResult(_currentCountryName, _score, _currentQuiz.questions.Count);
            }
            else
            {
                Debug.LogWarning("[QuizManager] GameProgressManager not found. Quiz progress was not saved.");
            }

            if (_feedbackCoroutine != null)
            {
                StopCoroutine(_feedbackCoroutine);
            }

            _feedbackCoroutine = StartCoroutine(ShowPassFeedback());
        }
    }

    private IEnumerator ShowWrongAnswerFeedback()
    {
        SetButtonsInteractable(false);
        HideOptionButtons();

        questionText.text = wrongAnswerMessage;

        yield return new WaitForSeconds(wrongAnswerDisplayTime);

        CloseQuizPanel();
    }

    private IEnumerator ShowPassFeedback()
    {
        SetButtonsInteractable(false);
        HideOptionButtons();

        questionText.text = $"{passQuizMessage}\nScore: {_score}/{_currentQuiz.questions.Count}";

        yield return new WaitForSeconds(passMessageDisplayTime);

        CloseQuizPanel();
    }

    private void HideOptionButtons()
    {
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] != null)
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void SetButtonsInteractable(bool value)
    {
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] != null)
            {
                optionButtons[i].interactable = value;
            }
        }
    }

    private void CloseQuizPanel()
    {
        LockCursor();

        if (_scaleCoroutine != null)
        {
            StopCoroutine(_scaleCoroutine);
        }

        _scaleCoroutine = StartCoroutine(ScalePanel(quizPanel.transform.localScale, Vector3.zero, true));
    }

    private IEnumerator ScalePanel(Vector3 startScale, Vector3 endScale, bool closeAtEnd)
    {
        float elapsedTime = 0f;
        quizPanel.transform.localScale = startScale;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / animationDuration);
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