using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

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
    private GameObject mainPanel;
    private List<Question> questions;
    private int totalQuestions = 3;
    private int currentQuestion = 0;
    private int questionsGuessed = 0;

    private string[] diveAnimations = { "DiveUpperRight", "DiveRight", "JumpHitRight", "JumpHitRight", "DiveUpperLeft", "DiveLeft" };

    private bool isPlayable = false;

    void Start()
    {
        mainPanel.SetActive(false);

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

        mainPanel.SetActive(true);
        questionsText.text = questions[currentQuestion].question;

        for (int i = 0; i < questions[currentQuestion].answers.Count; i++)
        {
            if (i < answersText.Count) // Evitar desbordamiento
            {
                answersText[i].text = questions[currentQuestion].answers[i]; // Asignar correctamente el texto
            }
            else
            {
                Debug.LogWarning("Más respuestas de las esperadas. Verifica el tamaño de answersText.");
            }
        }

        isPlayable = true;
    }

    private IEnumerator HandleAnswer(int answer)
    {
        mainPanel.SetActive(false);
        isPlayable = false;
        yield return new WaitForSeconds(0.5f);
        goalKeeper.StartDive(diveAnimations[answer]);
        string currentAnswer = questions[currentQuestion].answer;
        string selectedAnswer = questions[currentQuestion].answers[answer];
        ball.Shoot(answer, currentAnswer == selectedAnswer);
        currentQuestion++;
        if(currentAnswer == selectedAnswer){
            score++;
        }
        yield return new WaitForSeconds(0.5f);
        string message = currentAnswer == selectedAnswer ? "Goool!!!" : "La volo!!!";
        textAnimator.displayAnimation(message);
        yield return new WaitForSeconds(1.5f);
        ShowQuestion();
        yield return null;
    }

}