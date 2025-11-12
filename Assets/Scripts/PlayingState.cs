using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayingState : GameState
{
    [SerializeField]
    private GoalKeeperController goalKeeper;

    [SerializeField]
    private BallController ball;

    [SerializeField]
    private JSONLoader jsonLoader;

    [SerializeField]
    private TextMeshProUGUI questionsText;

    [SerializeField]
    private TextAnimator textAnimator;

    [SerializeField]
    private List<TextMeshProUGUI> answersText;

    [SerializeField]
    private List<Button> answerButtons;

    [SerializeField]
    private GameObject mainPanel;

    [SerializeField]
    private Camera mainCamera;

    [SerializeField]
    private Camera questionCamera;

    private const int OptionsPerQuestion = 6;
    private List<Question> questions;
    private int totalQuestions = 3;
    private int currentQuestion = 0;
    private int questionsGuessed = 0;

    private string[] diveAnimations =
    {
        "DiveUpperRight",
        "JumpHitRight",
        "DiveUpperLeft",
        "DiveRight",
        "JumpHitRight",
        "DiveLeft",
    };

    private bool isPlayable = false;
    private readonly WaitForSeconds answerFeedbackDelay = new WaitForSeconds(1.5f);
    private readonly WaitForSeconds shotMessageDelay = new WaitForSeconds(0.5f);

    void Start()
    {
        mainPanel.SetActive(false);

        // Asegurar estado inicial
        if (mainCamera != null)
            mainCamera.enabled = true;
        if (questionCamera != null)
            questionCamera.enabled = false;

        SetupAnswerButtons();
        ToggleTextAnimator(false);
    }

    public override IEnumerator EnterState()
    {
        if (!ValidateSetup())
        {
            OnStateCompleted?.Invoke(score);
            yield break;
        }

        questionsGuessed = 0;
        currentQuestion = 0;
        score = 0;
        questions = jsonLoader.LoadQuestionsFromJSON();

        if (questions == null || questions.Count == 0)
        {
            Debug.LogError("PlayingState no pudo cargar preguntas válidas.");
            OnStateCompleted?.Invoke(score);
            yield break;
        }

        ShowQuestion();
        yield return null;
    }

    void Update()
    {
        if (isPlayable && questions != null && currentQuestion < questions.Count)
        {
            int playableOptions = Mathf.Min(OptionsPerQuestion, answersText.Count);
            for (int i = 0; i < playableOptions; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    RequestAnswer(i);
                    break;
                }
            }
        }
    }

    private void ShowQuestion()
    {
        if (currentQuestion >= questions.Count || currentQuestion >= totalQuestions)
        {
            OnStateCompleted?.Invoke(score);
            return;
        }

        goalKeeper.ResetToIdle();
        ball.ResetBallPosition();

        // Activar cámara de preguntas
        SwitchToQuestionCamera(true);

        mainPanel.SetActive(true);
        questionsText.text = questions[currentQuestion].question;

        for (int i = 0; i < answersText.Count; i++)
        {
            if (i < questions[currentQuestion].answers.Count)
            {
                answersText[i].text = questions[currentQuestion].answers[i];
            }
            else
            {
                answersText[i].text = string.Empty;
            }

            answersText[i].color = Color.white;
        }

        isPlayable = true;
        SetButtonsState(true);
    }

    private IEnumerator HandleAnswer(int answer)
    {
        if (questions == null || currentQuestion >= questions.Count)
        {
            yield break;
        }

        if (answer >= questions[currentQuestion].answers.Count)
        {
            Debug.LogError($"La pregunta actual no tiene la opción {answer}.");
            yield break;
        }

        // No ocultamos el panel aún: necesitamos ver el color en la cámara de preguntas
        isPlayable = false;
        SetButtonsState(false);

        // Determinar si es correcta la respuesta
        string currentAnswer = questions[currentQuestion].answer;
        string selectedAnswer = questions[currentQuestion].answers[answer];
        bool isCorrect = currentAnswer == selectedAnswer;

        // Asegurar que hay label para pintar
        if (answer < answersText.Count)
        {
            // Pintar la seleccionada (verde si acierto, rojo si falla)
            answersText[answer].color = isCorrect ? Color.green : Color.red;
        }

        // ⏳ Primera espera extendida: 1.5s mostrando el color en la cámara de preguntas
        yield return answerFeedbackDelay;

        // Ahora sí: cambiar a la cámara principal para el tiro/atajada
        SwitchToQuestionCamera(false);

        // Regresar el color a blanco tras el cambio de cámara
        if (answer < answersText.Count)
        {
            answersText[answer].color = Color.white;
        }

        // Ocultar panel para la secuencia de tiro
        mainPanel.SetActive(false);

        // Animaciones y lógica de juego
        if (answer < diveAnimations.Length)
        {
            goalKeeper.StartDive(diveAnimations[answer]);
        }
        else
        {
            Debug.LogError($"No hay animación configurada para la opción {answer}.");
        }
        ball.Shoot(answer, isCorrect);

        if (isCorrect)
            score++;

        yield return shotMessageDelay;

        string message = isCorrect ? "¡Goool!" : "¡Fallaste!";
        ToggleTextAnimator(true);
        textAnimator.displayAnimation(message);

        yield return answerFeedbackDelay;
        ToggleTextAnimator(false);

        currentQuestion++;
        ShowQuestion();
    }

    private void SwitchToQuestionCamera(bool showQuestionCam)
    {
        if (mainCamera == null || questionCamera == null)
            return;

        mainCamera.enabled = !showQuestionCam;
        questionCamera.enabled = showQuestionCam;
    }

    private bool ValidateSetup()
    {
        bool isValid = true;

        if (jsonLoader == null)
        {
            Debug.LogError("JSONLoader no está asignado en PlayingState.");
            isValid = false;
        }

        if (goalKeeper == null)
        {
            Debug.LogError("GoalKeeperController no está asignado en PlayingState.");
            isValid = false;
        }

        if (ball == null)
        {
            Debug.LogError("BallController no está asignado en PlayingState.");
            isValid = false;
        }
        else if (!ball.HasTargetsForOptions(OptionsPerQuestion))
        {
            isValid = false;
        }

        if (answersText == null || answersText.Count < OptionsPerQuestion)
        {
            Debug.LogError(
                $"Se requieren {OptionsPerQuestion} labels para respuestas en PlayingState."
            );
            isValid = false;
        }

        if (answerButtons == null || answerButtons.Count < OptionsPerQuestion)
        {
            Debug.LogError(
                $"Se requieren {OptionsPerQuestion} botones para activar las respuestas."
            );
            isValid = false;
        }

        if (diveAnimations == null || diveAnimations.Length < OptionsPerQuestion)
        {
            Debug.LogError(
                $"Se requieren {OptionsPerQuestion} animaciones configuradas para el portero."
            );
            isValid = false;
        }

        return isValid;
    }

    private void ToggleTextAnimator(bool enable)
    {
        if (textAnimator == null)
        {
            return;
        }

        GameObject animatorObject = textAnimator.gameObject;
        if (animatorObject != null)
        {
            animatorObject.SetActive(enable);
        }
    }

    private void SetupAnswerButtons()
    {
        if (answerButtons == null)
        {
            return;
        }

        for (int i = 0; i < answerButtons.Count; i++)
        {
            Button button = answerButtons[i];
            if (button == null)
            {
                continue;
            }

            int optionIndex = i;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => RequestAnswer(optionIndex));
            button.interactable = false;
        }
    }

    private void RequestAnswer(int optionIndex)
    {
        if (!isPlayable)
        {
            return;
        }

        if (optionIndex < 0 || optionIndex >= OptionsPerQuestion)
        {
            return;
        }

        StartCoroutine(HandleAnswer(optionIndex));
    }

    private void SetButtonsState(bool enable)
    {
        if (answerButtons == null)
        {
            return;
        }

        for (int i = 0; i < answerButtons.Count; i++)
        {
            Button button = answerButtons[i];
            if (button == null)
            {
                continue;
            }

            bool hasAnswer = questions != null && currentQuestion < (questions?.Count ?? 0) && i < questions[currentQuestion].answers.Count;
            button.interactable = enable && hasAnswer;
        }
    }
}
