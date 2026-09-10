using System.Collections.Generic;
using UnityEngine;

public class ConditionManager : MonoBehaviour
{
    // ============================================================
    // SHADER
    // ============================================================

    private const string StunIntensityProperty =
        "_StunIntensity";

    private const float StunIntensityOn = 0.3f;
    private const float StunIntensityOff = 0f;


    // ============================================================
    // STUN DATA
    // ============================================================

    private class StunData
    {
        public int remainingTurns;
    }

    private static readonly Dictionary<GameObject, StunData>
        stunnedObjects =
            new Dictionary<GameObject, StunData>();


    // ============================================================
    // VISUAL
    // ============================================================

    [Header("Stun Visual")]

    [SerializeField]
    private bool useStunVisual = true;

    [SerializeField]
    private bool smoothStunVisual = true;

    [SerializeField, Min(0.01f)]
    private float visualFadeSpeed = 8f;

    private Renderer[] cachedRenderers;

    private MaterialPropertyBlock propertyBlock;

    private float currentStunIntensity;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        cachedRenderers =
            GetComponentsInChildren<Renderer>(
                true
            );

        propertyBlock =
            new MaterialPropertyBlock();

        currentStunIntensity =
            StunIntensityOff;

        SetStunVisualImmediate(
            StunIntensityOff
        );
    }


    private void Update()
    {
        if (!useStunVisual)
        {
            return;
        }

        bool stunned =
            IsStunned(gameObject);

        float targetIntensity =
            stunned
                ? StunIntensityOn
                : StunIntensityOff;

        if (smoothStunVisual)
        {
            currentStunIntensity =
                Mathf.MoveTowards(
                    currentStunIntensity,
                    targetIntensity,
                    visualFadeSpeed *
                    Time.deltaTime
                );
        }
        else
        {
            currentStunIntensity =
                targetIntensity;
        }

        SetStunVisual(
            currentStunIntensity
        );
    }


    private void OnDestroy()
    {
        ClearStun(gameObject);
    }


    // ============================================================
    // APPLY STUN
    // ============================================================

    public static bool ApplyStun(
        GameObject target,
        int duration)
    {
        if (target == null)
        {
            return false;
        }

        if (duration <= 0)
        {
            return false;
        }

        AttackUnit attackUnit =
            target.GetComponent<AttackUnit>();

        if (attackUnit == null)
        {
            Debug.LogWarning(
                $"[STUN] Cannot stun {target.name}: " +
                "AttackUnit not found."
            );

            return false;
        }

        if (attackUnit.IsDead())
        {
            Debug.Log(
                $"[STUN] {target.name} is dead. " +
                "Stun ignored."
            );

            return false;
        }

        StunData existingStun;

        // --------------------------------------------------------
        // ALREADY STUNNED
        // --------------------------------------------------------

        if (
            stunnedObjects.TryGetValue(
                target,
                out existingStun
            )
        )
        {
            int oldTurns =
                existingStun.remainingTurns;

            // Refresh to the larger value.
            existingStun.remainingTurns =
                Mathf.Max(
                    existingStun.remainingTurns,
                    duration
                );

            Debug.Log(
                $"[STUN] {target.name} stun refreshed. " +
                $"Old={oldTurns} turns | " +
                $"New={existingStun.remainingTurns} turns"
            );

            ConditionManager manager =
                target.GetComponent<ConditionManager>();

            if (manager != null)
            {
                manager.SetStunVisualImmediate(
                    StunIntensityOn
                );
            }

            return true;
        }


        // --------------------------------------------------------
        // NEW STUN
        // --------------------------------------------------------

        StunData stunData =
            new StunData
            {
                remainingTurns = duration
            };

        stunnedObjects.Add(
            target,
            stunData
        );


        // Make sure the target has a ConditionManager
        // for the shader visual.
        ConditionManager conditionManager =
            target.GetComponent<ConditionManager>();

        if (conditionManager == null)
        {
            conditionManager =
                target.AddComponent<ConditionManager>();
        }


        conditionManager.SetStunVisualImmediate(
            StunIntensityOn
        );


        Debug.Log(
            $"[STUN] {target.name} STUNNED. " +
            $"Duration={duration} turn(s)"
        );

        return true;
    }


    // ============================================================
    // APPLY STUN - ATTACK UNIT
    // ============================================================

    public static bool ApplyStun(
        AttackUnit target,
        int duration)
    {
        if (target == null)
        {
            return false;
        }

        return ApplyStun(
            target.gameObject,
            duration
        );
    }


    // ============================================================
    // IS STUNNED
    // ============================================================

    public static bool IsStunned(
        GameObject target)
    {
        if (target == null)
        {
            return false;
        }

        StunData stunData;

        if (
            !stunnedObjects.TryGetValue(
                target,
                out stunData
            )
        )
        {
            return false;
        }

        if (
            stunData == null ||
            stunData.remainingTurns <= 0
        )
        {
            stunnedObjects.Remove(
                target
            );

            return false;
        }

        return true;
    }


    // ============================================================
    // IS STUNNED - ATTACK UNIT
    // ============================================================

    public static bool IsStunned(
        AttackUnit target)
    {
        if (target == null)
        {
            return false;
        }

        return IsStunned(
            target.gameObject
        );
    }


    // ============================================================
    // GET REMAINING TURNS
    // ============================================================

    public static int GetStunRemaining(
        GameObject target)
    {
        if (target == null)
        {
            return 0;
        }

        StunData stunData;

        if (
            !stunnedObjects.TryGetValue(
                target,
                out stunData
            )
        )
        {
            return 0;
        }

        if (
            stunData == null ||
            stunData.remainingTurns <= 0
        )
        {
            return 0;
        }

        return stunData.remainingTurns;
    }


    // ============================================================
    // GET REMAINING TURNS - ATTACK UNIT
    // ============================================================

    public static int GetStunRemaining(
        AttackUnit target)
    {
        if (target == null)
        {
            return 0;
        }

        return GetStunRemaining(
            target.gameObject
        );
    }


    // ============================================================
    // CONSUME ONE STUN TURN
    // ============================================================

    public static bool ConsumeStunTurn(
        GameObject target)
    {
        if (target == null)
        {
            return false;
        }

        StunData stunData;

        if (
            !stunnedObjects.TryGetValue(
                target,
                out stunData
            )
        )
        {
            return false;
        }

        if (
            stunData == null ||
            stunData.remainingTurns <= 0
        )
        {
            ClearStun(target);

            return false;
        }


        // --------------------------------------------------------
        // CONSUME TURN
        // --------------------------------------------------------

        stunData.remainingTurns--;

        Debug.Log(
            $"[STUN] {target.name} consumed a stun turn. " +
            $"Remaining={stunData.remainingTurns}"
        );


        // --------------------------------------------------------
        // STUN FINISHED
        // --------------------------------------------------------

        if (stunData.remainingTurns <= 0)
        {
            stunnedObjects.Remove(
                target
            );

            ConditionManager manager =
                target.GetComponent<ConditionManager>();

            if (manager != null)
            {
                manager.SetStunVisualImmediate(
                    StunIntensityOff
                );
            }

            Debug.Log(
                $"[STUN] {target.name} STUN EXPIRED."
            );
        }

        return true;
    }


    // ============================================================
    // CONSUME ONE STUN TURN - ATTACK UNIT
    // ============================================================

    public static bool ConsumeStunTurn(
        AttackUnit target)
    {
        if (target == null)
        {
            return false;
        }

        return ConsumeStunTurn(
            target.gameObject
        );
    }


    // ============================================================
    // CLEAR STUN
    // ============================================================

    public static void ClearStun(
        GameObject target)
    {
        if (target == null)
        {
            return;
        }

        bool wasStunned =
            stunnedObjects.Remove(
                target
            );

        ConditionManager manager =
            target.GetComponent<ConditionManager>();

        if (manager != null)
        {
            manager.SetStunVisualImmediate(
                StunIntensityOff
            );
        }

        if (wasStunned)
        {
            Debug.Log(
                $"[STUN] {target.name} STUN CLEARED."
            );
        }
    }


    // ============================================================
    // CLEAR STUN - ATTACK UNIT
    // ============================================================

    public static void ClearStun(
        AttackUnit target)
    {
        if (target == null)
        {
            return;
        }

        ClearStun(
            target.gameObject
        );
    }


    // ============================================================
    // CLEAR ALL CONDITIONS
    // ============================================================

    public static void ClearAllConditions(
        GameObject target)
    {
        ClearStun(target);
    }


    // ============================================================
    // SHADER VISUAL
    // ============================================================

    private void SetStunVisual(
        float intensity)
    {
        if (
            !useStunVisual ||
            cachedRenderers == null
        )
        {
            return;
        }

        for (
            int i = 0;
            i < cachedRenderers.Length;
            i++
        )
        {
            Renderer renderer =
                cachedRenderers[i];

            if (renderer == null)
            {
                continue;
            }

            if (
                renderer.sharedMaterial == null ||
                !renderer.sharedMaterial.HasProperty(
                    StunIntensityProperty
                )
            )
            {
                continue;
            }

            renderer.GetPropertyBlock(
                propertyBlock
            );

            propertyBlock.SetFloat(
                StunIntensityProperty,
                intensity
            );

            renderer.SetPropertyBlock(
                propertyBlock
            );
        }
    }


    // ============================================================
    // IMMEDIATE SHADER UPDATE
    // ============================================================

    private void SetStunVisualImmediate(
        float intensity)
    {
        currentStunIntensity =
            intensity;

        SetStunVisual(
            intensity
        );
    }
}