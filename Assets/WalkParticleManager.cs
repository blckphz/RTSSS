using System.Collections.Generic;
using UnityEngine;

public class WalkParticleManager : MonoBehaviour
{
    public static WalkParticleManager Instance { get; private set; }

    [Header("Particle Pool")]
    [SerializeField] private GameObject walkParticlePrefab;
    [SerializeField] private int initialPoolSize = 10;
    [SerializeField] private bool expandablePool = true;

    [Header("Particle Root")]
    [SerializeField] private Transform particleRoot;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private readonly Queue<GameObject> availableParticles =
        new Queue<GameObject>();

    private readonly List<GameObject> activeParticles =
        new List<GameObject>();


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "[WalkParticles][Manager] Duplicate manager found. Destroying this object.",
                this
            );

            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (particleRoot == null)
        {
            particleRoot = transform;

            DebugLog(
                "Particle Root not assigned. Using manager Transform."
            );
        }

        DebugLog(
            $"Awake. Prefab = " +
            $"{(walkParticlePrefab != null ? walkParticlePrefab.name : "NULL")}, " +
            $"Pool Size = {initialPoolSize}, " +
            $"Expandable = {expandablePool}"
        );

        PrewarmPool();
    }


    private void OnEnable()
    {
        UnitMoveBrain.OnWalkParticleTile += HandleWalkParticleTile;

        DebugLog(
            "Subscribed to tile particle event."
        );
    }


    private void OnDisable()
    {
        UnitMoveBrain.OnWalkParticleTile -= HandleWalkParticleTile;

        DebugLog(
            "Unsubscribed from tile particle event."
        );

        ReleaseAllParticles();
    }


    private void Update()
    {
        CheckFinishedParticles();
    }


    // ============================================================
    // POOL
    // ============================================================

    private void PrewarmPool()
    {
        if (walkParticlePrefab == null)
        {
            Debug.LogError(
                "[WalkParticles][Manager] Walk Particle Prefab is NOT assigned!",
                this
            );

            return;
        }

        initialPoolSize =
            Mathf.Max(
                0,
                initialPoolSize
            );

        for (int i = 0;
             i < initialPoolSize;
             i++)
        {
            GameObject particle =
                CreateParticleInstance();

            if (particle != null)
            {
                availableParticles.Enqueue(
                    particle
                );
            }
        }

        DebugLog(
            $"Pool ready. Available particles = {availableParticles.Count}"
        );
    }


    private GameObject CreateParticleInstance()
    {
        if (walkParticlePrefab == null)
        {
            return null;
        }

        GameObject particle =
            Instantiate(
                walkParticlePrefab,
                particleRoot
            );

        particle.name =
            walkParticlePrefab.name + "_Pooled";

        particle.SetActive(false);

        particle.transform.localPosition =
            Vector3.zero;

        particle.transform.localRotation =
            Quaternion.identity;

        particle.transform.localScale =
            Vector3.one;

        return particle;
    }


    private GameObject GetParticleFromPool()
    {
        while (availableParticles.Count > 0)
        {
            GameObject particle =
                availableParticles.Dequeue();

            if (particle != null)
            {
                return particle;
            }
        }

        if (!expandablePool)
        {
            Debug.LogWarning(
                "[WalkParticles][Manager] Pool is empty and Expandable Pool is disabled.",
                this
            );

            return null;
        }

        DebugLog(
            "Pool empty. Creating additional particle."
        );

        return CreateParticleInstance();
    }


    // ============================================================
    // TILE PARTICLE
    // ============================================================

    private void HandleWalkParticleTile(
        Vector3 worldPosition
    )
    {
        GameObject particle =
            GetParticleFromPool();

        if (particle == null)
        {
            Debug.LogWarning(
                "[WalkParticles][Manager] Could not get particle for tile.",
                this
            );

            return;
        }

        particle.transform.SetParent(
            particleRoot,
            false
        );

        particle.transform.position =
            worldPosition;

        particle.transform.rotation =
            Quaternion.identity;

        particle.transform.localScale =
            Vector3.one;

        particle.SetActive(true);

        activeParticles.Add(
            particle
        );

        PlayParticleSystems(
            particle
        );

        DebugLog(
            $"Tile particle spawned -> " +
            $"Position={worldPosition}, " +
            $"Active={activeParticles.Count}, " +
            $"Available={availableParticles.Count}"
        );
    }


    // ============================================================
    // PLAY
    // ============================================================

    private void PlayParticleSystems(
        GameObject particle
    )
    {
        if (particle == null)
        {
            return;
        }

        ParticleSystem[] systems =
            particle.GetComponentsInChildren<ParticleSystem>(
                true
            );

        if (systems.Length == 0)
        {
            Debug.LogError(
                $"[WalkParticles][Manager] NO ParticleSystem found -> {particle.name}",
                particle
            );

            return;
        }

        for (int i = 0;
             i < systems.Length;
             i++)
        {
            ParticleSystem system =
                systems[i];

            if (system == null)
            {
                continue;
            }

            system.Clear(true);
            system.Play(true);
        }
    }


    // ============================================================
    // FINISHED PARTICLES
    // ============================================================

    private void CheckFinishedParticles()
    {
        if (activeParticles.Count == 0)
        {
            return;
        }

        for (int i = activeParticles.Count - 1;
             i >= 0;
             i--)
        {
            GameObject particle =
                activeParticles[i];

            if (particle == null)
            {
                activeParticles.RemoveAt(i);
                continue;
            }

            ParticleSystem[] systems =
                particle.GetComponentsInChildren<ParticleSystem>(
                    true
                );

            if (systems.Length == 0)
            {
                ReturnParticleToPool(
                    particle
                );

                activeParticles.RemoveAt(i);

                continue;
            }

            bool allFinished = true;

            for (int j = 0;
                 j < systems.Length;
                 j++)
            {
                ParticleSystem system =
                    systems[j];

                if (system == null)
                {
                    continue;
                }

                if (system.IsAlive(true))
                {
                    allFinished = false;
                    break;
                }
            }

            if (!allFinished)
            {
                continue;
            }

            ReturnParticleToPool(
                particle
            );

            activeParticles.RemoveAt(i);

            DebugLog(
                $"Tile particle finished -> " +
                $"Active={activeParticles.Count}, " +
                $"Available={availableParticles.Count}"
            );
        }
    }


    // ============================================================
    // RETURN TO POOL
    // ============================================================

    private void ReturnParticleToPool(
        GameObject particle
    )
    {
        if (particle == null)
        {
            return;
        }

        particle.SetActive(false);

        particle.transform.SetParent(
            particleRoot,
            false
        );

        particle.transform.localPosition =
            Vector3.zero;

        particle.transform.localRotation =
            Quaternion.identity;

        particle.transform.localScale =
            Vector3.one;

        availableParticles.Enqueue(
            particle
        );
    }


    // ============================================================
    // CLEANUP
    // ============================================================

    private void ReleaseAllParticles()
    {
        for (int i = 0;
             i < activeParticles.Count;
             i++)
        {
            GameObject particle =
                activeParticles[i];

            if (particle == null)
            {
                continue;
            }

            ParticleSystem[] systems =
                particle.GetComponentsInChildren<ParticleSystem>(
                    true
                );

            for (int j = 0;
                 j < systems.Length;
                 j++)
            {
                if (systems[j] == null)
                {
                    continue;
                }

                systems[j].Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear
                );
            }

            ReturnParticleToPool(
                particle
            );
        }

        activeParticles.Clear();
    }


    // ============================================================
    // DEBUG
    // ============================================================

    private void DebugLog(
        string message
    )
    {
        if (!debugLogs)
        {
            return;
        }

        Debug.Log(
            $"[WalkParticles][Manager] {message}",
            this
        );
    }
}