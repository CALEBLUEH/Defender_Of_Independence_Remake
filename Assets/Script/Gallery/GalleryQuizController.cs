using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[Serializable]
public sealed class GalleryQuizQuestion
{
    [SerializeField, TextArea(2, 4)] private string question;
    [SerializeField] private string[] answers = new string[4];
    [SerializeField, Range(0, 3)] private int correctAnswerIndex;

    public string Question => question;
    public string[] Answers => answers;
    public int CorrectAnswerIndex => correctAnswerIndex;
}

public sealed class GalleryQuizController : MonoBehaviour
{
    [SerializeField] private GalleryFirstPersonController playerController;
    [SerializeField] private CanvasGroup panelGroup;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private Button[] answerButtons;
    [SerializeField] private TMP_Text[] answerLabels;
    [SerializeField] private GameObject answersRoot;
    [SerializeField] private GameObject resultRoot;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private GalleryQuizQuestion[] questions;

    private readonly int[] answerOrder = { 0, 1, 2, 3 };
    private int questionIndex;
    private int correctCount;

    public event Action<int, int> Completed;

    public bool IsOpen { get; private set; }
    public int CorrectCount => correctCount;
    public int QuestionIndex => questionIndex;
    public int QuestionCount => questions != null ? questions.Length : 0;
    public bool IsComplete => IsOpen && resultRoot != null && resultRoot.activeSelf;
    public int CurrentCorrectDisplaySlot
    {
        get
        {
            if (!IsOpen || IsComplete || questions == null || questionIndex >= questions.Length) return -1;
            return Array.IndexOf(answerOrder, questions[questionIndex].CorrectAnswerIndex);
        }
    }

    private void Awake()
    {
        for (int index = 0; index < answerButtons.Length; index++)
        {
            int capturedIndex = index;
            answerButtons[index].onClick.AddListener(() => SelectAnswer(capturedIndex));
        }
        retryButton?.onClick.AddListener(BeginQuiz);
        continueButton?.onClick.AddListener(ConfirmResult);
        quitButton?.onClick.AddListener(Close);
        SetVisible(false);
    }

    private void Update()
    {
        if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
    }

    public bool Open()
    {
        if (IsOpen || questions == null || questions.Length == 0) return false;
        IsOpen = true;
        playerController?.SetControlsEnabled(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SetVisible(true);
        BeginQuiz();
        return true;
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        SetVisible(false);
        playerController?.SetControlsEnabled(true);
    }

    public void ConfirmResult()
    {
        if (!IsComplete) return;
        int finalCorrect = correctCount;
        int finalTotal = QuestionCount;
        Close();
        Completed?.Invoke(finalCorrect, finalTotal);
    }

    private void BeginQuiz()
    {
        questionIndex = 0;
        correctCount = 0;
        answersRoot.SetActive(true);
        resultRoot.SetActive(false);
        ShowQuestion();
    }

    private void ShowQuestion()
    {
        GalleryQuizQuestion current = questions[questionIndex];
        progressText.text = $"QUESTION {questionIndex + 1} OF {questions.Length}";
        questionText.text = current.Question;
        for (int index = 0; index < answerOrder.Length; index++) answerOrder[index] = index;
        for (int index = answerOrder.Length - 1; index > 0; index--)
        {
            int swapIndex = UnityEngine.Random.Range(0, index + 1);
            (answerOrder[index], answerOrder[swapIndex]) = (answerOrder[swapIndex], answerOrder[index]);
        }
        for (int slot = 0; slot < answerButtons.Length; slot++)
        {
            bool valid = slot < current.Answers.Length;
            answerButtons[slot].gameObject.SetActive(valid);
            if (valid) answerLabels[slot].text = current.Answers[answerOrder[slot]];
        }
    }

    public void SelectAnswer(int displaySlot)
    {
        if (!IsOpen || !answersRoot.activeSelf || displaySlot < 0 || displaySlot >= answerOrder.Length) return;
        if (answerOrder[displaySlot] == questions[questionIndex].CorrectAnswerIndex) correctCount++;
        questionIndex++;
        if (questionIndex < questions.Length)
        {
            ShowQuestion();
            return;
        }

        answersRoot.SetActive(false);
        resultRoot.SetActive(true);
        progressText.text = "QUIZ COMPLETE";
        questionText.text = "Your knowledge of Malaya's journey to independence";
        float percentage = questions.Length > 0 ? correctCount * 100f / questions.Length : 0f;
        resultText.text = $"SCORE\n{correctCount} / {questions.Length}\n{percentage:0}%";
    }

    private void SetVisible(bool visible)
    {
        if (panelGroup == null) return;
        panelGroup.gameObject.SetActive(true);
        panelGroup.alpha = visible ? 1f : 0f;
        panelGroup.interactable = visible;
        panelGroup.blocksRaycasts = visible;
    }
}
