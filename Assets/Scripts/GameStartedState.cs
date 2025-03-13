using System.Collections;
using UnityEngine;
using TMPro;

public class GameStartedState : GameState
{
    [SerializeField]
    private GameObject mainPanel;

    [SerializeField] private ImageSwitcher imageSwitcher;
    [SerializeField] private TMP_Dropdown imageSelector;

    void Start()
    {
        imageSelector.gameObject.SetActive(true);
        imageSelector.onValueChanged.AddListener(OnDropdownValueChanged);
    }


    public override IEnumerator EnterState()
    {
        mainPanel.SetActive(true);
        yield return null;
    }

    public void onStartClicked(){
        mainPanel.SetActive(false);
        imageSelector.gameObject.SetActive(false);
        OnStateCompleted?.Invoke(-1);
    }

    private void OnDropdownValueChanged(int index)
    {
        Debug.Log($"index {index}");
        switch (index)
        {
            case 0:
                imageSwitcher.SetImageA();
                break;
            case 1:
                imageSwitcher.SetImageB();
                break;
            case 2:
                imageSwitcher.ClearImage();
                break;
        }
    }

}