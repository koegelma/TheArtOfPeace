using System.Collections;
using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Handles the interpolation of pattern coordinates and moves the object.
/// </summary>
public class PatternInterpolation : MonoBehaviour
{
    private PatternManager manager;
    private PatternRecognition patternRecognition;
    public PatternDictionary patternDictionary;
    public TrackingProfile profile;
    private int index;
    private Vector3 targetPosition;
    private bool isLerping;
    private bool isInitialized;
    private bool TargetReached { get { return transform.localPosition == targetPosition; } }
    private bool adjustVelocity;
    /// <summary>
    /// Limit the value of the adjusted interpolation duration, if the player's speed is slower than initial speed.
    /// The min interpolation duration will be limited to the initial duration multiplied by this factor. 0.8 means adjustment up to 0.8 times the initial duration.
    /// </summary>
    public float velocityMinFactor;
    /// <summary>
    /// Limit the value of the adjusted interpolation duration, if the player's speed is faster than initial speed.
    /// The max interpolation duration will be limited to the initial duration multiplied by this factor. 1.2 means adjustment up to 1.2 times the initial duration.
    /// </summary>
    public float velocityMaxFactor;

    private Vector3 waistPos;

    [Header("Particle Systems - Velocity")]
    public bool useVelocitySystem;
    public ParticleSystem velocityPS;

    [Header("Particle System - Trail")]
    public bool useTrailSystem;
    public GameObject trailPrefab;
    public float trailTimeAhead;
    private GameObject trail;
    private int trailIndex;
    private int trailIndexAhead;
    private bool trailIsLerping;

    [Header("Particle System - Tolerance")]
    private bool useToleranceSystem;
    public GameObject toleranceHelper;
    public ParticleSystem energyRadiusPS;
    public Material redEmMat;
    public Material greenEmMat;
    private Gradient gradient;
    private GradientColorKey[] colorKey;
    private GradientAlphaKey[] alphaKey;
    private Material material;
    private bool toleranceIsLerping;
    private int toleranceIndex = 0;

    [Header("DTW")]
    public List<Vector3> playerPositions = new List<Vector3>();
    public List<Vector3> patternPositions = new List<Vector3>();

    private void Awake()
    {
        manager = PatternManager.instance;
    }

    private void Start()
    {
        gradient = new Gradient();
        colorKey = new GradientColorKey[2];
        colorKey[0].color = redEmMat.color;
        colorKey[0].time = 0.0f;
        colorKey[1].color = greenEmMat.color;
        colorKey[1].time = 1.0f;

        alphaKey = new GradientAlphaKey[2];
        alphaKey[0].alpha = 1.0f;
        alphaKey[0].time = 0.0f;
        alphaKey[1].alpha = 1.0f;
        alphaKey[1].time = 1.0f;

        gradient.SetKeys(colorKey, alphaKey);

        material = energyRadiusPS.GetComponent<ParticleSystemRenderer>().material;

        UpdateParticleColor(1f);
        if (useVelocitySystem) SetParticleSystem();
    }

    private void Update()
    {
        if (!isInitialized) return;
        if (useToleranceSystem) MoveTolerance();
        MoveHelper();
    }

    /// <summary>
    /// Input pattern and pattern coordinates to setup and initialize interpolation.
    /// </summary>
    public void Initialize(PatternDictionary _patternDictionary, PatternRecognition _patternRecognition)
    {
        patternDictionary = _patternDictionary;
        profile = _patternRecognition.profile;
        patternRecognition = _patternRecognition;
        index = 0;
        targetPosition = patternDictionary.localCoordinatesMap[profile][index];
        isLerping = false;
        adjustVelocity = manager.adjustInterpolationVelocity;
        waistPos = manager.waistDevice.transform.position;

        transform.parent.position = waistPos;

        var ts = patternDictionary.Tolerance * 2;

        transform.localScale = new Vector3(ts, ts, ts);
        if (useVelocitySystem) velocityPS.transform.localScale = new Vector3(ts, ts, ts);
        else velocityPS.gameObject.SetActive(false);
        toleranceIndex = 0;

        /// Tolerance
        toleranceHelper.transform.localScale = new Vector3(ts, ts, ts);
        ParticleSystem.MainModule pMain = energyRadiusPS.main;
        pMain.startSizeMultiplier = ts;

        if (useTrailSystem)
        {
            SetTrailSystem();
            trail.transform.localPosition = targetPosition;
            MoveTrail();
        }

        transform.localPosition = targetPosition;
        transform.parent.eulerAngles = new Vector3(0, manager.waistDevice.transform.eulerAngles.y, 0);

        isInitialized = true;

        ToggleTrailSystem(manager.trailHelper);
        ToggleToleranceSystem(manager.toleranceHelper);
        ToggleVelocitySystem(manager.velocityHelper);

        if (manager.useDTW) GetPatternPositions();
    }

