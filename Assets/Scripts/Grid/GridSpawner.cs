using UnityEngine;
using UnityEngine.InputSystem;

public class GridSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GameObject prefab;
    [SerializeField] private Camera mainCamera;

    private Transform mainCameraTransform;

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
            Debug.LogError(
                "[GridSpawner] Main Camera could not be found!"
            );
        }

        if (gridManager == null)
        {
            gridManager =
                FindFirstObjectByType<GridManager>();

            if (gridManager == null)
            {
                Debug.LogError(
                    "[GridSpawner] GridManager could not be found!"
                );
            }
        }

        if (prefab == null)
        {
            Debug.LogError(
                "[GridSpawner] No prefab assigned!"
            );
        }
    }

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            SpawnAtMouse();
        }
    }

    private void SpawnAtMouse()
    {
        if (prefab == null ||
            gridManager == null ||
            mainCamera == null)
        {
            return;
        }

        Vector2 mouseScreenPosition =
            Mouse.current.position.ReadValue();

        // ========================================================
        // RAYCAST TO BOARD
        // ========================================================

        Ray ray =
            mainCamera.ScreenPointToRay(
                mouseScreenPosition
            );

        Vector3 boardNormal =
            gridManager.transform.forward;

        Plane boardPlane =
            new Plane(
                boardNormal,
                gridManager.transform.position
            );

        if (!boardPlane.Raycast(
                ray,
                out float enterDistance))
        {
            return;
        }

        Vector3 mouseWorldPosition =
            ray.GetPoint(enterDistance);


        // ========================================================
        // WORLD -> LOGICAL GRID
        // ========================================================

        Vector2Int gridPosition =
            gridManager.WorldToGridPosition(
                mouseWorldPosition
            );


        // ========================================================
        // VALIDATION
        // ========================================================

        if (!gridManager.IsInsideGrid(gridPosition))
        {
            return;
        }

        if (gridManager.IsCellOccupied(gridPosition))
        {
            return;
        }


        // ========================================================
        // SPAWN
        // ========================================================

        GameObject unit =
            Instantiate(prefab);

        unit.name =
            $"{prefab.name}_{gridPosition.x}_{gridPosition.y}";


        if (!gridManager.PlaceUnit(
                unit,
                gridPosition))
        {
            Destroy(unit);
        }
    }
}