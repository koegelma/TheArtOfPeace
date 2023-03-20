using System.Collections.Generic;
using UnityEngine;

public class OrbManager : MonoBehaviour
{
    public static OrbManager instance;
    public GameObject easyOrbPrefab;
    public GameObject mediumOrbPrefab;
    public GameObject hardOrbPrefab;
    public GameObject destroyOrbPSPrefab;
    public List<OrbMovement> orbs = new List<OrbMovement>();
    public int orbsCreated = 0;

    private void Awake()
    {
        instance = this;
    }

    private void OnEnable()
    {
        Actions.OnPlayerTargetReached += PlayerTargetReached;
        Actions.OnRecognition += PatternRecognized;
        Actions.OnRecognitionFailed += PatternFailed;
    }

    private void OnDisable()
    {
        Actions.OnPlayerTargetReached -= PlayerTargetReached;
        Actions.OnRecognition -= PatternRecognized;
        Actions.OnRecognitionFailed -= PatternFailed;
    }

    public int AddOrb(OrbMovement orb)
    {
        orbs.Add(orb);
        orbsCreated++;
        return orbsCreated;
    }

    public void RemoveOrb(OrbMovement orb)
    {
        Debug.Log("Orb " + orb.name + " removed");
        orbs.Remove(orb);
    }

    public List<OrbMovement> GetAllOrbsDirectedAtPlayer()
    {
        List<OrbMovement> orbsDirectedAtPlayer = new List<OrbMovement>();
        foreach (OrbMovement orb in orbs)
        {
            if (orb.targetIsPlayer) orbsDirectedAtPlayer.Add(orb);
        }
        return orbsDirectedAtPlayer;
    }

    public void SetTargetsToHands()
    {
        List<OrbMovement> orbsDirectedAtPlayer = GetAllOrbsDirectedAtPlayer();

        foreach (OrbMovement orb in orbsDirectedAtPlayer)
        {
            orb.targetIsHands = true;
        }
    }

    public List<OrbMovement> GetAllOrbsDirectedAtHands()
    {
        List<OrbMovement> orbsDirectedAtHands = new List<OrbMovement>();
        foreach (OrbMovement orb in orbs)
        {
            if (orb.targetIsHands) orbsDirectedAtHands.Add(orb);
        }
        return orbsDirectedAtHands;
    }

    private void PatternRecognized(PatternDictionary patternDictionary)
    {
        Enemy closestEnemy = FindClosestEnemy(patternDictionary.averageVelocity, PatternManager.instance.GetDevicePosition(TrackingProfile.RightArm));

        List<OrbMovement> orbsDirectedAtHands = GetAllOrbsDirectedAtHands();

        foreach (OrbMovement orb in orbsDirectedAtHands)
        {
            if (AssertTier(orb, patternDictionary)) orb.SetEnemyTarget(closestEnemy);
            else orb.DestroyOrb();
        }
    }

    private bool AssertTier(OrbMovement _orb, PatternDictionary _patternDictionary)
    {
        bool tierAssert = (int)_orb.tier <= (int)_patternDictionary.Tier;
        Debug.Log("Orb tier: " + _orb.tier + " Pattern tier: " + _patternDictionary.Tier + " Assert: " + tierAssert + "");
        return tierAssert;
    }

    private void PlayerTargetReached(OrbMovement _orb)
    {
        _orb.DestroyOrb();
    }

    private void PatternFailed()
    {
        OrbMovement[] orbsDirectedAtHands = GetAllOrbsDirectedAtHands().ToArray();
        for (int i = 0; i < orbsDirectedAtHands.Length; i++)
        {
            orbsDirectedAtHands[i].DestroyOrb();
        }
    }

    private Enemy FindClosestEnemy(Vector3 _averageVelocity, Vector3 _devicePosition)
    {
        List<Enemy> enemies = GameManager.instance.enemies;
        Enemy closestEnemy = null;
        float minAngle = float.MaxValue;
        Vector3 averageVelocityNormalized = _averageVelocity.normalized;
        foreach (Enemy enemy in enemies)
        {
            Vector3 enemyDirection = (enemy.transform.position - _devicePosition).normalized;
            float angle = Vector3.Angle(averageVelocityNormalized, enemyDirection);
            if (angle < minAngle)
            {
                minAngle = angle;
                closestEnemy = enemy;
            }
        }
        return closestEnemy;
    }

}
