
using UnityEngine;
using UnityEngine.Tilemaps;

public class WaterTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Tilemap sandTilemap;

    private BossLobsterBrain boss;

    private void Awake()
    {
        if (sandTilemap == null)
            sandTilemap = GetComponent<Tilemap>();
    }

    private void Update()
    {
        if (boss == null)
        {
            boss = FindFirstObjectByType<BossLobsterBrain>();

            if (boss == null)
                return;
        }

    }
}
