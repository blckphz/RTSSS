
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimationController : MonoBehaviour, IAttackAnimation
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Walk Animation States")]
    [SerializeField] private string walkDownState = "basicEnemyWalkDown";
    [SerializeField] private string walkUpState = "basicEnemyWalkUp";
    [SerializeField] private string walkLeftState = "basicEnemyWalkLeft";
    [SerializeField] private string walkRightState = "basicEnemyWalkRight";

    [Header("Idle Animation State")]
    [SerializeField] private string idleState = "Idle";

    [Header("Attack Sequence Animator Bool")]
    [SerializeField] private string attackSequenceBool = "IsAttacking";

    [Header("Enemy Attack Animation Layer")]
    [SerializeField] private string attackAnimatorLayerName = "attackLayer";

    [Header("Enemy Attack Animation States")]
    [SerializeField] private string enemyAttackUpState = "enemyattackup";
    [SerializeField] private string enemyAttackDownState = "enemyattackdown";
    [SerializeField] private string enemyAttackLeftState = "enemyattackleft";
    [SerializeField] private string enemyAttackRightState = "enemyattackright";

    [Header("Animation Settings")]
    [SerializeField] private bool skipWalkAnimation = false;
    [SerializeField] private bool skipAttackAnimations = false;

    private bool isWalking;
    private int attackAnimatorLayerIndex = -1;
    private bool attackFinished = true;
    private bool attackHitEventReceived;
    private bool attackFinishedEventReceived;

    // Independent melee prefab reference
    private meleeBehav meleeBehaviour;

    // Last calculated attack direction
    private Vector2Int currentAttackDirection = Vector2Int.up;

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

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
                "[AnimationController] Animator has no controller.",
                this
            );
            return;
        }

        attackAnimatorLayerIndex =
            animator.GetLayerIndex(attackAnimatorLayerName);

        if (!string.IsNullOrEmpty(attackSequenceBool))
            animator.SetBool(attackSequenceBool, false);
    }

    // ============================================================
    // MELEE PREFAB REGISTRATION
    // ============================================================

    public void RegisterMelee(meleeBehav melee)
    {
        meleeBehaviour = melee;

        if (meleeBehaviour != null)
        {
            meleeBehaviour.SetAttackDirection(
                currentAttackDirection
            );
        }
    }

    public void UnregisterMelee(meleeBehav melee)
    {
        if (meleeBehaviour == melee)
            meleeBehaviour = null;
    }

    private void SendDirectionToMelee(Vector2Int direction)
    {
        if (direction == Vector2Int.zero)
            return;

        currentAttackDirection = direction;

        if (meleeBehaviour != null)
        {
            meleeBehaviour.SetAttackDirection(direction);
        }
    }

    public Vector2Int GetCurrentAttackDirection()
    {
        return currentAttackDirection;
    }

    // ============================================================
    // ATTACK SEQUENCE BOOL
    // ============================================================

    public void SetAttackSequenceActive(bool active)
    {
        if (animator == null || string.IsNullOrEmpty(attackSequenceBool))
            return;

        animator.SetBool(attackSequenceBool, active);
    }

    public bool IsAttackSequenceActive()
    {
        if (animator == null || string.IsNullOrEmpty(attackSequenceBool))
            return false;

        return animator.GetBool(attackSequenceBool);
    }

    // ============================================================
    // GENERIC ATTACK
    // ============================================================

    public void PlayAttackAnimation()
    {
        if (skipAttackAnimations || animator == null)
        {
            attackFinished = true;
            return;
        }

        attackFinished = false;
    }

    public IEnumerator WaitForAttackFinished()
    {
        while (!attackFinished)
            yield return null;
    }

    public void OnAttackAnimationFinished()
    {
        attackFinished = true;

        AttackUnit attackUnit =
            GetComponentInParent<AttackUnit>();

        if (attackUnit != null)
            attackUnit.OnAttackAnimationFinished();
    }

    // ============================================================
    // CONVERT DIRECTION
    // ============================================================

    private Vector2Int ConvertDirectionToWorldDirection(
        Vector2Int direction)
    {
        if (direction == Vector2Int.zero)
            return Vector2Int.zero;

        Vector3 logicalDirection = new Vector3(
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

        if (Mathf.Abs(logicalDirection.y) >=
            Mathf.Abs(logicalDirection.x))
        {
            return logicalDirection.y >= 0f
                ? Vector2Int.up
                : Vector2Int.down;
        }

        return logicalDirection.x >= 0f
            ? Vector2Int.right
            : Vector2Int.left;
    }

    // ============================================================
    // MOVEMENT
    // ============================================================

    public void SetMovementDirection(Vector2Int direction)
    {
        if (animator == null || direction == Vector2Int.zero)
            return;

        isWalking = true;

        if (skipWalkAnimation)
            return;

        Vector2Int visualDirection =
            ConvertDirectionToWorldDirection(direction);

        if (visualDirection == Vector2Int.up)
            PlayWalkUp();
        else if (visualDirection == Vector2Int.down)
            PlayWalkDown();
        else if (visualDirection == Vector2Int.left)
            PlayWalkLeft();
        else if (visualDirection == Vector2Int.right)
            PlayWalkRight();
    }

    // ============================================================
    // WALK
    // ============================================================

    private void PlayWalkUp()
    {
        PlayWalkState(walkUpState);
    }

    private void PlayWalkDown()
    {
        PlayWalkState(walkDownState);
    }

    private void PlayWalkLeft()
    {
        PlayWalkState(walkLeftState);
    }

    private void PlayWalkRight()
    {
        PlayWalkState(walkRightState);
    }

    private void PlayWalkState(string stateName)
    {
        if (animator == null || skipWalkAnimation)
            return;

        if (string.IsNullOrEmpty(stateName))
        {
            Debug.LogWarning(
                "[AnimationController] Walk state is empty.",
                this
            );
            return;
        }

        if (IsCurrentState(stateName, 0))
            return;

        animator.Play(stateName, 0, 0f);
    }

    // ============================================================
    // ENEMY ATTACK
    // ============================================================

    public void PlayEnemyAttack(Vector2Int direction)
    {
        if (animator == null || direction == Vector2Int.zero)
            return;

        Vector2Int visualDirection =
            ConvertDirectionToWorldDirection(direction);

        // Send the same direction to the independent melee prefab.
        SendDirectionToMelee(visualDirection);

        if (skipAttackAnimations)
        {
            attackHitEventReceived = true;
            attackFinishedEventReceived = true;
            attackFinished = true;

            AttackUnit attackUnit =
                GetComponentInParent<AttackUnit>();

            if (attackUnit != null)
            {
                attackUnit.OnAttackAnimationEvent();
                attackUnit.OnAttackAnimationFinished();
            }

            return;
        }

        if (attackAnimatorLayerIndex < 0)
        {
            attackAnimatorLayerIndex =
                animator.GetLayerIndex(attackAnimatorLayerName);
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

        string stateToPlay = null;

        if (visualDirection == Vector2Int.up)
            stateToPlay = enemyAttackUpState;
        else if (visualDirection == Vector2Int.down)
            stateToPlay = enemyAttackDownState;
        else if (visualDirection == Vector2Int.left)
            stateToPlay = enemyAttackLeftState;
        else if (visualDirection == Vector2Int.right)
            stateToPlay = enemyAttackRightState;

        if (string.IsNullOrEmpty(stateToPlay))
        {
            Debug.LogError(
                "[AnimationController] Enemy attack state is empty.",
                this
            );
            return;
        }

        attackHitEventReceived = false;
        attackFinishedEventReceived = false;
        attackFinished = false;

        animator.Play(
            stateToPlay,
            attackAnimatorLayerIndex,
            0f
        );

        animator.Update(0f);
    }

    public void PlayEnemyAttack(
        Vector2Int attackerTile,
        Vector2Int targetTile)
    {
        Vector2Int direction =
            targetTile - attackerTile;

        direction.x = Mathf.Clamp(direction.x, -1, 1);
        direction.y = Mathf.Clamp(direction.y, -1, 1);

        PlayEnemyAttack(direction);
    }

    // ============================================================
    // ATTACK HIT
    // ============================================================

    public void AttackHit()
    {
        if (attackHitEventReceived)
            return;

        attackHitEventReceived = true;

        AttackUnit attackUnit =
            GetComponentInParent<AttackUnit>();

        if (attackUnit != null)
            attackUnit.OnAttackAnimationEvent();
    }

    // ============================================================
    // ATTACK FINISHED
    // ============================================================

    public void AttackFinished()
    {
        attackFinishedEventReceived = true;
        attackFinished = true;

        AttackUnit attackUnit =
            GetComponentInParent<AttackUnit>();

        if (attackUnit != null)
            attackUnit.OnAttackAnimationFinished();
    }

    // ============================================================
    // WAIT FOR ATTACK HIT
    // ============================================================

    public IEnumerator WaitForAttackHit()
    {
        while (!attackHitEventReceived)
            yield return null;
    }

    // ============================================================
    // WAIT FOR ENEMY ATTACK FINISHED
    // ============================================================

    public IEnumerator WaitForEnemyAttackFinished()
    {
        while (!attackFinishedEventReceived)
            yield return null;
    }

    // ============================================================
    // BACKWARD COMPATIBILITY
    // ============================================================

    public void PlayEnemyAttackDown(Vector2Int direction)
    {
        PlayEnemyAttack(direction);
    }

    public void PlayEnemyAttackDown(
        Vector2Int attackerTile,
        Vector2Int targetTile)
    {
        PlayEnemyAttack(attackerTile, targetTile);
    }

    // ============================================================
    // IDLE
    // ============================================================

    public void PlayIdle()
    {
        if (animator == null)
            return;

        isWalking = false;

        if (IsCurrentState(idleState, 0))
            return;

        animator.Play(idleState, 0, 0f);
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
        int layerIndex)
    {
        if (animator == null || layerIndex < 0)
            return false;

        if (string.IsNullOrEmpty(stateName))
            return false;

        AnimatorStateInfo stateInfo =
            animator.GetCurrentAnimatorStateInfo(layerIndex);

        return stateInfo.IsName(stateName);
    }

    // ============================================================
    // GET ATTACK LAYER INDEX
    // ============================================================

    public int GetAttackAnimatorLayerIndex()
    {
        if (animator == null)
            return -1;

        if (attackAnimatorLayerIndex < 0)
        {
            attackAnimatorLayerIndex =
                animator.GetLayerIndex(attackAnimatorLayerName);
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

    public string GetAttackAnimatorLayerName()
    {
        return attackAnimatorLayerName;
    }

    // ============================================================
    // PUBLIC STATE
    // ============================================================

    public bool IsWalking() => isWalking;
    public bool HasAttackHit() => attackHitEventReceived;
    public bool HasAttackFinished() => attackFinishedEventReceived;
    public bool SkipsAttackAnimations() => skipAttackAnimations;
    public bool SkipsWalkAnimation() => skipWalkAnimation;
}
