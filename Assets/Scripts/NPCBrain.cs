using UnityEngine;
using UnityEngine.AI;
using TMPro;

public class NPCBrain : MonoBehaviour
{
    public enum NPCState
    {
        Patrol,
        Chase,
        Search
    }

    [Header("References")]
    [SerializeField]
    private NPCSensor sensor;

    [SerializeField]
    private NavMeshAgent agent;

    [Header("Patrol Settings")]
    [SerializeField]
    private Transform[] patrolPoints;

    [SerializeField]
    private float waypointTolerance = 0.7f;

    // Challenge 1
    [SerializeField]
    [Min(0f)]
    private float waitDuration = 2f;

    [SerializeField]
    private float patrolSpeed = 2f;

    [Header("Chase Settings")]
    [SerializeField]
    private float chaseSpeed = 4f;

    [Header("Search Settings")]
    [SerializeField]
    private float searchDuration = 4f;

    [SerializeField]
    private float searchTolerance = 0.8f;

    [SerializeField]
    private float hearingRadius = 10f;

    [SerializeField]
    private float searchRotationSpeed = 120f;

    [SerializeField]
    [Min(0.1f)]
    private float searchTurnDuration = 1f;

    [Header("Debug")]
    [SerializeField]
    private NPCState currentState;

    [Header("State Indicator")]
    [SerializeField]
    private TMP_Text stateIndicator;

    private NPCState previousState;

    private int patrolIndex = 0;

    // =============================
    // MEMORY
    // =============================

    private Vector3 lastKnownPosition;

    private bool hasLastKnownPosition;

    private float searchTimer;

    private float searchTurnTimer;

    private float searchTurnDirection = -1f;

    private float waitTimer; //challenge 1

    private bool isWaitingAtPatrolPoint;

    private Vector3 heardNoisePosition;

    private bool hasHeardNoise;

    private void OnEnable()
    {
        PlayerController.NoiseMade += HearNoise;
    }

    private void OnDisable()
    {
        PlayerController.NoiseMade -= HearNoise;
    }

    private void Start()
    {
        currentState = NPCState.Patrol;
        previousState = currentState;

        UpdateStateIndicator();
        GoToCurrentPatrolPoint();
    }

    private void Update()
    {
        UpdateMemory();

        MakeDecision();

        ExecuteCurrentState();
    }

    // ======================================
    // MEMORY
    // ======================================

    private void UpdateMemory()
    {
        if (sensor.CanSeePlayer)
        {
            lastKnownPosition =
                sensor.Player.position;

            hasLastKnownPosition = true;
        }
    }

    // ======================================
    // DECISION
    // ======================================

    private void MakeDecision()
    {
        // PRIORITAS 1
        // PLAYER TERLIHAT
        if (sensor.CanSeePlayer)
        {
            ChangeState(
                NPCState.Chase
            );

            return;
        }

        if (hasHeardNoise &&
            currentState != NPCState.Chase)
        {
            lastKnownPosition = heardNoisePosition;
            hasLastKnownPosition = true;
            hasHeardNoise = false;

            searchTimer = searchDuration;
            searchTurnTimer = searchTurnDuration;
            searchTurnDirection = -1f;

            ChangeState(NPCState.Search);
            return;
        }

        // PRIORITAS 2
        // PLAYER BARU HILANG
        if (currentState ==
                NPCState.Chase &&
            hasLastKnownPosition)
        {
            searchTimer =
                searchDuration;

            searchTurnTimer =
                searchTurnDuration;

            searchTurnDirection = -1f;

            ChangeState(
                NPCState.Search
            );

            return;
        }

        // PRIORITAS 3
        // SEARCH SELESAI
        if (currentState ==
                NPCState.Search &&
            searchTimer <= 0f)
        {
            hasLastKnownPosition =
                false;

            ChangeState(
                NPCState.Patrol
            );
        }
    }

    // ======================================
    // ACTION
    // ======================================

    private void ExecuteCurrentState()
    {
        switch (currentState)
        {
            case NPCState.Patrol:

                Patrol();
                break;

            case NPCState.Chase:

                Chase();
                break;

            case NPCState.Search:

                Search();
                break;
        }
    }

    // ======================================
    // PATROL
    // ======================================

