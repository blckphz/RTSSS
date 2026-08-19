using UnityEngine;
using UnityEngine.InputSystem;

public class GridSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GameObject prefab;
    [SerializeField] private Camera mainCamera;

    private Transform mainCameraTransform;

    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera != null)
        {
            mainCameraTransform = mainCamera.transform;
        }
        else
        {
            Debug.LogError("[GridSpawner] Main Camera could not be found!");
        }

        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<GridManager>();

            if (gridManager == null)
            {
                Debug.LogError("[GridSpawner] GridManager could not be found!");
            }
        }

        if (prefab == null)
        {
            Debug.LogError("[GridSpawner] No prefab assigned!");
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            SpawnAtMouse();
        }
    }

    // ==================================================
    // SPAWNING
    // ==================================================

    private void SpawnAtMouse()
    {
        if (prefab == null || gridManager == null || mainCamera == null)
        {
            return;
        }

        // ----------------------------------------------
        // SCREEN -> WORLD -> GRID
        // ----------------------------------------------

        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();

        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(
            new Vector3(
                mouseScreenPosition.x,
                mouseScreenPosition.y,
                Mathf.Abs(mainCameraTransform.position.z)
            )
        );

        Vector2Int gridPosition = gridManager.WorldToGridPosition(mouseWorldPosition);

        // ----------------------------------------------
        // VALIDATION & PLACEMENT
        // ----------------------------------------------

        if (!gridManager.IsInsideGrid(gridPosition) || gridManager.IsCellOccupied(gridPosition))
        {
            return;
        }

        GameObject unit = Instantiate(prefab);
        unit.name = $"{prefab.name}_{gridPosition.x}_{gridPosition.y}";

        if (!gridManager.PlaceUnit(unit, gridPosition))
        {
            Destroy(unit);
        }
    }
}