using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatternAnimation : MonoBehaviour
{
    private VRMap leftHand;
    private VRMap rightHand;
    private VRMap leftFoot;
    private VRMap rightFoot;
    private VRMap waist;
    public string patternName; // pattern to animate
    private PatternManager manager;
    private PlayerRig playerRig;
    private bool isInitialized = false;

    public GameObject patternHelper;

    private void Awake()
    {
        manager = PatternManager.instance;
        playerRig = GetComponent<PlayerRig>();
        leftHand = playerRig.leftHand;
        rightHand = playerRig.rightHand;
        leftFoot = playerRig.leftFoot;
        rightFoot = playerRig.rightFoot;
        waist = playerRig.waist;
    }

    private void Start()
    {
        ToggleMeshRenderer(false);
    }

    private void OnEnable()
    {
        Actions.OnPlayerRigReady += Initialize;
    }

    private void OnDisable()
    {
        Actions.OnPlayerRigReady -= Initialize;
    }

    private IEnumerator WaitForMeshRendererToggle()
    {
        yield return new WaitForSeconds(1f);
        ToggleMeshRenderer(true);
    }

    private void ToggleMeshRenderer(bool _status)
    {
        SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer renderer in renderers)
        {
            renderer.enabled = _status;
        }
    }

    private void Initialize(bool _playerAvatar, Dictionary<TrackingProfile, float> _scaleFactors)
    {
        if (_playerAvatar || isInitialized) return;
        StartCoroutine(WaitForInit());
    }

    private IEnumerator WaitForInit()
    {
        yield return new WaitForSeconds(0.5f);

        if (!AssertPatternDictionary(manager.patternName, out PatternDictionary animatedPattern))
        {
            if (!AssertPatternDictionary(patternName, out animatedPattern))
            {
                Debug.Log("No Pattern with name '" + patternName + "' found, Pattern Animation cancelled!");
                yield break;
            }
        }
        Debug.Log("Pattern Animation started: " + animatedPattern.Name);

        InstantiatePatternHelper(TrackingProfile.LeftArm, leftHand, animatedPattern, out RigPatternInterpolation leftHandRI);
        InstantiatePatternHelper(TrackingProfile.RightArm, rightHand, animatedPattern, out RigPatternInterpolation rightHandRI);
        InstantiatePatternHelper(TrackingProfile.LeftLeg, leftFoot, animatedPattern, out RigPatternInterpolation leftFootRI);
        InstantiatePatternHelper(TrackingProfile.RightLeg, rightFoot, animatedPattern, out RigPatternInterpolation rightFootRI);
        InstantiatePatternHelper(TrackingProfile.Waist, waist, animatedPattern, out RigPatternInterpolation waistRI);
        if (animatedPattern.worldCoordinatesMap.ContainsKey(TrackingProfile.Head) && animatedPattern.worldCoordinatesMap[TrackingProfile.Head].Length > 0)
        {
            Debug.Log("Head found");
            InstantiatePatternHelper(TrackingProfile.Head, playerRig.head, animatedPattern, out RigPatternInterpolation headRI);
        }
        else
        {
            playerRig.head.vrTarget = null;
            playerRig.spine.vrTarget = null;
        }
        StartCoroutine(WaitForMeshRendererToggle());
        isInitialized = true;
    }

    /// <summary>
    /// Asserts if a PatternDictionary with the given name exists in the PatternManager and returns it if true.
    /// </summary>
    private bool AssertPatternDictionary(string _patternName, out PatternDictionary _patternDictionary)
    {
        return (_patternDictionary = manager.patternDictionaries.Find(x => x.Name == _patternName)) != null;
    }

    /// <summary>
    /// Instantiates a PatternHelper and initializes it.
    /// </summary>
    private void InstantiatePatternHelper(TrackingProfile _profile, VRMap _vRMap, PatternDictionary _patternDict, out RigPatternInterpolation _rigPatternInterpolation)
    {
        GameObject newPatternHelper = Instantiate(patternHelper, Vector3.zero, Quaternion.identity);
        _rigPatternInterpolation = newPatternHelper.GetComponent<RigPatternInterpolation>();
        _rigPatternInterpolation.Initialize(_profile, _vRMap.vrTarget, _patternDict, GetPosOffset(_vRMap.vrTarget.position, _patternDict.worldCoordinatesMap[_profile][0]), GetRotOffset(_vRMap.vrTarget.rotation, _patternDict.rotationsMap[_profile][0]), transform.parent);
    }

    /// <summary>
    /// Returns the offset between the VR device position and the recorded Pattern position.
    /// </summary>
    private Vector3 GetPosOffset(Vector3 _vrPos, Vector3 _patternPos)
    {
        return _vrPos - _patternPos;
    }

    /// <summary>
    /// Returns the offset between the VR device rotation and the recorded Pattern rotation.
    /// </summary>
    private Vector3 GetRotOffset(Quaternion _vrRot, Quaternion _patternRot)
    {
        Quaternion offset = Quaternion.Inverse(_patternRot) * _vrRot;
        return offset.eulerAngles;
    }
}
