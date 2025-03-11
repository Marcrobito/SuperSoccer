using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private GamePhase gamePhase = GamePhase.Started;
    private bool isChangingState = false; // Flag para evitar múltiples cambios de estado

    [SerializeField] private GameState startedState;
    [SerializeField] private GameState gameOverState;
    [SerializeField] private GameState playingState;

    void Start()
    {
        // Validación para evitar errores por GameStates no asignados
        if (startedState == null || gameOverState == null || playingState == null)
        {
            Debug.LogError("Uno o más GameStates no están asignados en el GameManager.");
            return;
        }

        // Asignar delegados para el cambio de estado
        startedState.OnStateCompleted = GameStateMachine;
        gameOverState.OnStateCompleted = GameStateMachine;
        playingState.OnStateCompleted = GameStateMachine;
    }

    void GameStateMachine(int score)
    {
        if (isChangingState) return; // Evita múltiples ejecuciones al mismo tiempo
        isChangingState = true; 

        switch (gamePhase)
        {
            case GamePhase.Started:
                Debug.Log("Entrando a estado: PLAYING");
                gamePhase = GamePhase.Playing;
                StartCoroutine(ChangeState(playingState));
                break;

            case GamePhase.Playing:
                Debug.Log("Entrando a estado: GAME OVER con score: " + score);
                gamePhase = GamePhase.GameOver;
                gameOverState.score = score;
                StartCoroutine(ChangeState(gameOverState));
                break;

            case GamePhase.GameOver:
                Debug.Log("Reiniciando juego...");
                gamePhase = GamePhase.Started;
                gameOverState.score = 0;
                StartCoroutine(ChangeState(startedState));
                break;
        }
    }

    IEnumerator ChangeState(GameState newState)
    {
        yield return newState.EnterState();
        isChangingState = false;
    }

    public void QuitGame()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}