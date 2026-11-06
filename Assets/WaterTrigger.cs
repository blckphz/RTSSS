
using UnityEngine;
using UnityEngine.Tilemaps;

public class WaterTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Tilemap sandTilemap;

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    private BossLobsterBrain boss;
    private bool previousOnWater;
    private bool hasPreviousState;

    private void Awake()
    {
        if (sandTilemap == null)
            sandTilemap = GetComponent<Tilemap>();

        if (sandTilemap == null)
            Debug.LogError("[WATER DEBUG] Sand Tilemap NOT assigned!");
        else
            Debug.Log("[WATER DEBUG] Using Sand Tilemap: " + sandTilemap.name);
    }

    private void Update()
    {
        // Find the dynamically spawned boss.
        if (boss == null)
        {
            boss = FindFirstObjectByType<BossLobsterBrain>();

            if (boss == null)
                return;

            hasPreviousState = false;

            if (showDebug)
                Debug.Log("[WATER DEBUG] Found spawned boss: " + boss.name);
        }

        if (sandTilemap == null)
            return;

        Vector3 worldPosition = boss.transform.position;
        Vector3Int cell = sandTilemap.WorldToCell(worldPosition);

        bool hasSand = sandTilemap.HasTile(cell);
        bool onWater = !hasSand;

        boss.SetOnWater(onWater);

        if (!hasPreviousState || onWater != previousOnWater)
        {
            if (showDebug)
            {
                Debug.Log(
                    "[WATER DEBUG] Boss: " + boss.name +
                    " | Position: " + worldPosition +
                    " | Cell: " + cell +
                    " | Has Sand: " + hasSand +
                    " | On Water: " + onWater
                );
            }

            previousOnWater = onWater;
            hasPreviousState = true;
        }
    }
}
