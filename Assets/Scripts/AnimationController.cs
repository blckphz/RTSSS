using System.Collections;
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
    // WALK ANIMATION STATES
    // ============================================================

    [Header("Walk Animation States")]

    [SerializeField]
    private string walkDownState = "basicEnemyWalkDown";

    [SerializeField]
    private string walkUpState = "basicEnemyWalkUp";

    [SerializeField]
    private string walkLeftState = "basicEnemyWalkLeft";

    [SerializeField]
    private string walkRightState = "basicEnemyWalkRight";


    // ============================================================
    // IDLE
    // ============================================================

    [Header("Idle Animation State")]

    [SerializeField]
    private string idleState = "Idle";


    // ============================================================
    // ATTACK LAYER
    // ============================================================

    [Header("Enemy Attack Animation Layer")]

    [SerializeField]
    private string attackAnimatorLayerName = "attackLayer";


    // ============================================================
    // ATTACK STATES
    // ============================================================

    [Header("Enemy Attack Animation States")]

    [SerializeField]
    private string enemyAttackUpState = "enemyattackup";

    [SerializeField]
    private string enemyAttackDownState = "enemyattackdown";

    [SerializeField]
    private string enemyAttackLeftState = "enemyattackleft";

    [SerializeField]
    private string enemyAttackRightState = "enemyattackright";


    // ============================================================
    // STATE
    // ============================================================

    private bool isWalking;

    private int attackAnimatorLayerIndex = -1;


    // ============================================================
    // ATTACK EVENT STATE
    // ============================================================

    private bool attackHitEventReceived;

    private bool attackFinishedEventReceived;


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
            Debug.LogError(
                "[AnimationController] Animator reference is missing.",
                this
            );

            return;
        }

        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogError(
                "[AnimationController] Animator has no Runtime Animator Controller.",
                this
            );

            return;
        }

        attackAnimatorLayerIndex =
            animator.GetLayerIndex(
                attackAnimatorLayerName
            );

        if (attackAnimatorLayerIndex < 0)
        {
            Debug.LogError(
                "[AnimationController] Could not find Animator layer: "
                + attackAnimatorLayerName,
                this
            );

            return;
        }

        animator.SetLayerWeight(
            attackAnimatorLayerIndex,
            1f
        );

        Debug.Log(
            "[AnimationController] Attack layer found"
            + " | Name: "
            + attackAnimatorLayerName
            + " | Index: "
            + attackAnimatorLayerIndex,
            this
        );
    }


    // ============================================================
    // CONVERT GRID DIRECTION TO VISUAL/WORLD DIRECTION
    // ============================================================

    private Vector2Int ConvertDirectionToWorldDirection(
        Vector2Int direction
    )
    {
        if (direction == Vector2Int.zero)
        {
            return Vector2Int.zero;
        }

        Vector3 logicalDirection =
            new Vector3(
                direction.x,
                direction.y,
                0f
            );

        BoardViewController board =
            BoardViewController.Instance;

        if (board != null)
        {
            Transform boardTransform =
                board.GetBoardTransform();

            if (boardTransform != null)
            {
                logicalDirection =
                    boardTransform.TransformDirection(
                        logicalDirection
                    );
            }
        }

        // --------------------------------------------------------
        // Determine which visual cardinal direction is strongest.
        // --------------------------------------------------------

        if (
            Mathf.Abs(logicalDirection.y) >=
            Mathf.Abs(logicalDirection.x)
        )
        {
            if (logicalDirection.y >= 0f)
            {
                return Vector2Int.up;
            }

            return Vector2Int.down;
        }

        if (logicalDirection.x >= 0f)
        {
            return Vector2Int.right;
        }

        return Vector2Int.left;
    }


    // ============================================================
    // MOVEMENT DIRECTION
    // ============================================================

    public void SetMovementDirection(
        Vector2Int direction
    )
    {
        if (animator == null)
        {
            return;
        }

        if (direction == Vector2Int.zero)
        {
            return;
        }

        // --------------------------------------------------------
        // IMPORTANT:
        //
        // UnitMoveBrain gives us a LOGICAL GRID direction.
        //
        // The board may have been rotated visually.
        //
        // Convert the logical direction into the direction the
        // player actually sees on screen.
        // --------------------------------------------------------

        Vector2Int visualDirection =
            ConvertDirectionToWorldDirection(
                direction
            );


        // --------------------------------------------------------
        // PLAY CORRECT WALK ANIMATION
        // --------------------------------------------------------

        if (visualDirection == Vector2Int.up)
        {
            PlayWalkUp();
        }
        else if (visualDirection == Vector2Int.down)
        {
            PlayWalkDown();
        }
        else if (visualDirection == Vector2Int.left)
        {
            PlayWalkLeft();
        }
        else if (visualDirection == Vector2Int.right)
        {
            PlayWalkRight();
        }

        isWalking = true;
    }


    // ============================================================
    // WALK
    // ============================================================

    private void PlayWalkUp()
    {
        PlayWalkState(
            walkUpState
        );
    }


    private void PlayWalkDown()
    {
        PlayWalkState(
            walkDownState
        );
    }


    private void PlayWalkLeft()
    {
        PlayWalkState(
            walkLeftState
        );
    }


    private void PlayWalkRight()
    {
        PlayWalkState(
            walkRightState
        );
    }


    private void PlayWalkState(
        string stateName
    )
    {
        if (animator == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(stateName))
        {
            Debug.LogWarning(
                "[AnimationController] Walk state is empty.",
                this
            );

            return;
        }

        if (IsCurrentState(
                stateName,
                0
            ))
        {
            return;
        }

        animator.Play(
            stateName,
            0,
            0f
        );
    }


    // ============================================================
    // ENEMY ATTACK
    // ============================================================

    public void PlayEnemyAttack(
        Vector2Int direction
    )
    {
        if (animator == null)
        {
            Debug.LogWarning(
                "[AnimationController] Cannot play enemy attack. Animator is null.",
                this
            );

            return;
        }

        if (direction == Vector2Int.zero)
        {
            Debug.LogWarning(
                "[AnimationController] Cannot play enemy attack. Direction is zero.",
                this
            );

            return;
        }

        if (attackAnimatorLayerIndex < 0)
        {
            attackAnimatorLayerIndex =
                animator.GetLayerIndex(
                    attackAnimatorLayerName
                );
        }

        if (attackAnimatorLayerIndex < 0)
        {
            Debug.LogError(
                "[AnimationController] Attack layer not found: "
                + attackAnimatorLayerName,
                this
            );

            return;
        }

        animator.SetLayerWeight(
            attackAnimatorLayerIndex,
            1f
        );

        isWalking = false;


        // --------------------------------------------------------
        // CONVERT LOGICAL ATTACK DIRECTION TO VISUAL DIRECTION
        // --------------------------------------------------------

        Vector2Int visualDirection =
            ConvertDirectionToWorldDirection(
                direction
            );


        string stateToPlay = null;


        if (visualDirection == Vector2Int.up)
        {
            stateToPlay =
                enemyAttackUpState;
        }
        else if (visualDirection == Vector2Int.down)
        {
            stateToPlay =
                enemyAttackDownState;
        }
        else if (visualDirection == Vector2Int.left)
        {
            stateToPlay =
                enemyAttackLeftState;
        }
        else if (visualDirection == Vector2Int.right)
        {
            stateToPlay =
                enemyAttackRightState;
        }


        if (string.IsNullOrEmpty(stateToPlay))
        {
            Debug.LogError(
                "[AnimationController] Enemy attack state is empty.",
                this
            );

            return;
        }


        // --------------------------------------------------------
        // RESET ANIMATION EVENT STATE
        // --------------------------------------------------------

        attackHitEventReceived = false;

        attackFinishedEventReceived = false;


        Debug.Log(
            "[AnimationController] ENEMY ATTACK"
            + " | Logical Direction: "
            + direction
            + " | Visual Direction: "
            + visualDirection
            + " | State: "
            + stateToPlay
            + " | Layer: "
            + attackAnimatorLayerName
            + " | Layer Index: "
            + attackAnimatorLayerIndex,
            this
        );


        animator.Play(
            stateToPlay,
            attackAnimatorLayerIndex,
            0f
        );

        animator.Update(0f);


        AnimatorStateInfo stateInfo =
            animator.GetCurrentAnimatorStateInfo(
                attackAnimatorLayerIndex
            );


        Debug.Log(
            "[AnimationController] AFTER PLAY"
            + " | Requested: "
            + stateToPlay
            + " | IsName: "
            + stateInfo.IsName(stateToPlay)
            + " | Normalized Time: "
            + stateInfo.normalizedTime,
            this
        );
    }


    // ============================================================
    // ENEMY ATTACK USING GRID POSITIONS
    // ============================================================

    public void PlayEnemyAttack(
        Vector2Int attackerTile,
        Vector2Int targetTile
    )
    {
        Vector2Int direction =
            targetTile - attackerTile;


        Debug.Log(
            "[AnimationController] Enemy attack grid direction"
            + " | Attacker: "
            + attackerTile
            + " | Target: "
            + targetTile
            + " | Raw Direction: "
            + direction,
            this
        );


        direction.x =
            Mathf.Clamp(
                direction.x,
                -1,
                1
            );

        direction.y =
            Mathf.Clamp(
                direction.y,
                -1,
                1
            );


        PlayEnemyAttack(
            direction
        );
    }


    // ============================================================
    // ATTACK HIT
    //
    // UNITY ANIMATION EVENT
    //
    // DAMAGE HAPPENS HERE.
    // ============================================================

    public void AttackHit()
    {
        Debug.Log(
            "[AnimationController] ATTACK HIT ANIMATION EVENT FIRED.",
            this
        );


        // Prevent duplicate hit events.

        if (attackHitEventReceived)
        {
            Debug.LogWarning(
                "[AnimationController] AttackHit was already received for this attack.",
                this
            );

            return;
        }


        attackHitEventReceived = true;


        AttackUnit attackUnit =
            GetComponentInParent<AttackUnit>();


        if (attackUnit != null)
        {
            attackUnit.OnAttackAnimationEvent();
        }
        else
        {
            Debug.LogWarning(
                "[AnimationController] AttackHit fired but no AttackUnit was found.",
                this
            );
        }
    }


    // ============================================================
    // ATTACK FINISHED
    //
    // UNITY ANIMATION EVENT
    //
    // PUT THIS EVENT AT THE VERY END OF THE ATTACK ANIMATION.
    // ============================================================

    public void AttackFinished()
    {
        Debug.Log(
            "[AnimationController] ATTACK FINISHED ANIMATION EVENT FIRED.",
            this
        );


        attackFinishedEventReceived = true;


        AttackUnit attackUnit =
            GetComponentInParent<AttackUnit>();


        if (attackUnit != null)
        {
            attackUnit.OnAttackAnimationFinished();
        }
        else
        {
            Debug.LogWarning(
                "[AnimationController] AttackFinished fired but no AttackUnit was found.",
                this
            );
        }
    }


    // ============================================================
    // WAIT FOR ATTACK HIT
    // ============================================================

    public IEnumerator WaitForAttackHit()
    {
        while (!attackHitEventReceived)
        {
            yield return null;
        }
    }


    // ============================================================
    // WAIT FOR ATTACK FINISHED
    // ============================================================

    public IEnumerator WaitForAttackFinished()
    {
        while (!attackFinishedEventReceived)
        {
            yield return null;
        }
    }


    // ============================================================
    // BACKWARD COMPATIBILITY
    // ============================================================

    public void PlayEnemyAttackDown(
        Vector2Int direction
    )
    {
        PlayEnemyAttack(
            direction
        );
    }


    public void PlayEnemyAttackDown(
        Vector2Int attackerTile,
        Vector2Int targetTile
    )
    {
        PlayEnemyAttack(
            attackerTile,
            targetTile
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


        if (IsCurrentState(
                idleState,
                0
            ))
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
    // CHECK CURRENT STATE
    // ============================================================

    private bool IsCurrentState(
        string stateName,
        int layerIndex
    )
    {
        if (animator == null)
        {
            return false;
        }

        if (layerIndex < 0)
        {
            return false;
        }

        if (string.IsNullOrEmpty(stateName))
        {
            return false;
        }


        AnimatorStateInfo stateInfo =
            animator.GetCurrentAnimatorStateInfo(
                layerIndex
            );


        return stateInfo.IsName(
            stateName
        );
    }


    // ============================================================
    // GET ATTACK LAYER INDEX
    // ============================================================

    public int GetAttackAnimatorLayerIndex()
    {
        if (animator == null)
        {
            return -1;
        }


        if (attackAnimatorLayerIndex < 0)
        {
            attackAnimatorLayerIndex =
                animator.GetLayerIndex(
                    attackAnimatorLayerName
                );
        }


        return attackAnimatorLayerIndex;
    }


    // ============================================================
    // GET ANIMATOR
    // ============================================================

    public Animator GetAnimator()
    {
        return animator;
    }


    // ============================================================
    // GET ATTACK LAYER NAME
    // ============================================================

    public string GetAttackAnimatorLayerName()
    {
        return attackAnimatorLayerName;
    }


    // ============================================================
    // PUBLIC STATE
    // ============================================================

    public bool IsWalking()
    {
        return isWalking;
    }


    // ============================================================
    // ATTACK EVENT STATE
    // ============================================================

    public bool HasAttackHit()
    {
        return attackHitEventReceived;
    }


    public bool HasAttackFinished()
    {
        return attackFinishedEventReceived;
    }
}