    private void GetPatternPositions()
    {
        patternPositions.Clear();
        for (int i = 0; i < patternDictionary.localCoordinatesMap[profile].Length; i++)
        {
            Vector3 worldPosition = transform.TransformPoint(patternDictionary.localCoordinatesMap[profile][i]);
            patternPositions.Add(worldPosition);
        }
    }

    /// <summary> 
    /// Move pattern helper to next position. 
    /// </summary>
    private void MoveHelper()
    {
        if (TargetReached && !isLerping)
        {
            if (index >= patternDictionary.localCoordinatesMap[profile].Length - 1)
            {
                /// last position reached
                patternRecognition.CompareMovement(this);
                Actions.OnPatternRecognized?.Invoke(profile, patternDictionary);
                isInitialized = false;
                return;
            }

            targetPosition = patternDictionary.localCoordinatesMap[profile][index];
            if (useVelocitySystem) SetParticleVelocity(velocityPS, patternDictionary.localCoordinatesMap[profile][index + 1] - patternDictionary.localCoordinatesMap[profile][index]);
            if (useTrailSystem) MoveTrail();

            index++;

            /// DTW
            if (manager.useDTW) playerPositions.Add(manager.GetDevicePosition(profile));
            patternRecognition.CompareMovement(this);
            Vector3 currVel = manager.GetDeviceVelocity(profile);
            StartCoroutine(LerpPosition(targetPosition, patternDictionary.SamplingRate));
        }
    }

    /// <summary>
    /// Returns the duration of the lerp in seconds based on the velocity of the pattern and the current velocity of the player.
    /// </summary>
    private float GetLerpDuration(Vector3 _currentVelocity, Vector3 _recordedVelocity, float _samplingRate)
    {
        if (!adjustVelocity) return _samplingRate;
        float currentVelocityMagnitude = _currentVelocity.magnitude;
        float recordedVelocityMagnitude = _recordedVelocity.magnitude;
        float velocityRatio = recordedVelocityMagnitude / currentVelocityMagnitude;
        float duration = velocityRatio * _samplingRate;
        float minDuration = velocityMinFactor * _samplingRate;
        float maxDuration = velocityMaxFactor * _samplingRate;
        duration = Mathf.Clamp(duration, minDuration, maxDuration);
        return duration;
    }

