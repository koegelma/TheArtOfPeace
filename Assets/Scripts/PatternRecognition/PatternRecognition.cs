using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PatternRecognition : MonoBehaviour
{
    [Header("General Setup")]
    private PatternManager manager;
    public TrackingProfile profile;

    [Header("Interpolation")]
    public GameObject patternHelperPrefab;
    [HideInInspector] public List<GameObject> patternHelpers;
    private bool recognitionActive = true;
    private object patternHelpersLock = new object();

    [Header("T-Pose Check")]
    public GameObject tPoseHelperPrefab;
    private GameObject tPoseHelper;
    private bool inTPose = false;

    [Header("Start-Pose Check")]
    public GameObject startPoseHelperPrefab;
    [HideInInspector] public GameObject startPatternHelper;
    private bool inStartPosition = false;
    private Coroutine startPoseCoroutine = null;

    private void Awake()
    {
        manager = PatternManager.instance;
    }

    private void Start()
    {
        if (manager.gameObject.GetComponent<PatternRecording>())
        {
            InstantiateStartHelper();
            return;
        }
    }

    private void OnEnable()
    {
        Actions.OnCalibrationStarted += DestroyTPoseHelper;
        Actions.OnRecordingStarted += DeactivateStartHelper;
        Actions.OnThresholdCrossed += RemovePatternHelper;
        Actions.OnRecognitionReset += ResetRecognition;

        Actions.OnPlayerTargetSet += ActivateStartHelper;
        Actions.OnPlayerTargetReached += OnPlayerTargetReached;
    }

    private void OnDisable()
    {
        Actions.OnCalibrationStarted -= DestroyTPoseHelper;
        Actions.OnRecordingStarted -= DeactivateStartHelper;
        Actions.OnThresholdCrossed -= RemovePatternHelper;
        Actions.OnRecognitionReset -= ResetRecognition;

        Actions.OnPlayerTargetSet -= ActivateStartHelper;
        Actions.OnPlayerTargetReached -= OnPlayerTargetReached;
    }

    private void Update()
    {
        if (manager.recognitionActive) return;
        if (tPoseHelper != null)
        {
            CheckForTPose();
            return;
        }
        if (startPatternHelper == null) return;
        CheckForStartPose();
    }

    /// <summary>
    /// Instantiates T-Pose helper for calibration.
    /// </summary>
    public void InstantiateTPoseHelper()
    {
        tPoseHelper = Instantiate(tPoseHelperPrefab, manager.waistDevice.transform.position, Quaternion.identity);
        HelperMovement helperMovement = tPoseHelper.GetComponentInChildren<HelperMovement>();
        helperMovement.SetProfile(profile);
    }

    /// <summary>
    /// Checks if device position is within tolerance of T-Pose position and sends status to PatternManager.
    /// </summary>
    private void CheckForTPose()
    {
        if (DistanceOutOfToleranceRange(tPoseHelper.transform.GetChild(0).position, manager.startPose.tolerance * 2) <= 0)
        {
            if (!inTPose)
            {
                inTPose = true;
                tPoseHelper.transform.GetChild(0).GetChild(0).gameObject.SetActive(false);
                Actions.OnTPosePositionReached?.Invoke(profile, inTPose);
            }
            return;
        }

        if (inTPose)
        {
            inTPose = false;
            tPoseHelper.transform.GetChild(0).GetChild(0).gameObject.SetActive(true);
            Actions.OnTPosePositionReached?.Invoke(profile, inTPose);
        }
    }

    /// <summary>
    /// Destroys T-Pose helper after calibration.
    /// </summary>
    private void DestroyTPoseHelper()
    {
        Destroy(tPoseHelper);
    }

    /// <summary>
    /// Instantiates start helper for pattern recognition start pose.
    /// </summary> 
    public void InstantiateStartHelper()
    {
        recognitionActive = true;
        startPatternHelper = Instantiate(startPoseHelperPrefab, manager.waistDevice.transform.position, Quaternion.identity);
        HelperMovement helperMovement = startPatternHelper.GetComponentInChildren<HelperMovement>();
        helperMovement.SetProfile(profile);
    }

    /// <summary> 
    /// Checks if device position is within tolerance of start position and sends status to PatternManager. 
    /// </summary>
    private void CheckForStartPose()
    {
        if (!startPatternHelper.activeSelf) return;
        if (DistanceOutOfToleranceRange(startPatternHelper.transform.GetChild(0).position, manager.startPose.tolerance) <= 0)
        {
            if (!inStartPosition)
            {
                inStartPosition = true;
                startPatternHelper.transform.GetChild(0).GetChild(0).gameObject.SetActive(false);
                Actions.OnStartPositionReached?.Invoke(profile, inStartPosition);
            }
            return;
        }

        if (inStartPosition)
        {
            inStartPosition = false;
            startPatternHelper.transform.GetChild(0).GetChild(0).gameObject.SetActive(true);
            Actions.OnStartPositionReached?.Invoke(profile, inStartPosition);
        }
    }

    /// <summary>
    /// Instantiates pattern helpers for each pattern in devicePatternCoords.
    /// </summary>
    public void InstantiatePatternHelpers()
    {
        for (int i = 0; i < manager.patternDictionaries.Count; i++)
        {
            if (manager.patternDictionaries[i].localCoordinatesMap[profile].Length > 0)
            {
                GameObject newPatternHelper = Instantiate(patternHelperPrefab, Vector3.zero, Quaternion.identity);
                patternHelpers.Add(newPatternHelper);
                newPatternHelper.GetComponentInChildren<PatternInterpolation>().Initialize(manager.patternDictionaries[i], this);
            }
            else Debug.Log("No pattern coords: " + profile + " " + manager.patternDictionaries[i].Name);
        }
        startPatternHelper.transform.GetChild(0).GetChild(0).gameObject.SetActive(true);
        startPatternHelper.SetActive(false);
    }

    /// <summary>
    /// Compares player movement to pattern movement by calculating the Euclidean difference and DTW difference between the two.
    /// </summary>
    public void CompareMovement(PatternInterpolation _patternInterpolation)
    {
        CompareEuclidean(_patternInterpolation);
        if (manager.useDTW) CompareDTW(_patternInterpolation.patternDictionary, _patternInterpolation.playerPositions.ToArray(), _patternInterpolation.patternPositions.ToArray());
    }

    /// <summary>
    /// Updates the Euclidean difference between the device world space position and the pattern world space position.
    /// </summary>
    private void CompareEuclidean(PatternInterpolation _patternInterpolation)
    {
        float distance = DistanceOutOfToleranceRange(_patternInterpolation.transform.position, _patternInterpolation.patternDictionary.Tolerance);
        if (distance < 0) distance = 0;
        _patternInterpolation.UpdateParticleColor(distance);
        _patternInterpolation.patternDictionary.euclideanMap[profile] += distance;
        Actions.OnEuclideanDifferenceChanged?.Invoke(_patternInterpolation.patternDictionary);
    }

    /// <summary>
    /// Returns the distance between the device world space position and the tolerance range (negative if inside tolerance).
    /// </summary>
    public float DistanceOutOfToleranceRange(Vector3 _patternPosition, float _tolerance)
    {
        float distance = Vector3.Distance(_patternPosition, transform.position);
        distance -= _tolerance;
        return distance;
    }

    /// <summary>
    /// Updates the DTW difference between the device world space positions and the pattern world space positions.
    /// </summary>
    private void CompareDTW(PatternDictionary _patternDictionary, Vector3[] _playerPositions, Vector3[] _patternPositions)
    {
        float dtw = CalculateDTW(_playerPositions, _patternPositions);
        _patternDictionary.dtwMap[profile] = dtw;
        _patternDictionary.playerPositionsMap[profile] = _playerPositions;
        if (!manager.isGameScene || _playerPositions.Length < (0.75f * _patternPositions.Length)) return;

        float time = 0.25f;
        float averageVelocity = (manager.GetAverageVelocity(TrackingProfile.LeftArm, time).magnitude +
            manager.GetAverageVelocity(TrackingProfile.RightArm, time).magnitude + manager.GetAverageVelocity(TrackingProfile.LeftLeg, time).magnitude +
            manager.GetAverageVelocity(TrackingProfile.RightLeg, time).magnitude) / 4;
        if (averageVelocity < 0.1f) Actions.OnPlayerMovementStopped?.Invoke();
    }

    /// <summary>
    /// Calculates the Dynamic Time Warping between two Vector3 arrays and returns the minimum distance for the optimal path.
    /// </summary>
    private float CalculateDTW(Vector3[] _playerPositions, Vector3[] _patternPositions)
    {
        int m = _playerPositions.Length;
        int n = _patternPositions.Length;

        float[,] dtw = new float[m, n];

        for (int i = 0; i < m; i++)
        {
            for (int j = 0; j < n; j++)
            {
                dtw[i, j] = Vector3.Distance(_playerPositions[i], _patternPositions[j]);
            }
        }
        for (int i = 1; i < m; i++)
        {
            for (int j = 1; j < n; j++)
            {
                dtw[i, j] += Mathf.Min(dtw[i - 1, j], Mathf.Min(dtw[i, j - 1], dtw[i - 1, j - 1]));
            }
        }
        return dtw[m - 1, n - 1];
    }

    /// <summary>
    /// Removes the pattern helper from the patternHelpers list and destroys the patternHelper gameobject.
    /// </summary>
    private void RemovePatternHelper(PatternDictionary _patternDictionary)
    {
        //Debug.Log("RemovePatternHelper: " + _patternDictionary.Name + " in " + profile + "");
        lock (patternHelpersLock)
        {
            foreach (GameObject patternHelper in patternHelpers.ToArray())
            {
                if (patternHelper.GetComponentInChildren<PatternInterpolation>().patternDictionary == _patternDictionary)
                {
                    patternHelpers.Remove(patternHelper);
                    Destroy(patternHelper);
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Activate the pattern recognition script. (Only called from InteractiveUI - assigned in editor)
    /// </summary>
    public void ActivateRecognition()
    {
        recognitionActive = true;
        startPatternHelper.SetActive(true);
        startPatternHelper.transform.GetChild(0).GetChild(0).gameObject.SetActive(true);
        inStartPosition = false;
    }

    /// <summary>
    /// Deactivate the pattern recognition script. (Only called from InteractiveUI - assigned in editor)
    /// </summary>
    public void DeactivateRecognition()
    {
        recognitionActive = false;
        startPatternHelper.SetActive(false);
    }

    /// <summary>
    /// Reset the pattern recognition script to initial state.
    /// </summary>
    private void ResetRecognition()
    {
        lock (patternHelpersLock)
        {
            if (patternHelpers.Count != 0)
            {
                foreach (GameObject patternHelper in patternHelpers)
                {
                    Destroy(patternHelper);
                }
                patternHelpers.Clear();
            }
        }
        inStartPosition = false;
        if (manager.isGameScene || startPoseCoroutine != null) return;
        startPoseCoroutine = StartCoroutine(ReactivateStartHelper());
    }

    private void ActivateStartHelper()
    {
        if (startPatternHelper == null) { InstantiateStartHelper(); return; }
        if (startPatternHelper.activeSelf || manager.recognitionActive) return;
        startPatternHelper.SetActive(true);
    }

    private IEnumerator ReactivateStartHelper()
    {
        if (startPatternHelper.activeSelf) yield break;
        yield return new WaitForSeconds(1f);
        if (patternHelpers.Count != 0) yield break;
        if (startPatternHelper.activeSelf) yield break;
        if (recognitionActive)
        {
            startPatternHelper.SetActive(true);
            startPatternHelper.transform.GetChild(0).GetChild(0).gameObject.SetActive(true);
        }
        startPoseCoroutine = null;
    }

    private void DeactivateStartHelper(InputAction.CallbackContext _context)
    {
        startPatternHelper.SetActive(false);
    }

    private void OnPlayerTargetReached(OrbMovement _orbMovement)
    {
        if (OrbManager.instance.GetAllOrbsDirectedAtPlayer().Count > 1) return;
        DeactivateStartHelper(new InputAction.CallbackContext());
    }
}
