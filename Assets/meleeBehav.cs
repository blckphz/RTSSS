
using UnityEngine;

public class meleeBehav : MonoBehaviour
{
    [Header("Melee Visual")]
    [SerializeField] private Transform meleeVisual;

    [Header("Target Point")]
    [SerializeField] private string visualTargetPointName = "VisualTargetPoint";

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
    }

    public void SetTarget(GameObject target)
    {
        if (target == null)
            return;

        Transform[] children =
            target.GetComponentsInChildren<Transform>(true);

        Transform visualTargetPoint = null;

        foreach (Transform child in children)
        {
            if (child.name == visualTargetPointName)
            {
                visualTargetPoint = child;
                break;
            }
        }

        if (visualTargetPoint == null)
        {
            Debug.LogWarning(
                $"[meleeBehav] Could not find {visualTargetPointName} " +
                $"on {target.name}. Using target position.",
                target
            );

            transform.position = target.transform.position;
            return;
        }

        transform.position = visualTargetPoint.position;

        Debug.Log(
            $"[meleeBehav] Woosh moved to {target.name}/" +
            $"{visualTargetPointName} at {visualTargetPoint.position}",
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
