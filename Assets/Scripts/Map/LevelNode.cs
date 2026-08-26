using UnityEngine;

public class LevelNode
{
    public GameObject icon;
    public Vector3 position;

    public LevelNode(GameObject icon, Vector3 position)
    {
        this.icon = icon;
        this.position = position;
    }
}