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
    // STATE
    // ============================================================

    private bool isWalking;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator == null)
        {
            return;
        }

        if (animator.runtimeAnimatorController == null)
        {
            return;
        }
    }


    // ============================================================
    // MOVEMENT DIRECTION
    // ============================================================

    public void SetMovementDirection(Vector2Int direction)
    {
        if (animator == null)
        {
            return;
        }

        if (direction.x == 0 && direction.y == 0)
        {
            return;
        }

        if (Mathf.Abs(direction.y) >= Mathf.Abs(direction.x))
        {
            if (direction.y > 0)
            {
                PlayWalkUp();
            }
            else if (direction.y < 0)
            {
                PlayWalkDown();
            }
        }
        else
        {
            return;
        }

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

        if (IsCurrentState(walkUpState))
        {
            return;
        }

        animator.Play(
            walkUpState,
            0,
            0f
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

        if (IsCurrentState(walkDownState))
        {
            return;
        }

        animator.Play(
            walkDownState,
            0,
            0f
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

        isWalking = false;

        if (IsCurrentState(idleState))
        {
            return;
        }

        animator.Play(
            idleState,
            0,
            0f
        );
    }


    // ============================================================
    // STOP WALKING
    // ============================================================

    public void StopWalking()
    {
        isWalking = false;
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

        return isCurrent;
    }


    // ============================================================
    // PUBLIC STATE
    // ============================================================

    public bool IsWalking()
    {
        return isWalking;
    }
}