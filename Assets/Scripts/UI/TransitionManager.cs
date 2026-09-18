using UnityEngine;

public class TransitionManager : MonoBehaviour
{
    public MainMenuClassSelector mainMenuClassSelector;

    [Header("Transition")]
    [SerializeField] private Animator transitionAnimator;

    private Transform grandParent;

    private void Awake()
    {
        grandParent = transform.parent?.parent;

        if (grandParent != null)
        {
            DontDestroyOnLoad(grandParent.gameObject);
        }
    }

    public void CallTransition()
    {
        mainMenuClassSelector.StartGamePressed();
    }

    public void transitionEnd()
    {
        if (grandParent != null)
        {
            grandParent.gameObject.SetActive(false);
        }
    }

    public void FetchTransition()
    {
        if (grandParent != null)
        {
            // Turn it back on
            grandParent.gameObject.SetActive(true);
        }

        if (transitionAnimator != null)
        {
            // Completely reset the Animator
            transitionAnimator.Rebind();
            transitionAnimator.Update(0f);
        }
    }
}