using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;

[System.Serializable]
public class VRMap
{
    public Transform vrTarget;
    public Transform rigTarget;
    public Vector3 trackingPositionOffset;
    public Vector3 trackingRotationOffset;
    /// <summary>
    /// Independent transform that is not affected by the intitial tracker rotation.
    /// </summary>
    private Transform trackingTransform;

    /// <summary>
    /// Calculates the rotational offset between the VR target (tracker) and the rig target (IK constraint).
    /// </summary>
    public void CalculateRotationOffset()
    {
        if (vrTarget == null || rigTarget == null) return;

        Quaternion offset = Quaternion.Inverse(vrTarget.rotation) * rigTarget.rotation;
        trackingRotationOffset = offset.eulerAngles;
    }

    public void SetTrackingTransform()
    {
        if (vrTarget.GetComponent<TrackedInput>())
        {
            trackingTransform = vrTarget.GetComponent<TrackedInput>().trackingTransform;
            return;
        }
        //Debug.Log("TrackedInput not found on " + vrTarget.name + "!");
        trackingTransform = vrTarget;
    }

    /// <summary>
    /// Maps the position and rotation of the VR target (tracker) to the rig target (IK constraint).
    /// </summary>
    public void Map()
    {
        if (vrTarget == null || rigTarget == null) return;
        if (trackingTransform == null) SetTrackingTransform();

        rigTarget.position = trackingTransform.TransformPoint(trackingPositionOffset);
        rigTarget.rotation = vrTarget.rotation * Quaternion.Euler(trackingRotationOffset);
    }
}
public class PlayerRig : MonoBehaviour
{
    [Header("VR Maps")]
    public VRMap head;
    public VRMap spine;
    public VRMap leftHand;
    public VRMap rightHand;
    public VRMap leftFoot;
    public VRMap rightFoot;
    public VRMap waist;
    public Animator leftHandanim;
    public Animator rightHandanim;
    private bool PlayerAvatar { get { return !transform.GetComponent<PatternAnimation>(); } }

    [Header("Constraint Target Transforms")]
    private Transform hipConstraint;
    public Vector3 waistBodyOffset;
    [HideInInspector] public bool offsetCalculated = false;
    public ChainIKConstraint spineConstraint;
    private float spineWeight;

    private float referenceWaistHeight = 0.98196357f;

    private void Awake()
    {
        if (PlayerAvatar)
        {
            if (PatternManager.instance.playerRig != null)
            {
                Debug.LogWarning("More than one PlayerAvatar in scene!");
                return;
            }
            PatternManager.instance.playerRig = this;
        }
    }
    private void Start()
    {
        spineWeight = spineConstraint.weight;
        spineConstraint.weight = 0;
        hipConstraint = waist.rigTarget;
        waistBodyOffset = transform.position - hipConstraint.position;

        ToggleMeshRenderer(!PlayerAvatar);

        if (!PlayerAvatar) CalibrateRig();
    }

    private void OnEnable()
    {
        if (!PlayerAvatar) return;
        Actions.OnCalibrationStarted += CalibrateRig;
    }

    private void OnDisable()
    {
        if (!PlayerAvatar) return;
        Actions.OnCalibrationStarted -= CalibrateRig;
    }

    private void FixedUpdate()
    {
        if (!offsetCalculated)
        {
            if (PlayerAvatar)
            {
                var newPos = waist.vrTarget.position + waistBodyOffset;
                transform.position = new Vector3(newPos.x, transform.position.y, newPos.z);
                var newRotation = new Vector3(transform.eulerAngles.x, waist.vrTarget.transform.eulerAngles.y, transform.eulerAngles.z);
                transform.eulerAngles = newRotation;
            }
            return;
        }

        Map();
    }

    private void Map()
    {
        var newPos = hipConstraint.position + waistBodyOffset;
        transform.position = new Vector3(newPos.x, transform.position.y, newPos.z);

        head.Map();
        spine.Map();
        leftHand.Map();
        rightHand.Map();
        leftFoot.Map();
        rightFoot.Map();
        waist.Map();
    }

