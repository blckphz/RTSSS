using UnityEngine;

public class transitionLinkManager : MonoBehaviour
{
    private transitionGameManager transitionManager;


    // ============================================================
    // UNITY
    // ============================================================

    private void Start()
    {
        transitionManager =
            FindFirstObjectByType<transitionGameManager>();

        if (transitionManager == null)
        {
            Debug.LogError(
                "[transitionLinkManager] transitionGameManager was not found in the scene!",
                this
            );
        }
    }


    // ============================================================
    // TRANSITION PEAK
    // ============================================================

    // Called by the transition animation at its peak.
    public void TransitionPeak()
    {
        if (transitionManager == null)
        {
            transitionManager =
                FindFirstObjectByType<transitionGameManager>();
        }

        if (transitionManager == null)
        {
            Debug.LogError(
                "[transitionLinkManager] Cannot find transitionGameManager!",
                this
            );

            return;
        }

        // Let transitionGameManager decide whether
        // we are going to combat or returning to the map.
        transitionManager.StartEncounterAtPeak();
    }


    // ============================================================
    // END TRANSITION
    // ============================================================

    // Called by the transition animation when it finishes.
    public void EndTransition()
    {
        if (transitionManager == null)
        {
            transitionManager =
                FindFirstObjectByType<transitionGameManager>();
        }

        if (transitionManager == null)
        {
            Debug.LogError(
                "[transitionLinkManager] Cannot find transitionGameManager!",
                this
            );

            return;
        }

        transitionManager.EndTransition();
    }
}
