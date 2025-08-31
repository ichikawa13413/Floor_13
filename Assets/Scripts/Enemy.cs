using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private float damagePower;
    public float EnemyDamagePower { get => damagePower; }
}
