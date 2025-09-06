using UnityEngine;


public class Enemy : MonoBehaviour
{
    private Transform _transform;

    [SerializeField] private float damagePower;
    public float EnemyDamagePower { get => damagePower; }


    private void Awake()
    {
        _transform = transform;
    }

    private void FixedUpdate()
    {

    }
}