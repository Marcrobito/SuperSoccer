using System;
using UnityEngine;

public class BallController : MonoBehaviour
{
    [SerializeField]
    private GameObject ball;

    [SerializeField]
    private GameObject[] missingTargets;

    [SerializeField]
    private GameObject[] goalTargets;

    [SerializeField]
    private GameObject goalCollider;

    private Vector3 ballStartingPosition = new Vector3(-808.04f, 0f, -476.87f);
    private Rigidbody rb;

    private bool ballShoot = false;

    public Action OnGoal;

    [SerializeField] private float Force = 10f; // Fuerza inicial del disparo
    [SerializeField] private float BounceForce = 5f; // Fuerza del rebote

    void Start()
    {
        if (ball == null)
        {
            Debug.LogError("Ball no está asignado en el Inspector.");
            return;
        }

        rb = ball.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("No se encontró Rigidbody en la pelota.");
        }
    }

    public void Shoot(int position, bool isGoal)
    {
        if (rb == null)
        {
            Debug.LogError("No se puede disparar porque Rigidbody es nulo.");
            return;
        }

        if (!IsValidTargetIndex(position))
        {
            Debug.LogError($"No hay objetivos configurados para la opción {position}. Revisa goalTargets/missingTargets.");
            return;
        }

        // Reiniciar posición y detener movimiento previo
        ball.transform.position = ballStartingPosition;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Seleccionar el objetivo correcto
        GameObject targetObject = isGoal ? goalTargets[position] : missingTargets[position];
        if (targetObject == null)
        {
            Debug.LogError($"El target {(isGoal ? "goal" : "miss")} en la posición {position} no está asignado.");
            return;
        }
        Transform target = targetObject.transform;

        // Calcular dirección del disparo
        Vector3 shootDirection = (target.position - ball.transform.position).normalized;

        // Aplicar la fuerza inicial
        rb.AddForce(shootDirection * Force + new Vector3(0, 4f, 0), ForceMode.Impulse);

        ballShoot = true;
    }

    void OnCollisionEnter(Collision collision)
    {
        // Si la pelota choca con algo, aplicar un rebote extra
        if (rb != null && ballShoot)
        {
            Vector3 bounceDirection = Vector3.Reflect(rb.linearVelocity.normalized, collision.contacts[0].normal);
            bounceDirection.y = Mathf.Abs(bounceDirection.y); // Asegurar que rebote hacia arriba si es necesario

            rb.linearVelocity = Vector3.zero; // Detener la velocidad actual para evitar acumulación de física
            rb.AddForce(bounceDirection * BounceForce, ForceMode.Impulse);

            Debug.Log("Pelota rebotó con fuerza extra: " + BounceForce);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == goalCollider)
        {
            Debug.Log("¡GOOOOL! La pelota entró en el arco.");
            //ballShoot = false; // Reseteamos el estado de disparo
            OnGoal?.Invoke();
        }
    }

    public void ResetBallPosition()
    {
        if (rb == null)
        {
            Debug.LogError("No se puede reiniciar la pelota porque Rigidbody es nulo.");
            return;
        }
        ballShoot = false;

        Debug.Log("Reiniciando la posición de la pelota...");

        // Resetear la posición de la pelota
        ball.transform.position = ballStartingPosition;

        // Detener cualquier movimiento previo
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Opcional: Resetear la rotación
        ball.transform.rotation = Quaternion.identity;

        ballShoot = false; // Resetear estado del disparo
    }

    public bool HasTargetsForOptions(int requiredOptions)
    {
        if (goalTargets == null || missingTargets == null)
        {
            Debug.LogError("Los arreglos de target no están asignados en BallController.");
            return false;
        }

        if (goalTargets.Length < requiredOptions || missingTargets.Length < requiredOptions)
        {
            Debug.LogError($"Se requieren {requiredOptions} targets configurados para cada tipo (goal/miss).");
            return false;
        }

        for (int i = 0; i < requiredOptions; i++)
        {
            if (goalTargets[i] == null || missingTargets[i] == null)
            {
                Debug.LogError($"El target en la posición {i} no está asignado correctamente.");
                return false;
            }
        }

        return true;
    }

    private bool IsValidTargetIndex(int index)
    {
        return goalTargets != null && missingTargets != null &&
               index >= 0 && index < goalTargets.Length && index < missingTargets.Length;
    }
}
