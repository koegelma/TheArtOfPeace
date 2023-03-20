using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbGravity : MonoBehaviour
{

    private OrbManager orbManager;
    public int pull;
    public int range;
    private OrbMovement orbMovement;
    private bool isMerging = false;

    private void Start()
    {
        orbManager = OrbManager.instance;
        orbMovement = GetComponent<OrbMovement>();
    }


    private void Update()
    {
        ApplyGravity();
    }
    public void ApplyGravity()
    {
        if (orbMovement.tier != Tier.Hard)
        {
            List<OrbMovement> tempOrbList = new List<OrbMovement>();
            foreach (OrbMovement orb in orbManager.orbs)
            {
                Vector3 vectorFromOrbToThis = orb.transform.position - transform.position;
                if ((name != orb.name && vectorFromOrbToThis.magnitude < range) && orb.GetComponent<OrbMovement>().target == orbMovement.target)
                {
                    tempOrbList.Add(orb);
                }
            }
            Vector3 sumGravityVector = new Vector3(0, 0, 0);
            foreach (OrbMovement gravOrb in tempOrbList)
            {
                sumGravityVector += (gravOrb.transform.position - transform.position);
            }
            if (tempOrbList.Count > 0)
            {
                sumGravityVector /= tempOrbList.Count;
            }
            GetComponent<Rigidbody>().AddForce(sumGravityVector * pull);
            CheckGroups(tempOrbList);
        }
    }

    public void CheckGroups(List<OrbMovement> tempOrbList)
    {
        if (orbMovement.tier == Tier.Hard) return;

        List<OrbMovement> mergeList = new List<OrbMovement>();
        foreach (OrbMovement orb in tempOrbList)
        {
            if (((orb.transform.position - transform.position).magnitude < 0.7) && orb.transform.parent == null && orb.name != name && orb.GetComponent<OrbMovement>().tier == Tier.Easy)
            {
                mergeList.Add(orb);
            }
        }
        if (mergeList.Count > 0)
        {
            isMerging = true;
            GameObject newOrb = null;
            if (mergeList.Count == 1)
            {
                orbManager.RemoveOrb(gameObject.GetComponent<OrbMovement>());
                StartCoroutine(DestroyThisOrb());
                orbManager.RemoveOrb(mergeList[0]);
                Destroy(mergeList[0].gameObject);

                if (orbMovement.tier == Tier.Easy)
                {
                    newOrb = Instantiate(orbManager.mediumOrbPrefab, transform.position, transform.rotation);
                }
                else
                {
                    newOrb = Instantiate(orbManager.hardOrbPrefab, transform.position, transform.rotation);
                }
            }
            else
            {
                if (orbMovement.tier == Tier.Easy)
                {
                    newOrb = Instantiate(orbManager.hardOrbPrefab, transform.position, transform.rotation);
                    orbManager.RemoveOrb(gameObject.GetComponent<OrbMovement>());
                    StartCoroutine(DestroyThisOrb());
                    orbManager.RemoveOrb(mergeList[0]);
                    Destroy(mergeList[0].gameObject);
                    orbManager.RemoveOrb(mergeList[1]);
                    Destroy(mergeList[1].gameObject);
                }
                else if (orbMovement.tier == Tier.Medium)
                {
                    newOrb = Instantiate(orbManager.hardOrbPrefab, transform.position, transform.rotation);
                    orbManager.RemoveOrb(gameObject.GetComponent<OrbMovement>());
                    StartCoroutine(DestroyThisOrb());
                    orbManager.RemoveOrb(mergeList[0]);
                    Destroy(mergeList[0].gameObject);
                }
            }
            MergeMovement(orbMovement, newOrb.GetComponent<OrbMovement>());
        }

    }

    private IEnumerator DestroyThisOrb()
    {
        yield return new WaitUntil(() => isMerging == false);
        Destroy(gameObject);
    }

    private void MergeMovement(OrbMovement original, OrbMovement merged)
    {
        if (merged != null)
        {
            merged.target = original.target;
            merged.targetIsHands = original.targetIsHands;
            merged.isMerged = true;
            isMerging = false;
        }
    }
}
