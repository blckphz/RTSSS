using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;
using static UnityEngine.RuleTile.TilingRuleOutput;

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
    // IDLE ANIMATION STATE
    // ============================================================

    [Header("Idle Animation State")]

    [SerializeField]
    private string idleState = "Idle";


    // ============================================================
    // ENEMY ATTACK ANIMATION LAYER
    // ============================================================

    [Header("Enemy Attack Animation Layer")]

    [SerializeField]
    private string attackAnimatorLayerName = "attackLayer";


    // ============================================================
    // ENEMY ATTACK ANIMATION STATES
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
        // Determine dominant axis.
        //
        // This means:
        //
        // ( 1, 0) = RIGHT
        // (-1, 0) = LEFT
        // ( 0, 1) = UP
        // ( 0,-1) = DOWN
        //
        // Diagonal directions use whichever axis is larger.
        // --------------------------------------------------------

        if (Mathf.Abs(direction.y) >= Mathf.Abs(direction.x))
        {
            if (direction.y > 0)
            {
                PlayWalkUp();
            }
            else
            {
                PlayWalkDown();
            }
        }
        else
        {
            if (direction.x > 0)
            {
                PlayWalkRight();
            }
            else
            {
                PlayWalkLeft();
            }
        }

        isWalking = true;
    }


    // ============================================================
    // WALK UP
    // ============================================================

    private void PlayWalkUp()
    {
        PlayWalkState(
            walkUpState
        );
    }


    // ============================================================
    // WALK DOWN
    // ============================================================

    private void PlayWalkDown()
    {
        PlayWalkState(
            walkDownState
        );
    }


    // ============================================================
    // WALK LEFT
    // ============================================================

    private void PlayWalkLeft()
    {
        PlayWalkState(
            walkLeftState
        );
    }


    // ============================================================
    // WALK RIGHT
    // ============================================================

    private void PlayWalkRight()
    {
        PlayWalkState(
            walkRightState
        );
    }


    // ============================================================
    // PLAY WALK STATE
    // ============================================================

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

        // --------------------------------------------------------
        // Attack should be visible.
        // --------------------------------------------------------

        animator.SetLayerWeight(
            attackAnimatorLayerIndex,
            1f
        );

        // --------------------------------------------------------
        // Stop movement state.
        // --------------------------------------------------------

        isWalking = false;

        // --------------------------------------------------------
        // Normalize direction.
        // --------------------------------------------------------

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

        // --------------------------------------------------------
        // Determine attack animation.
        // --------------------------------------------------------

        string stateToPlay = null;

        if (
            Mathf.Abs(direction.y) >=
            Mathf.Abs(direction.x)
        )
        {
            if (direction.y > 0)
            {
                stateToPlay =
                    enemyAttackUpState;
            }
            else
            {
                stateToPlay =
                    enemyAttackDownState;
            }
        }
        else
        {
            if (direction.x > 0)
            {
                stateToPlay =
                    enemyAttackRightState;
            }
            else
            {
                stateToPlay =
                    enemyAttackLeftState;
            }
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
        // Reset animation events for this attack.
        // --------------------------------------------------------

        attackHitEventReceived = false;
        attackFinishedEventReceived = false;

        Debug.Log(
            "[AnimationController] ENEMY ATTACK"
            + " | Direction: "
            + direction
            + " | State: "
            + stateToPlay
            + " | Layer: "
            + attackAnimatorLayerName
            + " | Layer Index: "
            + attackAnimatorLayerIndex,
            this
        );

        // --------------------------------------------------------
        // Play attack on attack layer.
        // --------------------------------------------------------

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
            + stateInfo.IsName(
                stateToPlay
            )
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
    // ATTACK HIT ANIMATION EVENT
    // ============================================================

    public void AttackHit()
    {
        Debug.Log(
            "[AnimationController] ATTACK HIT ANIMATION EVENT FIRED.",
            this
        );

        attackHitEventReceived = true;
    }


    // ============================================================
    // ATTACK FINISHED ANIMATION EVENT
    // ============================================================

    public void AttackFinished()
    {
        Debug.Log(
            "[AnimationController] ATTACK FINISHED ANIMATION EVENT FIRED.",
            this
        );

        attackFinishedEventReceived = true;
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
    // CHECK CURRENT ANIMATION STATE
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
    // GET ATTACK ANIMATOR LAYER INDEX
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

