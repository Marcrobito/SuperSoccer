using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(Intro());
    }

    // Update is called once per frame
    IEnumerator Intro()
    {
        yield return new WaitForSeconds(3f); // Espera 1 segundo (1000 ms)
        SceneManager.LoadScene("Game");
    }
}