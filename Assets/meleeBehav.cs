
using UnityEngine;

public class meleeBehav : MonoBehaviour
{
    [Header("Melee Visual")]
    [SerializeField] private Transform meleeVisual;

    [Header("Spawn Point")]
    [SerializeField] private string spawnPointName = "AbilitySpawnPoint";

    [Header("Directional Rotation")]
    [SerializeField] private float upRotation = 0f;
    [SerializeField] private float rightRotation = -90f;
    [SerializeField] private float downRotation = 180f;
    [SerializeField] private float leftRotation = 90f;

    private AnimationController ownerController;
    private Vector2Int currentDirection = Vector2Int.up;

    private void Awake()
    {
        if (meleeVisual == null)
            meleeVisual = transform;
    }

    public void Initialize(AnimationController controller)
    {
        ownerController = controller;

        if (ownerController == null)
        {
            Debug.LogWarning(
                "[meleeBehav] AnimationController was not provided.",
                this
            );
            return;
        }

        ownerController.RegisterMelee(this);

        MoveToAbilitySpawnPoint();
    }

    private void MoveToAbilitySpawnPoint()
    {
        if (ownerController == null)
            return;

        Transform[] children =
            ownerController.GetComponentsInChildren<Transform>(true);

        Transform spawnPoint = null;

        foreach (Transform child in children)
        {
            if (child.name == spawnPointName)
            {
                spawnPoint = child;
                break;
            }
        }

        if (spawnPoint == null)
        {
            Debug.LogWarning(
                $"[meleeBehav] Could not find {spawnPointName} on " +
                $"{ownerController.name}. Woosh will stay at its current position.",
                this
            );
            return;
        }

        // Move the entire woosh object to the spawn point.
        transform.position = spawnPoint.position;

        Debug.Log(
            $"[meleeBehav] Woosh moved to {spawnPoint.name} at " +
            $"{spawnPoint.position}",
            this
        );
    }

    public void SetAttackDirection(Vector2Int direction)
    {
        if (direction == Vector2Int.zero)
            return;

        currentDirection = direction;

        float rotation = GetRotation(direction);

        meleeVisual.localRotation =
            Quaternion.Euler(0f, 0f, rotation);
    }

    private float GetRotation(Vector2Int direction)
    {
        if (direction == Vector2Int.up) return upRotation;
        if (direction == Vector2Int.right) return rightRotation;
        if (direction == Vector2Int.down) return downRotation;
        if (direction == Vector2Int.left) return leftRotation;

        return upRotation;
    }

    public Vector2Int GetAttackDirection()
    {
        return currentDirection;
    }

    public void DestroyMelee()
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (ownerController != null)
            ownerController.UnregisterMelee(this);
    }
}
