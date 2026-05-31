using UnityEngine;

public class FootHitbox : MonoBehaviour
{
    public Vector2 size = new Vector2(0.5f, 0.5f);

    public PlayerHealth playerHealth { get; private set; }

    private void Awake()
    {
        playerHealth = GetComponentInParent<PlayerHealth>();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, new Vector3(size.x, size.y, 0f));
    }
}
