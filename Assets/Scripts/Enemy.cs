using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    private GameManager gameManager;
    private GameObject orbPrefab;
    private Transform player;
    public Transform firePosition;
    private float timeToNextOrbSpawn;
    private float timeBetweenOrbs;
    public GameObject currentCooldownBar;
    public AudioSource huhSpawnSound;
    private Animator animator;
    private float stamina = 100f;
    private float maxStamina = 100f;


    private void Start()
    {
        gameManager = GameManager.instance;
        gameManager.AddEnemy(this);
        animator = GetComponent<Animator>();
        timeBetweenOrbs = Random.Range(3f, 5f);
        timeToNextOrbSpawn = 0;
        player = PatternManager.instance.playerRig.transform;
        orbPrefab = OrbManager.instance.easyOrbPrefab;
        stamina = maxStamina;
    }

    private void OnEnable() {
        Actions.OnEnemyTargetReached += OnEnemyTargetReached;
    }

    private void OnDisable() {
        Actions.OnEnemyTargetReached -= OnEnemyTargetReached;
    }

    private void Update()
    {
        if (gameManager.gameState != GameState.Playing) return;
        Vector3 targetPostition = new Vector3(player.position.x, transform.position.y, player.position.z);
        transform.LookAt(targetPostition);
        transform.GetChild(0).transform.LookAt(targetPostition);
        float cooldownProgress = 2 - (timeToNextOrbSpawn / timeBetweenOrbs);
        currentCooldownBar.transform.localScale = new Vector3(cooldownProgress, currentCooldownBar.transform.localScale.y, currentCooldownBar.transform.localScale.z);


        if (timeToNextOrbSpawn >= timeBetweenOrbs)
        {
            ShootOrb();
            timeToNextOrbSpawn = 0;
            timeBetweenOrbs = GetTimeBetweenOrbs();
            return;
        }
        timeToNextOrbSpawn += Time.deltaTime / (OrbManager.instance.orbs.Count + 1);
    }

    private float GetTimeBetweenOrbs()
    {
        timeBetweenOrbs = Random.Range(10f, 20f);
        return timeBetweenOrbs;
    }

    private void ShootOrb()
    {
        Instantiate(orbPrefab, firePosition.position, transform.rotation);
        animator.SetTrigger("Attack Trigger");
        huhSpawnSound.Play();
    }

    private void OnEnemyTargetReached(OrbMovement _orb, Enemy _enemy)
    {
        if (_enemy == this)
        {
            stamina -= _orb.GetOrbDamage() * 2;
            if (stamina <= 0)
            {
                _orb.DestroyOrbFromEnemy();
                Die();
            }
        }
    }

    public void ReceiveOrb(GameObject _orb)
    {
        OrbMovement orbScript = _orb.GetComponent<OrbMovement>();
    }

    public void Die()
    {
        gameManager.RemoveEnemy(this);
        Destroy(gameObject);
    }
}
