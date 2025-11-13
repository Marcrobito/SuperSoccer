using System;
using System.Collections;
using UnityEngine;

public abstract class GameState : MonoBehaviour
{
    public abstract IEnumerator EnterState();
    public Action<int> OnStateCompleted;
    public int score = 0;
}