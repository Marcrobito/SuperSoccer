using System.Collections;
using UnityEngine;
using TMPro;

public class GameOverState : GameState
{
    [SerializeField]
    private GameObject mainPanel;

    [SerializeField]
    private TextMeshProUGUI scoreText;

    void Start()
    {
        mainPanel.SetActive(false);
    }

    public override IEnumerator EnterState()
    {
        mainPanel.SetActive(true);
        scoreText.text = score.ToString();
        yield return null;
    }

    public void onReStartClicked(){
        mainPanel.SetActive(false);
        OnStateCompleted?.Invoke(-1);
    }
}