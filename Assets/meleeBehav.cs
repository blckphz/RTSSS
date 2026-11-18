
using UnityEngine;

public class meleeBehav : MonoBehaviour
{
    [Header("Melee Visual")]
    [SerializeField] private Transform meleeVisual;

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

    // Called by the spawning script.
    public void Initialize(AnimationController controller)
    {
        ownerController = controller;

        if (ownerController != null)
        {
            ownerController.RegisterMelee(this);
        }
        else
        {
            Debug.LogWarning(
                "[meleeBehav] AnimationController was not provided.",
                this
            );
        }
    }

    // Called by AnimationController.
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
        if (direction == Vector2Int.up)
            return upRotation;

        if (direction == Vector2Int.right)
            return rightRotation;

        if (direction == Vector2Int.down)
            return downRotation;

        if (direction == Vector2Int.left)
            return leftRotation;

        return upRotation;
    }

    public Vector2Int GetAttackDirection()
    {
        return currentDirection;
    }

    // Animation Event - destroy melee.
    public void DestroyMelee()
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (ownerController != null)
        {
            ownerController.UnregisterMelee(this);
        }
    }
}
