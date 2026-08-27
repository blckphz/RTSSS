using System.Collections;
using UnityEngine;

public class CharacterAnimator :
    MonoBehaviour,
    IAttackAnimation
{
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private string attackTrigger = "Attack";

    private bool attackFinished = true;

    private void Awake()
    {
        if (animator == null)
        {
            animator =
                GetComponent<Animator>();
        }
    }

    public void PlayAttackAnimation()
    {
        if (animator == null)
        {
            attackFinished = true;
            return;
        }

        attackFinished = false;

        animator.SetTrigger(
            attackTrigger
        );
    }

    public IEnumerator WaitForAttackFinished()
    {
        while (!attackFinished)
        {
            yield return null;
        }
    }

    // ==================================================
    // ANIMATION EVENT
    // ==================================================

    public void OnAttackAnimationFinished()
    {
        attackFinished = true;
    }
}