using System.Collections;
using UnityEngine;

public class OrbMovement : MonoBehaviour
{
    private OrbManager orbManager;
    private GameManager gameManager;
    private PatternManager patternManager;

    [Header("Orb Setup")]
    private float speed = 2f;
    private float newSpeed;
    private float t;
    private float slowSpeed = 1f;
    private float mediumSpeed = 2f;
    private float fastSpeed = 4f;
    private float rotateSpeed = 300f;
    private int orbDamage;
    private Rigidbody rb;
    public Tier tier;
    public bool targetIsHands = false;
    [HideInInspector] public bool isMerged = false;

    [Header("Target Setup")]
    private Transform playerTarget;
    private Enemy enemyTarget;
    public Transform target;
    public bool hasTarget { get { return target != null; } }
    public bool targetIsPlayer { get { return target == playerTarget; } }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        orbManager = OrbManager.instance;
        gameObject.name = "Orb" + orbManager.AddOrb(this);

        gameManager = GameManager.instance;
        patternManager = PatternManager.instance;

        playerTarget = PatternManager.instance.playerRig.waist.rigTarget.transform;
        GetOrbDamage();
        SetPlayerTarget();
    }

    private void FixedUpdate()
    {
        if (gameManager.gameState != GameState.Playing) return;

        if (targetIsHands)
        {
            FollowHands();
            return;
        }

        if (!hasTarget) DestroyOrb();

        CheckSpeed();
        UpdateSpeed();
        Translate();
    }

    public float GetOrbDamage()
    {
        switch (tier)
        {
            case Tier.Easy:
                orbDamage = 10;
                break;
            case Tier.Medium:
                orbDamage = 20;
                break;
            case Tier.Hard:
                orbDamage = 30;
                break;
        }
        return orbDamage;
    }

    private void CheckSpeed()
    {
        if (targetIsPlayer)
        {
            if (newSpeed != mediumSpeed)
            {
                newSpeed = mediumSpeed;
                t = 0;
            }
            return;
        }

        if (newSpeed != fastSpeed)
        {
            newSpeed = fastSpeed;
            t = 0;
        }
    }

    private void UpdateSpeed()
    {
        speed = Mathf.Lerp(speed, newSpeed, t);
        t += 0.5f;
    }

    private void Translate()
    {
        Vector3 direction = target.position - rb.position;
        direction.Normalize();
        Vector3 rotateAmount = Vector3.Cross(direction, transform.forward);
        rb.angularVelocity = -rotateAmount * rotateSpeed;
        rb.velocity = transform.forward * speed;

        if (GetDistanceToTarget() <= 0.1f) GetNextTarget();
    }

    private float GetDistanceToTarget()
    {
        float distance = Vector3.Distance(rb.position, target.position);
        return distance;
    }

    private void GetNextTarget()
    {
        if (targetIsPlayer) Actions.OnPlayerTargetReached?.Invoke(this);
        else
        {
            SetPlayerTarget();
            Actions.OnEnemyTargetReached?.Invoke(this, enemyTarget);
        }
    }

    private void SetPlayerTarget()
    {
        target = playerTarget;
        Actions.OnPlayerTargetSet?.Invoke();
    }

    public void SetEnemyTarget(Enemy enemy)
    {
        targetIsHands = false;
        target = enemy.firePosition;
        enemyTarget = enemy;
    }

    private void FollowHands()
    {
        Vector3 handPosition = (patternManager.GetDevicePosition(TrackingProfile.LeftArm) + patternManager.GetDevicePosition(TrackingProfile.RightArm)) / 2;
        Vector3 offset = patternManager.playerRig.transform.forward * 0.5f;
        Vector3 finalPosition = handPosition + offset;
        float distance = Vector3.Distance(rb.position, finalPosition);

        if (distance > 2 && newSpeed != 4)
        {
            newSpeed = 4;
            t = 0;
        }
        if (distance < 2 && distance > 0.5 && newSpeed != 2)
        {
            newSpeed = 2;
            t = 0;
        }
        if (distance < 0.5 && distance > 0.2 && newSpeed != 0.25f)
        {
            newSpeed = 0.25f;
            t = 0;
        }
        if (distance < 0.2 && newSpeed != 0.05f)
        {
            newSpeed = 0.05f;
            t = 0;
        }

        UpdateSpeed();

        Vector3 direction = finalPosition - rb.position;
        direction.Normalize();
        Vector3 rotateAmount = Vector3.Cross(direction, transform.forward);
        rb.angularVelocity = -rotateAmount * rotateSpeed;
        rb.velocity = transform.forward * speed;
    }

    public void DestroyOrb()
    {
        Actions.OnPlayerTakeDamage?.Invoke(orbDamage);
        Instantiate(orbManager.destroyOrbPSPrefab, transform.position, transform.rotation);
        AudioManager.instance.Play("PlayerDamage");
        Debug.Log("Orb " + gameObject.name + " destroyed");
        orbManager.RemoveOrb(this);
        Destroy(gameObject);
    }

    public void DestroyOrbFromEnemy()
    {
        Instantiate(orbManager.destroyOrbPSPrefab, transform.position, transform.rotation);
        orbManager.RemoveOrb(this);
        Destroy(gameObject);
    }

    private IEnumerator DestroyOrbAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        DestroyOrb();
    }
}
