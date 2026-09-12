using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static event System.Action<Vector3, float> NoiseMade;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;

    [SerializeField] private float noiseRadius = 10f;

    [SerializeField] private float noiseInterval = 0.35f;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 10f;

    private float noiseTimer;

    private void Update()
    {
        MovePlayer();
    }

    private void MovePlayer()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 movement = new Vector3(
            horizontal,
            0f,
            vertical
        ).normalized;

        if (movement != Vector3.zero)
        {
            noiseTimer -= Time.deltaTime;

            if (noiseTimer <= 0f)
            {
                NoiseMade?.Invoke(
                    transform.position,
                    noiseRadius
                );

                noiseTimer = noiseInterval;
            }
        }

        // Gerakkan Player
        transform.position +=
            movement *
            moveSpeed *
            Time.deltaTime;

        // Putar Player mengikuti arah gerakan
        if (movement != Vector3.zero)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(movement);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(
            transform.position,
            noiseRadius
        );
    }
}