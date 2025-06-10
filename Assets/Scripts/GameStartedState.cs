using System.Collections;
using UnityEngine;
using TMPro;

public class GameStartedState : GameState
{
    [SerializeField]
    private GameObject mainPanel;

    public override IEnumerator EnterState()
    {
        mainPanel.SetActive(true);
        yield return null;
    }

    public void onStartClicked(){
        mainPanel.SetActive(false);
        OnStateCompleted?.Invoke(-1);
    }

}