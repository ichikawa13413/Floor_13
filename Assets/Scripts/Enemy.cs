using UnityEngine;

public class Enemy : MonoBehaviour
{
    private Transform _transform;

    [SerializeField] private float damagePower;
    public float EnemyDamagePower { get => damagePower; }

    //--ノックバック系--
    [SerializeField] private float knockbackForce;
    [SerializeField] private Vector3 knockbackDirection;

    private void Awake()
    {
        _transform = transform;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Player player = collision.gameObject.GetComponent<Player>();
        if (player != null)
        {
            KnockbackFunction(player);
        }
    }

    private void KnockbackFunction(Player player)
    {
        Rigidbody playerRB = player.GetComponent<Rigidbody>();

        Vector3 direction = player.transform.position - _transform.position;
        Vector3 knockbackVector = (direction.normalized * knockbackForce) + (knockbackDirection * knockbackForce);

        playerRB.AddForce(knockbackVector,ForceMode.VelocityChange);
    }
}