    private void CalibrateRig()
    {
        /// set model rotation to match waist rotation
        if (PlayerAvatar)
        {
            var newRotation = new Vector3(transform.eulerAngles.x, waist.vrTarget.transform.eulerAngles.y, transform.eulerAngles.z);
            transform.eulerAngles = newRotation;
        }

        /// calculate rotation offset
        leftHand.CalculateRotationOffset();
        rightHand.CalculateRotationOffset();
        leftFoot.CalculateRotationOffset();
        rightFoot.CalculateRotationOffset();
        waist.CalculateRotationOffset();

        /// calibrate to player size
        ScaleAvatar();
        var scaleFactors = GetScaleFactor();

        // postives offset = down, negatives = up 
        var heightDiference = waist.vrTarget.position.y - referenceWaistHeight;
        var waistOffset = waist.trackingPositionOffset;
        waistOffset.y -= heightDiference;
        waist.trackingPositionOffset = waistOffset;

        Debug.Log("Offset calculated. Waist size difference: " + heightDiference);

        if (PlayerAvatar) ToggleMeshRenderer(true);

        spineConstraint.weight = spineWeight;
        StartCoroutine(GripAnimation(0f, 0.05f));

        Actions.OnPlayerRigReady?.Invoke(!transform.GetComponent<PatternAnimation>(), scaleFactors);
        offsetCalculated = true;
    }

    private float ScaleAvatar()
    {
        var rigDiff = Vector3.Distance(leftHand.rigTarget.position, rightHand.rigTarget.position);
        var vrDiff = Vector3.Distance(leftHand.vrTarget.position - leftHand.trackingPositionOffset, rightHand.vrTarget.position - leftHand.trackingPositionOffset);

        var scaleFactor = rigDiff / vrDiff;
        if (PlayerAvatar) Debug.Log("Scale factor: " + scaleFactor);
        var scale = transform.localScale.x * scaleFactor;
        if (PlayerAvatar) Debug.Log("Rig scale before: " + transform.localScale);
        transform.localScale = new Vector3(scale, scale, scale);
        if (PlayerAvatar) Debug.Log("Rig scale after: " + transform.localScale);
        return scaleFactor;
    }

    private void ToggleMeshRenderer(bool _status)
    {
        SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer renderer in renderers)
        {
            renderer.enabled = _status;
        }
    }

    /// <summary>
    /// Returns a dictionary with the scale factor for each tracking profile to scale the recorded patterns to player size.
    /// </summary>
    private Dictionary<TrackingProfile, float> GetScaleFactor()
    {
        if (!PlayerAvatar) return null;
        List<GameObject> devices = PatternManager.instance.devices;
        var leftArm = devices.Find(x => x.name == "LeftArm Tracker");
        var rightArm = devices.Find(x => x.name == "RightArm Tracker");
        var leftLeg = devices.Find(x => x.name == "LeftLeg Tracker");
        var rightLeg = devices.Find(x => x.name == "RightLeg Tracker");
        var waist = PatternManager.instance.waistDevice;

        var leftArmDist = Vector3.Distance(leftArm.transform.position, waist.transform.position);
        var rightArmDist = Vector3.Distance(rightArm.transform.position, waist.transform.position);
        var leftLegDist = Vector3.Distance(leftLeg.transform.position, waist.transform.position);
        var rightLegDist = Vector3.Distance(rightLeg.transform.position, waist.transform.position);

        var referenceRightArmDist = 0.832f;
        var referenceLeftArmDist = 0.874f;
        var referenceRightLegDist = 0.806f;
        var referenceLeftLegDist = 0.811f;

        var rightArmScale = rightArmDist / referenceRightArmDist;
        var leftArmScale = leftArmDist / referenceLeftArmDist;
        var rightLegScale = rightLegDist / referenceRightLegDist;
        var leftLegScale = leftLegDist / referenceLeftLegDist;

        var scaleFactors = new Dictionary<TrackingProfile, float>();
        scaleFactors.Add(TrackingProfile.RightArm, rightArmScale);
        scaleFactors.Add(TrackingProfile.LeftArm, leftArmScale);
        scaleFactors.Add(TrackingProfile.RightLeg, rightLegScale);
        scaleFactors.Add(TrackingProfile.LeftLeg, leftLegScale);

        return scaleFactors;
    }

    /// <summary>
    /// Animates the grip of the model's hands.
    /// </summary>
    private IEnumerator GripAnimation(float _minValue, float _maxValue)
    {
        float animationSpeed = 0.03f;
        float currentValue = _minValue;
        while (gameObject.activeSelf)
        {
            if (currentValue >= _maxValue)
            {
                animationSpeed = -animationSpeed;
            }
            else if (currentValue <= _minValue)
            {
                animationSpeed = Mathf.Abs(animationSpeed);
            }

            currentValue += animationSpeed * Time.deltaTime;
            leftHandanim.SetFloat("Grip", currentValue);
            rightHandanim.SetFloat("Grip", currentValue);
            yield return null;
        }
    }

}
