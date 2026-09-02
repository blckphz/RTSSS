using UnityEngine;

public class transitionLinkManager : MonoBehaviour
{
    private transitionGameManager transitionManager;
    public GameObject MapCanvas;
    public transitionGameManager tgm;


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
                "[transitionLinkManager] transitionGameManager was not found in the scene!"
            );
        }
    }


    // ============================================================
    // START TRANSITION
    // ============================================================

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
                "[transitionLinkManager] Cannot find transitionGameManager!"
            );

            return;
        }

        tgm.StartEncounterAtPeak();

        transitionManager.TransitionPeakProcess();
        MapCanvas.SetActive(false);
    }


    // ============================================================
    // END TRANSITION
    // ============================================================


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
                "[transitionLinkManager] Cannot find transitionGameManager!"
            );

            return;
        }
      
        transitionManager.EndTransition();
       
    }
}