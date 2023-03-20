using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    private float health = 100f;
    private float maxHealth = 100f;

    private void Start()
    {
        health = maxHealth;
    }

    private void OnEnable()
    {
        Actions.OnPlayerTakeDamage += TakeDamage;
    }

    private void OnDisable()
    {
        Actions.OnPlayerTakeDamage -= TakeDamage;
    }

    public void TakeDamage(float _damage)
    {
        health -= _damage;
        if (health <= 0)
        {
            health = 0;
            GameManager.instance.EndGame();
        }
    }
}
