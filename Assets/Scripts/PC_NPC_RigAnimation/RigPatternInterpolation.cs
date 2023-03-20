using System.Collections;
using UnityEngine;

public class RigPatternInterpolation : MonoBehaviour
{
    private PatternManager manager;
    private TrackingProfile profile;
    private Transform target;
    private Transform waistTarget;
    private PatternDictionary patternDictionary;
    private Vector3[] patternCoords;
    private Quaternion[] patternRotations;
    private int index;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Vector3 rotationOffset; // rig
    private bool TargetReached { get { return transform.position == targetPosition; } }
    private bool isLerping;
    public bool isInitialized;
    private Transform targetTransform;
    private Vector3 startPosOffset;
    private Vector3 startRotOffset;

    private void Awake()
    {
        manager = PatternManager.instance;
    }

    private void Update()
    {
        if (!isInitialized) return;
        MoveHelper();
        UpdateRigTarget(targetTransform);
    }

    public void Initialize(TrackingProfile _profile, Transform _target, PatternDictionary _patternDictionary, Vector3 _posOffset, Vector3 _rotOffset, Transform _transform)
    {
        index = 0;
        profile = _profile;
        patternDictionary = _patternDictionary;
        patternCoords = patternDictionary.worldCoordinatesMap[profile];
        patternRotations = patternDictionary.rotationsMap[profile];
        target = _target;

        startPosOffset = _posOffset;
        startRotOffset = _rotOffset;

        SetTargetPosition();

        SetTargetRotation();

        CalculateRotationOffset();
        transform.position = targetPosition;
        transform.rotation = targetRotation;

        targetTransform = _transform;

        isLerping = false;
        isInitialized = true;
    }


    /// <summary> 
    /// Move pattern helper to next position. 
    /// </summary>
    private void MoveHelper()
    {
        if (TargetReached && !isLerping)
        {
            if (index >= patternCoords.Length - 1)
            {
                index = 0;
            }
            SetTargetPosition();
            SetTargetRotation();
            index++;
            StartCoroutine(LerpPosAndRot(targetPosition, targetRotation, patternDictionary.SamplingRate));
        }
    }

    private IEnumerator LerpPosAndRot(Vector3 _targetPosition, Quaternion _targetRotation, float _duration)
    {
        isLerping = true;

        float time = 0;
        Vector3 startPosition = transform.localPosition;
        Quaternion startRotation = transform.rotation;

        while (time < _duration)
        {
            transform.position = Vector3.Lerp(startPosition, _targetPosition, time / _duration);
            transform.rotation = Quaternion.Lerp(startRotation, _targetRotation, time / _duration);
            time += Time.deltaTime;
            yield return null;
        }

        transform.position = _targetPosition;
        transform.rotation = _targetRotation;
        isLerping = false;
    }

    private void UpdateRigTarget(Transform _transform)
    {
        target.position = _transform.TransformPoint(transform.localPosition);
        target.rotation = transform.rotation * Quaternion.Euler(rotationOffset);
    }

    private void SetTargetPosition()
    {
        targetPosition = patternCoords[index];
    }

    private void SetTargetRotation()
    {
        targetRotation = patternRotations[index];
    }

    public void CalculateRotationOffset()
    {
        Quaternion offset = Quaternion.Inverse(patternDictionary.rotationOffsetsMap[profile]) * target.rotation;
        rotationOffset = offset.eulerAngles;
    }
}
