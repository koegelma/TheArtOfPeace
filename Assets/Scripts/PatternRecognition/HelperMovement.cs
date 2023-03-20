using UnityEngine;
using System.Collections.Generic;

public class HelperMovement : MonoBehaviour
{
    private PatternManager manager;
    public TrackingProfile profile;
    public PatternHelper helperType;
    private Vector3 uiCoords;
    private void Awake()
    {
        manager = PatternManager.instance;
    }

    private void OnEnable()
    {
        if (helperType == PatternHelper.UI) transform.localPosition = GetCoords();
    }

    private void Update()
    {
        if (helperType == PatternHelper.UI) return;
        if (helperType == PatternHelper.TPose)
        {
            transform.position = GetCoords();
            return;
        }
        transform.parent.position = manager.waistDevice.transform.position;
        transform.parent.eulerAngles = new Vector3(0, manager.waistDevice.transform.eulerAngles.y, 0);
    }

    public void SetProfile(TrackingProfile _profile)
    {
        profile = _profile;
        if (helperType == PatternHelper.TPose) transform.position = GetCoords();
        if (helperType == PatternHelper.UI) transform.localPosition = uiCoords = GetCoords();
        if (helperType == PatternHelper.StartPose) transform.localPosition = GetCoords();

        float ts = manager.startPose.tolerance * 2;
        if (helperType == PatternHelper.TPose) ts *= 2;

        transform.localScale = new Vector3(ts, ts, ts);

        if (transform.GetChild(0).GetComponent<ParticleSystem>())
        {
            ParticleSystem childPs = transform.GetChild(0).GetComponent<ParticleSystem>();
            ParticleSystem.MainModule pMain = childPs.main;
            pMain.startSizeMultiplier = ts;
        }

        if (helperType == PatternHelper.StartPose && !manager.startHelper) transform.GetChild(0).gameObject.SetActive(false);
    }

    /// <summary> 
    /// Get the position of start pose coordinates for this device. 
    /// </summary>
    private Vector3 GetCoords()
    {
        if (helperType == PatternHelper.TPose)
        {
            switch (profile)
            {
                case TrackingProfile.LeftArm:
                    return manager.playerRig.leftHand.rigTarget.position;
                case TrackingProfile.RightArm:
                    return manager.playerRig.rightHand.rigTarget.position;
                case TrackingProfile.LeftLeg:
                    return manager.playerRig.leftFoot.rigTarget.position;
                case TrackingProfile.RightLeg:
                    return manager.playerRig.rightFoot.rigTarget.position;
                default:
                    return Vector3.zero;
            }
        }

        if (helperType == PatternHelper.UI)
        {
            Vector3 leftOffset = new Vector3(-0.4f, manager.startPose.leftArmPatternCoords[0].y, 0.35f);
            Vector3 rightOffset = new Vector3(0.4f, manager.startPose.leftArmPatternCoords[0].y, 0.35f);
            transform.parent.position = manager.waistDevice.transform.position;
            
            switch (profile)
            {
                case TrackingProfile.LeftArm:
                    return leftOffset;
                case TrackingProfile.RightArm:
                    return rightOffset;
                default:
                    return Vector3.zero;
            }
        }

        switch (profile)
        {
            case TrackingProfile.LeftArm:
                return manager.startPose.leftArmPatternCoords[0];
            case TrackingProfile.RightArm:
                return manager.startPose.rightArmPatternCoords[0];
            case TrackingProfile.LeftLeg:
                return manager.startPose.leftLegPatternCoords[0];
            case TrackingProfile.RightLeg:
                return manager.startPose.rightLegPatternCoords[0];
            default:
                return Vector3.zero;
        }
    }
}