    private void Patrol()
    {
        agent.speed = patrolSpeed;

        if (patrolPoints == null ||
            patrolPoints.Length == 0)
        {
            return;
        }

        if (isWaitingAtPatrolPoint)
        {
            waitTimer -= Time.deltaTime;

            if (waitTimer > 0f)
            {
                return;
            }

            isWaitingAtPatrolPoint = false;

            patrolIndex++;

            if (patrolIndex >=
                patrolPoints.Length)
            {
                patrolIndex = 0;
            }

            GoToCurrentPatrolPoint();
            return;
        }

        if (!agent.pathPending &&
            agent.remainingDistance <=
            waypointTolerance)
        {
            isWaitingAtPatrolPoint = true;
            waitTimer = waitDuration;
            agent.ResetPath();
        }
    }

    private void GoToCurrentPatrolPoint()
    {
        if (patrolPoints == null ||
            patrolPoints.Length == 0)
        {
            return;
        }

        agent.SetDestination(
            patrolPoints[
                patrolIndex
            ].position
        );
    }

    // ======================================
    // CHASE
    // ======================================

    private void Chase()
    {
        agent.speed =
            chaseSpeed;

        if (sensor.Player == null)
            return;

        agent.SetDestination(
            sensor.Player.position
        );
    }

    // ======================================
    // SEARCH
    // ======================================

    private void Search()
    {
        agent.speed =
            patrolSpeed;

        if (agent.pathPending ||
            agent.remainingDistance > searchTolerance)
        {
            agent.SetDestination(
                lastKnownPosition
            );

            return;
        }

        agent.ResetPath();

        searchTimer -=
            Time.deltaTime;

        transform.Rotate(
            Vector3.up,
            searchTurnDirection *
            searchRotationSpeed *
            Time.deltaTime
        );

        searchTurnTimer -=
            Time.deltaTime;

        if (searchTurnTimer <= 0f)
        {
            searchTurnDirection *= -1f;
            searchTurnTimer = searchTurnDuration;
        }
    }

    // ======================================
    // STATE TRANSITION
    // ======================================

    private void ChangeState(
        NPCState newState
    )
    {
        if (currentState ==
            newState)
        {
            return;
        }

        previousState =
            currentState;

        currentState =
            newState;

        Debug.Log(
            gameObject.name +
            ": " +
            previousState +
            " -> " +
            currentState
        );

        if (currentState != NPCState.Patrol)
        {
            isWaitingAtPatrolPoint = false;
            waitTimer = 0f;
        }

        if (currentState ==
            NPCState.Patrol)
        {
            isWaitingAtPatrolPoint = false;
            waitTimer = 0f;
            UpdateStateIndicator();
            GoToCurrentPatrolPoint();
        }
        else
        {
            UpdateStateIndicator();
        }
    }

    private void HearNoise(
        Vector3 noisePosition,
        float noiseRadius)
    {
        if (currentState == NPCState.Chase)
        {
            return;
        }

        float effectiveRadius =
            Mathf.Min(noiseRadius, hearingRadius);

        if (Vector3.Distance(
                transform.position,
                noisePosition) <= effectiveRadius)
        {
            heardNoisePosition = noisePosition;
            hasHeardNoise = true;
        }
    }

    private void UpdateStateIndicator()
    {
        if (stateIndicator == null)
        {
            return;
        }

        switch (currentState)
        {
            case NPCState.Chase:
                stateIndicator.text = "!";
                stateIndicator.color = Color.red;
                stateIndicator.gameObject.SetActive(true);
                break;

            case NPCState.Search:
                stateIndicator.text = "?";
                stateIndicator.color = Color.yellow;
                stateIndicator.gameObject.SetActive(true);
                break;

            default:
                stateIndicator.gameObject.SetActive(false);
                break;
        }
    }

    // ======================================
    // DEBUG GIZMOS
    // ======================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(
            transform.position,
            hearingRadius
        );

        if (hasHeardNoise)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(
                heardNoisePosition,
                0.25f
            );

            Gizmos.DrawLine(
                transform.position,
                heardNoisePosition
            );
        }

        switch (currentState)
        {
            case NPCState.Patrol:

                Gizmos.color =
                    Color.green;
                break;

            case NPCState.Chase:

                Gizmos.color =
                    Color.red;
                break;

            case NPCState.Search:

                Gizmos.color =
                    Color.blue;
                break;
        }

        Gizmos.DrawWireSphere(
            transform.position,
            0.8f
        );

        if (hasLastKnownPosition)
        {
            Gizmos.color =
                Color.magenta;

            Gizmos.DrawSphere(
                lastKnownPosition,
                0.3f
            );

            Gizmos.DrawLine(
                transform.position,
                lastKnownPosition
            );
        }
    }
}