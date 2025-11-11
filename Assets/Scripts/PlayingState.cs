using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayingState : GameState
{
    [SerializeField] private GoalKeeperController goalKeeper;
    [SerializeField] private BallController ball;
    [SerializeField] private JSONLoader jsonLoader;
    [SerializeField] private TextMeshProUGUI questionsText;
    [SerializeField] private TextAnimator textAnimator;
    [SerializeField] private List<TextMeshProUGUI> answersText;
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera questionCamera;

    private List<Question> questions;
    private int totalQuestions = 3;
    private int currentQuestion = 0;
    private int questionsGuessed = 0;

    private string[] diveAnimations = 
        { "DiveUpperRight", "JumpHitRight", "DiveUpperLeft", "DiveRight", "JumpHitRight", "DiveLeft" };

    private bool isPlayable = false;

    void Start()
    {
        mainPanel.SetActive(false);

        // Asegurar estado inicial
        if (mainCamera != null) mainCamera.enabled = true;
        if (questionCamera != null) questionCamera.enabled = false;
    }

    public override IEnumerator EnterState()
    {
        questionsGuessed = 0;
        currentQuestion = 0;
        score = 0;
        questions = jsonLoader.LoadQuestionsFromJSON();
        ShowQuestion();
        yield return null;
    }

    void Update()
    {
        if (isPlayable)
        {
            for (int i = 0; i < 6; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    StartCoroutine(HandleAnswer(i));
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

        for (int i = 0; i < questions[currentQuestion].answers.Count; i++)
        {
            if (i < answersText.Count)
                answersText[i].text = questions[currentQuestion].answers[i];
            else
                Debug.LogWarning("Más respuestas de las esperadas. Verifica el tamaño de answersText.");
        }

        isPlayable = true;
    }

    private IEnumerator HandleAnswer(int answer)
    {
        // No ocultamos el panel aún: necesitamos ver el color en la cámara de preguntas
        isPlayable = false;

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
        yield return new WaitForSeconds(1.5f);

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
        goalKeeper.StartDive(diveAnimations[answer]);
        ball.Shoot(answer, isCorrect);

        if (isCorrect) score++;

        yield return new WaitForSeconds(0.5f);

        string message = isCorrect ? "¡Goool!" : "¡Fallaste!";
        textAnimator.displayAnimation(message);

        yield return new WaitForSeconds(1.5f);

        currentQuestion++;
        ShowQuestion();
    }

    private void SwitchToQuestionCamera(bool showQuestionCam)
    {
        if (mainCamera == null || questionCamera == null) return;

        mainCamera.enabled = !showQuestionCam;
        questionCamera.enabled = showQuestionCam;
    }
}