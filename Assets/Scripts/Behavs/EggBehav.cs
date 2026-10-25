
using UnityEngine;

public class EggBehav : MonoBehaviour
{
    private Vector3 startPosition;
    private Vector3 targetPosition;

    private float speed = 8f;
    private float flightTime;
    private float elapsedTime;

    [SerializeField]
    private float arcHeight = 2f;

    [SerializeField]
    private GridManager gridManager;

    private Quaternion originalRotation;
    private bool initialized;

    public void Initialize(
        Vector3 targetPosition,
        float speed
    )
    {
        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<GridManager>();
        }

        this.targetPosition = targetPosition;
        this.speed = Mathf.Max(0.01f, speed);

        startPosition = transform.position;
        originalRotation = transform.rotation;

        float distance = Vector3.Distance(
            startPosition,
            targetPosition
        );

        flightTime = Mathf.Max(
            0.05f,
            distance / this.speed
        );

        elapsedTime = 0f;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized)
            return;

        elapsedTime += Time.deltaTime;

        float t = Mathf.Clamp01(
            elapsedTime / flightTime
        );

        Vector3 position = Vector3.Lerp(
            startPosition,
            targetPosition,
            t
        );

        float arc = Mathf.Sin(t * Mathf.PI) * arcHeight;

        position.y += arc;

        transform.position = position;
        transform.rotation = originalRotation;

        if (t >= 1f)
            Land();
    }

    private void Land()
    {
        if (!initialized)
            return;

        initialized = false;
        transform.position = targetPosition;

        if (gridManager == null)
        {
            Debug.LogError(
                "[EggBehav] GridManager was not found.",
                this
            );

            return;
        }

        Vector2Int gridPosition =
            gridManager.WorldToGridPosition(targetPosition);

        gridManager.RegisterWalkableObstacle(gridPosition);
    }
}
