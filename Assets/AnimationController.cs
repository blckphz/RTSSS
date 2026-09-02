using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimationController : MonoBehaviour
{
    // ============================================================
    // REFERENCES
    // ============================================================

    [Header("References")]
    [SerializeField]
    private Animator animator;


    // ============================================================
    // ANIMATION STATES
    // ============================================================

    [Header("Walk Animation States")]

    [SerializeField]
    private string walkDownState = "basicEnemyWalkDown";

    [SerializeField]
    private string walkUpState = "basicEnemyWalkUp";


    [Header("Idle Animation State")]

    [SerializeField]
    private string idleState = "Idle";


    // ============================================================
    // DEBUG
    // ============================================================

    [Header("Debug")]
    [SerializeField]
    private bool enableDebugLogs = true;


    // ============================================================
    // STATE
    // ============================================================

    private bool isWalking;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        // --------------------------------------------------------
        // Get Animator automatically if one was not assigned.
        // --------------------------------------------------------

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }


        // --------------------------------------------------------
        // Check Animator.
        // --------------------------------------------------------

        if (animator == null)
        {
            Debug.LogError(
                $"[{name}] AnimationController ERROR: " +
                "Animator component was not found!"
            );

            return;
        }


        // --------------------------------------------------------
        // Check Animator Controller.
        // --------------------------------------------------------

        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogError(
                $"[{name}] AnimationController ERROR: " +
                "No Animator Controller is assigned!"
            );

            return;
        }


        // --------------------------------------------------------
        // Debug information.
        // --------------------------------------------------------

        DebugLog(
            "AnimationController initialized.\n" +
            $"Animator: {animator.name}\n" +
            $"Controller: {animator.runtimeAnimatorController.name}\n" +
            $"Walk Up State: {walkUpState}\n" +
            $"Walk Down State: {walkDownState}\n" +
            $"Idle State: {idleState}"
        );
    }


    // ============================================================
    // MOVEMENT DIRECTION
    // ============================================================

    public void SetMovementDirection(Vector2Int direction)
    {
        // --------------------------------------------------------
        // Safety check.
        // --------------------------------------------------------

        if (animator == null)
        {
            Debug.LogError(
                $"[{name}] SetMovementDirection ERROR: " +
                "Animator is null!"
            );

            return;
        }


        DebugLog(
            $"SetMovementDirection called with: {direction}"
        );


        // --------------------------------------------------------
        // No movement.
        // --------------------------------------------------------

        if (direction.x == 0 && direction.y == 0)
        {
            DebugLog("Direction is zero. Ignoring.");
            return;
        }


        // --------------------------------------------------------
        // Determine dominant direction.
        // --------------------------------------------------------
        //
        // (0, 1)  = UP
        // (0,-1)  = DOWN
        // (1, 1)  = UP
        // (-1,1)  = UP
        // (1,-1)  = DOWN
        // (-1,-1) = DOWN
        //
        // --------------------------------------------------------

        if (Mathf.Abs(direction.y) >= Mathf.Abs(direction.x))
        {
            if (direction.y > 0)
            {
                DebugLog("Movement direction = UP");

                PlayWalkUp();
            }
            else if (direction.y < 0)
            {
                DebugLog("Movement direction = DOWN");

                PlayWalkDown();
            }
        }
        else
        {
            // ----------------------------------------------------
            // No left/right animation yet.
            // Keep current animation.
            // ----------------------------------------------------

            DebugLog(
                "Movement direction is horizontal. " +
                "No left/right animation is configured."
            );

            return;
        }


        // --------------------------------------------------------
        // Character is walking.
        // --------------------------------------------------------

        isWalking = true;
    }


    // ============================================================
    // WALK UP
    // ============================================================

    private void PlayWalkUp()
    {
        if (animator == null)
        {
            return;
        }


        DebugLog(
            $"Trying to play UP animation: {walkUpState}"
        );


        // --------------------------------------------------------
        // Check if already playing.
        // --------------------------------------------------------

        if (IsCurrentState(walkUpState))
        {
            DebugLog(
                "UP animation is already playing."
            );

            return;
        }


        // --------------------------------------------------------
        // Play animation.
        // --------------------------------------------------------

        animator.Play(
            walkUpState,
            0,
            0f
        );


        DebugLog(
            $"UP animation played: {walkUpState}"
        );
    }


    // ============================================================
    // WALK DOWN
    // ============================================================

    private void PlayWalkDown()
    {
        if (animator == null)
        {
            return;
        }


        DebugLog(
            $"Trying to play DOWN animation: {walkDownState}"
        );


        // --------------------------------------------------------
        // Check if already playing.
        // --------------------------------------------------------

        if (IsCurrentState(walkDownState))
        {
            DebugLog(
                "DOWN animation is already playing."
            );

            return;
        }


        // --------------------------------------------------------
        // Play animation.
        // --------------------------------------------------------

        animator.Play(
            walkDownState,
            0,
            0f
        );


        DebugLog(
            $"DOWN animation played: {walkDownState}"
        );
    }


    // ============================================================
    // IDLE
    // ============================================================

    public void PlayIdle()
    {
        if (animator == null)
        {
            return;
        }


        DebugLog(
            $"Trying to play IDLE animation: {idleState}"
        );


        // --------------------------------------------------------
        // Character is no longer walking.
        // --------------------------------------------------------

        isWalking = false;


        // --------------------------------------------------------
        // Check if already playing.
        // --------------------------------------------------------

        if (IsCurrentState(idleState))
        {
            DebugLog(
                "IDLE animation is already playing."
            );

            return;
        }


        // --------------------------------------------------------
        // Play idle animation.
        // --------------------------------------------------------

        animator.Play(
            idleState,
            0,
            0f
        );


        DebugLog(
            $"IDLE animation played: {idleState}"
        );
    }


    // ============================================================
    // STOP WALKING
    // ============================================================

    public void StopWalking()
    {
        isWalking = false;

        DebugLog("StopWalking called.");
    }


    // ============================================================
    // CHECK CURRENT ANIMATION STATE
    // ============================================================

    private bool IsCurrentState(string stateName)
    {
        if (animator == null)
        {
            return false;
        }


        AnimatorStateInfo stateInfo =
            animator.GetCurrentAnimatorStateInfo(0);


        bool isCurrent =
            stateInfo.IsName(stateName);


        DebugLog(
            $"Current Animator State:\n" +
            $"Checking: {stateName}\n" +
            $"Is Current: {isCurrent}\n" +
            $"Normalized Time: {stateInfo.normalizedTime}"
        );


        return isCurrent;
    }


    // ============================================================
    // PUBLIC STATE
    // ============================================================

    public bool IsWalking()
    {
        return isWalking;
    }


    // ============================================================
    // DEBUG LOG
    // ============================================================

    private void DebugLog(string message)
    {
        if (!enableDebugLogs)
        {
            return;
        }


        Debug.Log(
            $"[{name}] AnimationController: {message}"
        );
    }
}