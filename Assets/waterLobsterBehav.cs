using UnityEngine;

public class waterLobsterBehav : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<BossLobsterBrain>() != null)
        {
            Debug.Log("Boss Lobster is touching the water!");
        }
    }
}