using System;
using UnityEngine;

public class GoalKeeperController : MonoBehaviour
{
    public Animator animator; // Asigna el Animator en el Inspector
    private bool isDiving = false;

    public Action OnDiveCompleted;

    void Start()
    {
        animator.speed = 1.1f; 
    }

    void Update()
    {

        // Detectar si la animación terminó*/
        CheckAnimationEnd();
    }

    public void StartDive(string triggerName)
    {
        animator.SetTrigger(triggerName);
        Debug.Log("Trigger: " + triggerName);
        isDiving = true;
    }

    void CheckAnimationEnd()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        // Si la animación actual ha terminado (normalizedTime >= 1)
        if (isDiving && stateInfo.normalizedTime >= 1f && !animator.IsInTransition(0))
        {
            Debug.Log("¡Animación de Dive terminada!");
            isDiving = false; // Permite volver a hacer otro Dive

            //ResetToIdle();
            OnDiveCompleted?.Invoke();
        }
    }

    public void ResetToIdle()
    {
        Debug.Log("Reiniciando a Idle sin transición...");
        animator.Play("AA_Soccer_Goal_Waiting01", 0, 0f); // Reproduce instantáneamente la animación inicial
        isDiving = false;
    }
}