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
    private string walkDownState =
        "basicEnemyWalkDown";

    [SerializeField]
    private string walkUpState =
        "basicEnemyWalkUp";


    // ============================================================
    // ANIMATION HASHES
    // ============================================================

    private int walkDownHash;
    private int walkUpHash;


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
            animator =
                GetComponent<Animator>();
        }

        walkDownHash =
            Animator.StringToHash(
                walkDownState
            );

        walkUpHash =
            Animator.StringToHash(
                walkUpState
            );
    }


    // ============================================================
    // MOVEMENT DIRECTION
    // ============================================================

    public void SetMovementDirection(
        Vector2Int direction)
    {
        if (animator == null)
        {
            return;
        }


        // --------------------------------------------------------
        // Ignore zero movement.
        // --------------------------------------------------------

        if (
            direction.x == 0 &&
            direction.y == 0
        )
        {
            return;
        }


        // --------------------------------------------------------
        // Determine dominant direction.
        //
        // This matters for diagonal movement.
        //
        // Example:
        // (1, 1)  -> UP
        // (-1, 1) -> UP
        // (1,-1)  -> DOWN
        // (-1,-1) -> DOWN
        // --------------------------------------------------------

        if (
            Mathf.Abs(direction.y) >=
            Mathf.Abs(direction.x)
        )
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
            // ----------------------------------------------------
            // There are currently no left/right animations.
            //
            // For horizontal movement we keep the previous
            // animation instead of changing to an incorrect one.
            // ----------------------------------------------------

            return;
        }

        isWalking = true;
    }


    // ============================================================
    // WALK UP
    // ============================================================

    private void PlayWalkUp()
    {
        if (IsCurrentState(walkUpHash))
        {
            return;
        }

        animator.Play(
            walkUpHash,
            0,
            0f
        );
    }


    // ============================================================
    // WALK DOWN
    // ============================================================

    private void PlayWalkDown()
    {
        if (IsCurrentState(walkDownHash))
        {
            return;
        }

        animator.Play(
            walkDownHash,
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
    // STATE CHECK
    // ============================================================

    private bool IsCurrentState(
        int stateHash)
    {
        if (animator == null)
        {
            return false;
        }

        AnimatorStateInfo stateInfo =
            animator.GetCurrentAnimatorStateInfo(0);

        return
            stateInfo.shortNameHash ==
            stateHash;
    }


    // ============================================================
    // PUBLIC STATE
    // ============================================================

    public bool IsWalking()
    {
        return isWalking;
    }
}