    /// <summary>
    /// Linearly interpolate position of transform from current position to target position in given time.
    /// </summary>
    private IEnumerator LerpPosition(Vector3 _targetPosition, float _duration)
    {
        isLerping = true;

        float time = 0;
        float duration = GetLerpDuration(manager.GetDeviceVelocity(profile), patternDictionary.velocitiesMap[profile][index], _duration);
        Vector3 startPosition = transform.localPosition;

        while (time < duration)
        {
            duration = GetLerpDuration(manager.GetDeviceVelocity(profile), patternDictionary.velocitiesMap[profile][index], _duration);
            transform.localPosition = Vector3.Lerp(startPosition, _targetPosition, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = _targetPosition;
        isLerping = false;
    }

    private void SetParticleSystem()
    {
        SetupParticleVelocity(velocityPS);
        velocityPS.gameObject.SetActive(useVelocitySystem);
    }

    private void SetupParticleVelocity(ParticleSystem _ps)
    {
        var velocityOverLifetime = _ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.speedModifier = 50;

        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0, 0);
        curve.AddKey(1, 1);

        ParticleSystem.MinMaxCurve minMaxCurve = new ParticleSystem.MinMaxCurve(1, curve);

        velocityOverLifetime.x = minMaxCurve;
        velocityOverLifetime.y = minMaxCurve;
        velocityOverLifetime.z = minMaxCurve;
    }

    /// <summary>
    /// Sets the particle system velocity to the given vector.
    /// </summary>
    public void SetParticleVelocity(ParticleSystem _ps, Vector3 _velocity)
    {
        var velocityOverLifetime = _ps.velocityOverLifetime;

        velocityOverLifetime.xMultiplier = _velocity.x;
        velocityOverLifetime.yMultiplier = _velocity.y;
        velocityOverLifetime.zMultiplier = _velocity.z;
    }

    private void SetTrailSystem()
    {
        trail = Instantiate(trailPrefab, transform.position, Quaternion.identity);
        trail.transform.SetParent(transform.parent);
        trailIndexAhead = Mathf.RoundToInt(trailTimeAhead / patternDictionary.SamplingRate);
        trailIndex = 0;
        if (trailIndexAhead >= patternDictionary.localCoordinatesMap[profile].Length) trailIndexAhead = patternDictionary.localCoordinatesMap[profile].Length - 1;
        StartCoroutine(MoveTrailToTargetPosition(trailIndexAhead));
    }

    public void MoveTrail()
    {
        if (trailIsLerping) return;
        if (trailIndex >= patternDictionary.localCoordinatesMap[profile].Length) return;

        Vector3 targetPos = patternDictionary.localCoordinatesMap[profile][trailIndex];
        trailIndex++;
        StartCoroutine(LerpTrailPosition(targetPos, patternDictionary.SamplingRate));
    }

    private IEnumerator MoveTrailToTargetPosition(int _targetIndex)
    {
        while (trailIndex < _targetIndex)
        {
            yield return new WaitUntil(() => !trailIsLerping);
            Vector3 targetPos = patternDictionary.localCoordinatesMap[profile][trailIndex];
            trailIndex++;
            StartCoroutine(LerpTrailPosition(targetPos, patternDictionary.SamplingRate / 10));
            yield return null;
        }
        trailIndex = trailIndexAhead;
    }


    private IEnumerator LerpTrailPosition(Vector3 _targetPosition, float _duration)
    {
        trailIsLerping = true;
        float time = 0;
        Vector3 startPosition = trail.transform.localPosition;

        while (time < _duration)
        {
            trail.transform.localPosition = Vector3.Lerp(startPosition, _targetPosition, time / _duration);
            time += Time.deltaTime;
            yield return null;
        }
        trail.transform.localPosition = _targetPosition;
        trailIsLerping = false;
    }

    private void MoveTolerance()
    {
        if (toleranceIsLerping) return;
        if (toleranceIndex >= patternDictionary.localCoordinatesMap[profile].Length) return;
        Vector3 targetPos = patternDictionary.localCoordinatesMap[profile][toleranceIndex];
        toleranceIndex++;
        StartCoroutine(LerpTolerancePosition(targetPos, patternDictionary.SamplingRate));
    }

    /// <summary>
    /// Linearly interpolate position of transform from current position to target position in given time.
    /// </summary>
    private IEnumerator LerpTolerancePosition(Vector3 _targetPosition, float _duration)
    {
        toleranceIsLerping = true;
        float time = 0;
        Vector3 startPosition = toleranceHelper.transform.localPosition;

        while (time < _duration)
        {
            toleranceHelper.transform.localPosition = Vector3.Lerp(startPosition, _targetPosition, time / _duration);
            time += Time.deltaTime;
            yield return null;
        }
        toleranceHelper.transform.localPosition = _targetPosition;
        toleranceIsLerping = false;
    }

    public void UpdateParticleColor(float _distanceOutOfTolerance)
    {
        if (!energyRadiusPS.gameObject.activeSelf) return;
        float t = 1;

        if (_distanceOutOfTolerance > 0)
        {
            t = 1 - _distanceOutOfTolerance / (patternDictionary.Tolerance * 1.5f);
            if (t < 0) t = 0;
        }
        Color color = gradient.Evaluate(t);
        material.color = color;
    }

    public void ToggleToleranceSystem(bool _status)
    {
        useToleranceSystem = _status;
        energyRadiusPS.gameObject.SetActive(_status);
    }

    public void ToggleVelocitySystem(bool _status)
    {
        useVelocitySystem = _status;
        velocityPS.gameObject.SetActive(_status);
    }

    public void ToggleTrailSystem(bool _status)
    {
        useTrailSystem = _status;
        trail.gameObject.SetActive(_status);
    }
}
