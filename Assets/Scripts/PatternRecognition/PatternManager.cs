using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using UnityEngine.InputSystem;


[System.Serializable]
public class SerializableList<T>
{
    public List<T> list;
}

public class PatternManager : MonoBehaviour
{
    [Header("Setup")]
    public static PatternManager instance;
    public List<GameObject> devices;
    public GameObject waistDevice;
    [HideInInspector] public string saveFile;
    private AudioManager audioManager;

    [Header("Patterns")]
    public List<Pattern> patterns;
    public List<PatternDictionary> patternDictionaries;
    /// <summary>
    /// Load only this pattern, if empty load all patterns.
    /// </summary>
    public string patternName;
    public Pattern startPose;

    [Header("Recognition")]
    public bool isGameScene = false;
    public bool useDTW;
    public bool adjustInterpolationVelocity;
    public bool startHelper = true;
    public bool toleranceHelper;
    public bool velocityHelper;
    public bool trailHelper;
    private float timeStamp;
    [HideInInspector] public bool recognitionActive;
    [HideInInspector] public Dictionary<TrackingProfile, bool> startPositionsReached;
    [HideInInspector] public Dictionary<TrackingProfile, bool> tPosePositionsReached;
    [HideInInspector] public float lowestEuclideanDifference;
    [HideInInspector] public PatternDictionary patternWithLowestEuclideanDifference;
    [HideInInspector] public float lowestDTWDifference;
    [HideInInspector] public PatternDictionary patternWithLowestDTWDifference;
    [HideInInspector] public Dictionary<TrackingProfile, PatternDictionary> recognizedPattern;

    // [Header("Other")]
    private Coroutine playerCalibrationCoroutine = null;
    [HideInInspector] public PlayerRig playerRig = null;


    private void Awake()
    {
        instance = this;
        string filePath = Path.Combine(Application.dataPath, "Resources/patterns.json");
        saveFile = filePath;
        patterns = LoadFile(); // change to LoadFromAssets() before building the application
    }

    private void Start()
    {
        InitializePatternDictionaries();
        Debug.Log("Pattern dictionaries initialized: " + patternDictionaries.Count);
        InitializeTrackerDictionaries();
        recognitionActive = false;
        audioManager = AudioManager.instance;
    }

    private void OnEnable()
    {
        Actions.OnTPosePositionReached += UpdateTPosePositionStatus;
        Actions.OnPlayerRigReady += ScalePatterns;
        Actions.OnStartPositionReached += UpdateStartPositionStatus;
        Actions.OnEuclideanDifferenceChanged += UpdateEuclideanDifference;
        Actions.OnDTWDifferenceChanged += UpdateDTWDifference;
        Actions.OnPatternRecognized += PatternRecognized;
        Actions.OnPlayerMovementStopped += OnPlayerMovementStopped;
    }

    private void OnDisable()
    {
        Actions.OnTPosePositionReached -= UpdateTPosePositionStatus;
        Actions.OnPlayerRigReady += ScalePatterns;
        Actions.OnStartPositionReached -= UpdateStartPositionStatus;
        Actions.OnEuclideanDifferenceChanged -= UpdateEuclideanDifference;
        Actions.OnDTWDifferenceChanged -= UpdateDTWDifference;
        Actions.OnPatternRecognized -= PatternRecognized;
        Actions.OnPlayerMovementStopped -= OnPlayerMovementStopped;
    }

    /// <summary>
    /// Return GameObject of device with given profile.
    /// </summary>
    public GameObject GetDevice(TrackingProfile _profile)
    {
        foreach (GameObject device in devices)
        {
            if (device.GetComponent<PatternRecognition>().profile == _profile)
            {
                return device;
            }
        }
        Debug.LogError("No device with profile " + _profile + " found. Cannot return device.");
        return null;
    }

    /// <summary>
    /// Return position of device with given profile.
    /// </summary>
    public Vector3 GetDevicePosition(TrackingProfile _profile)
    {
        foreach (GameObject device in devices)
        {
            if (device.GetComponent<PatternRecognition>().profile == _profile)
            {
                return device.GetComponent<TrackedInput>().position;
            }
        }
        Debug.LogError("No device with profile " + _profile + " found. Cannot return position.");
        return Vector3.zero;
    }

    /// <summary>
    /// Return current velocity of device with given profile.
    /// </summary>
    public Vector3 GetDeviceVelocity(TrackingProfile _profile)
    {
        foreach (GameObject device in devices)
        {
            if (device.GetComponent<PatternRecognition>().profile == _profile)
            {
                return device.GetComponent<TrackedInput>().velocity;
            }
        }
        Debug.LogError("No device with profile " + _profile + " found. Cannot return velocity.");
        return Vector3.zero;
    }

    /// <summary>
    /// Activate velocity average counting for device with given profile.
    /// </summary>
    private void ActivateVelocityAverage(TrackingProfile _profile)
    {
        foreach (GameObject device in devices)
        {
            if (device.GetComponent<PatternRecognition>().profile == _profile)
            {
                device.GetComponent<TrackedInput>().ActivateVelocityAverage(10f);
                return;
            }
        }
    }

    /// <summary>
    /// Return average velocity over time of device with given profile and time.
    /// </summary>
    public Vector3 GetAverageVelocity(TrackingProfile _profile, float _time)
    {
        Vector3 averageVelocity = Vector3.zero;
        foreach (GameObject device in devices)
        {
            if (device.GetComponent<PatternRecognition>().profile == _profile)
            {
                averageVelocity = device.GetComponent<TrackedInput>().GetAverageVelocity(_time);
                break;
            }
        }
        return averageVelocity;
    }

    /// <summary>
    /// Return average velocity over time of all devices and for the given time.
    /// </summary>
    public float GetAverageSpeedForAllDevices(float _time)
    {
        float sum = 0f;
        foreach (GameObject device in devices)
        {
            sum += device.GetComponent<TrackedInput>().GetAverageVelocity(_time).magnitude;
        }
        return sum / devices.Count;
    }

    /// <summary>
    /// Initialize pattern dictionaries from patterns List.
    /// </summary>
    private void InitializePatternDictionaries()
    {
        patternDictionaries = new List<PatternDictionary>();

        if (!string.IsNullOrEmpty(patternName))
        {
            if (AssertPattern(patternName, out Pattern newPattern))
            {
                patternDictionaries.Add(new PatternDictionary(newPattern));
                Debug.Log("Pattern " + patternName + " loaded.");
                return;
            }
            Debug.LogError("Pattern " + patternName + " not found. Loading all paterns.");
        }

        foreach (Pattern pattern in patterns)
        {
            patternDictionaries.Add(new PatternDictionary(pattern));
            Debug.Log("Pattern " + pattern.name + " has length " + pattern.samplingRate * pattern.leftArmPatternCoords.Length);
        }
    }

    /// <summary>
    /// Assert if pattern with given name exists in patterns List and returns it in out parameter.
    /// </summary>
    private bool AssertPattern(string _patternName, out Pattern _pattern)
    {
        return (_pattern = patterns.Find(x => x.name == _patternName)) != null;
    }

    /// <summary>
    /// Initialize dictionaries for tracking profile status (start position reached and t-pose position reached).
    /// </summary>
    private void InitializeTrackerDictionaries()
    {
        tPosePositionsReached = new Dictionary<TrackingProfile, bool>();
        startPositionsReached = new Dictionary<TrackingProfile, bool>();
        recognizedPattern = new Dictionary<TrackingProfile, PatternDictionary>();
        foreach (GameObject device in devices)
        {
            tPosePositionsReached.Add(device.GetComponent<PatternRecognition>().profile, false);
            startPositionsReached.Add(device.GetComponent<PatternRecognition>().profile, false);
        }
    }

    /// <summary>
    /// Start calibration timer coroutine if all devices are in t-pose.
    /// </summary>
    private void UpdateTPosePositionStatus(TrackingProfile _profile, bool _status)
    {
        tPosePositionsReached[_profile] = _status;
        if (tPosePositionsReached.ContainsValue(false))
        {
            if (playerCalibrationCoroutine != null)
            {
                StopCoroutine(playerCalibrationCoroutine);
                Actions.OnDialoguePrevious?.Invoke();
                playerCalibrationCoroutine = null;
            }
            return;
        }
        if (playerCalibrationCoroutine == null) playerCalibrationCoroutine = StartCoroutine(PlayerCalibrationTimer());
    }

    /// <summary>
    /// Start calibration if all devices are in t-pose after waiting for 3 seconds.
    /// </summary>
    private IEnumerator PlayerCalibrationTimer()
    {
        Actions.OnDialogueNext?.Invoke();
        yield return new WaitForSeconds(3f);
        // if all devices are still in t-pose (all true) send calibration event, else return
        if (tPosePositionsReached.ContainsValue(false))
        {
            playerCalibrationCoroutine = null;
            yield break;
        }
        Actions.OnCalibrationStarted?.Invoke();
        Actions.OnDialogueNext?.Invoke();
    }

    /// <summary>
    /// Start recording or recognition if all devices are in start position.
    /// </summary>
    private void UpdateStartPositionStatus(TrackingProfile _profile, bool _status)
    {
        startPositionsReached[_profile] = _status;
        if (startPositionsReached.ContainsValue(false) || recognitionActive) return;
        if (GetComponent<PatternRecording>())
        {
            Actions.OnRecordingStarted?.Invoke(new InputAction.CallbackContext());
            return;
        }
        recognitionActive = true;
        InitializeRecognition();
    }

    /// <summary>
    /// Initializes recognition by setting recognition active to true and instantiating the pattern helper.
    /// </summary>
    public void InitializeRecognition()
    {
        audioManager.Play("TaikoOnce");
        // reset startPositionsReached so that all devices are false
        List<TrackingProfile> keysToModify = new List<TrackingProfile>(startPositionsReached.Keys);

        foreach (TrackingProfile key in keysToModify)
        {
            startPositionsReached[key] = false;
        }

        foreach (GameObject device in devices)
        {
            device.GetComponent<PatternRecognition>().InstantiatePatternHelpers();
        }
        timeStamp = Time.time;
        ActivateVelocityAverage(TrackingProfile.RightArm);
        ActivateVelocityAverage(TrackingProfile.LeftArm);
        ActivateVelocityAverage(TrackingProfile.RightLeg);
        ActivateVelocityAverage(TrackingProfile.LeftLeg);

        if (isGameScene) OrbManager.instance.SetTargetsToHands();

        /// first pattern in list
        patternWithLowestEuclideanDifference = patternDictionaries[0];
        lowestEuclideanDifference = patternDictionaries[0].EuclideanDifference;
        patternWithLowestDTWDifference = patternDictionaries[0];
        lowestDTWDifference = patternDictionaries[0].DtwDifference;
    }

    private void UpdateEuclideanDifference(PatternDictionary _patternDictionary)
    {
        if (useDTW) return;
        _patternDictionary.GetEuclideanAverage();

        if (patterns.Count == 1) return; // only one pattern in list
        /// update pattern with lowest euclidean difference
        if (_patternDictionary == patternWithLowestEuclideanDifference) lowestEuclideanDifference = _patternDictionary.EuclideanDifference;
        else if (_patternDictionary.EuclideanDifference < lowestEuclideanDifference)
        {
            lowestEuclideanDifference = _patternDictionary.EuclideanDifference;
            patternWithLowestEuclideanDifference = _patternDictionary;
        }
        if (!isGameScene) return;
        /// if similarity is above threshold, remove pattern from list
        if (_patternDictionary.EuclideanDifference > _patternDictionary.EuclideanThreshold)
        {
            Actions.OnThresholdCrossed?.Invoke(_patternDictionary);
            Debug.Log("Removed pattern " + _patternDictionary.pattern.name + " from list. Euclidean difference: " + _patternDictionary.EuclideanDifference + " Threshold: " + _patternDictionary.EuclideanThreshold);
            patternDictionaries.Remove(_patternDictionary);
            if (_patternDictionary == patternWithLowestEuclideanDifference && patternDictionaries.Count > 0)
            {
                foreach (PatternDictionary patternDictionary in patternDictionaries)
                {
                    if (patternDictionary.EuclideanDifference < lowestEuclideanDifference)
                    {
                        lowestEuclideanDifference = patternDictionary.EuclideanDifference;
                        patternWithLowestEuclideanDifference = patternDictionary;
                    }
                }
            }
            if (patternDictionaries.Count > 0) return;
            Actions.OnRecognitionFailed?.Invoke();
            Debug.Log("Recognition failed");
            ResetRecognition();
        }
    }

    private void UpdateDTWDifference(PatternDictionary _patternDictionary)
    {
        if (!useDTW) return;
        _patternDictionary.GetDTWAverage();
        if (patterns.Count == 1) return; // only one pattern in list
        if (_patternDictionary == patternWithLowestDTWDifference) lowestDTWDifference = _patternDictionary.DtwDifference;
        else if (_patternDictionary.DtwDifference < lowestDTWDifference)
        {
            lowestDTWDifference = _patternDictionary.DtwDifference;
            patternWithLowestDTWDifference = _patternDictionary;
        }
        if (!isGameScene) return;

        if (_patternDictionary.DtwDifference > _patternDictionary.DtwThreshold)
        {
            Actions.OnThresholdCrossed?.Invoke(_patternDictionary);
            Debug.Log("Removed pattern " + _patternDictionary.pattern.name + " from list. DTW difference: " + _patternDictionary.DtwDifference + " Threshold: " + _patternDictionary.DtwThreshold);
            patternDictionaries.Remove(_patternDictionary);
            if (_patternDictionary == patternWithLowestDTWDifference && patternDictionaries.Count > 0)
            {
                foreach (PatternDictionary patternDictionary in patternDictionaries)
                {
                    if (patternDictionary.DtwDifference < lowestDTWDifference)
                    {
                        lowestDTWDifference = patternDictionary.DtwDifference;
                        patternWithLowestDTWDifference = patternDictionary;
                    }
                }
            }
            if (patternDictionaries.Count > 0) return;
            Actions.OnRecognitionFailed?.Invoke();
            Debug.Log("Recognition failed");
            ResetRecognition();
        }
    }

    private void OnPlayerMovementStopped()
    {
        if (!useDTW) return;
        foreach (PatternDictionary patternDictionary in patternDictionaries.ToArray())
        {
            UpdateDTWDifference(patternDictionary);
        }
        if (patternDictionaries.Count == 0) return;

        // if average controller speed is way below average pattern speed, don't recognize pattern -> player is not moving
        float time = Time.time - timeStamp;
        float averageControllerSpeed = GetAverageSpeedForAllDevices(time);
        float minPatternSpeed = 0.001f * patternWithLowestDTWDifference.GetAveragePatternSpeed();

        if (averageControllerSpeed < minPatternSpeed)
        {
            Debug.Log("Average controller speed: " + averageControllerSpeed + " Min pattern speed: " + minPatternSpeed + " Time: " + time);
            return;
        }
        PatternRecognized(TrackingProfile.LeftArm, patternWithLowestDTWDifference);
        PatternRecognized(TrackingProfile.RightArm, patternWithLowestDTWDifference);
        PatternRecognized(TrackingProfile.LeftLeg, patternWithLowestDTWDifference);
        PatternRecognized(TrackingProfile.RightLeg, patternWithLowestDTWDifference);
    }

    private void PatternRecognized(TrackingProfile _profile, PatternDictionary _patternDictionary)
    {
        if (!recognitionActive) return;
        if (recognizedPattern.ContainsKey(_profile))
        {
            Debug.LogWarning("Profile already recognized a pattern: " + recognizedPattern[_profile].Name + ". Overwriting previous pattern with " + _patternDictionary.Name + ".");
            recognizedPattern[_profile] = _patternDictionary;
        }
        else recognizedPattern.Add(_profile, _patternDictionary);
        if (recognizedPattern.Count < startPositionsReached.Count) return;

        if (!AssertPatternEquality(recognizedPattern))
        {
            Debug.LogWarning("Not all recognized patterns are the same. Resetting recognition.");
            Actions.OnRecognitionFailed?.Invoke();
            ResetRecognition();
            return;
        }
        if (isGameScene)
        {
            // if total distance crossed is too low, don't recognize pattern -> player is not moving
            float playerDist = patternWithLowestDTWDifference.GetTotalPlayerDistanceCrossed();
            float patternDist = patternWithLowestDTWDifference.GetTotalPatternDistance();
            float minPatternDist = 0.3f * patternDist;
            Debug.Log("Total player distance crossed: " + playerDist + " Total pattern distance: " + patternDist + " Min pattern distance: " + minPatternDist);
            if (playerDist < minPatternDist)
            {
                Debug.LogWarning("Total distance crossed is too low. Resetting recognition.");
                Actions.OnRecognitionFailed?.Invoke();
                ResetRecognition();
                return;
            }
        }
        Debug.Log("All recognized patterns are the same. Pattern recognized: " + recognizedPattern.Values.First().Name);
        recognizedPattern.Values.First().averageVelocity = GetAverageVelocity(TrackingProfile.RightArm, 2f);
        Actions.OnRecognition?.Invoke(recognizedPattern.Values.First());
        ResetRecognition();
    }

    /// <summary>
    /// Returns true if all patterns in the dictionary are the same.
    /// </summary>
    private bool AssertPatternEquality(Dictionary<TrackingProfile, PatternDictionary> _dictionary)
    {
        if (!_dictionary.Values.Any()) return false;
        string firstValue = _dictionary.Values.First().pattern.name;
        return _dictionary.Values.All(v => v.pattern.name == firstValue);
    }

    private PatternDictionary ScalePattern(PatternDictionary _patternDictionary, Dictionary<TrackingProfile, float> _scaleFactorMap)
    {
        foreach (TrackingProfile profile in _patternDictionary.localCoordinatesMap.Keys)
        {
            if (profile == TrackingProfile.Waist) continue;
            float scaleFactor = _scaleFactorMap[profile];

            Vector3[] coordinates = _patternDictionary.localCoordinatesMap[profile];
            for (int i = 0; i < coordinates.Length; i++)
            {
                coordinates[i] *= scaleFactor;
            }
        }
        return _patternDictionary;
    }

    public void ScalePatterns(bool _playerAvatar, Dictionary<TrackingProfile, float> _scaleFactors)
    {
        if (!_playerAvatar) return;

        /// scale PatternDictionaries
        foreach (PatternDictionary patternDictionary in patternDictionaries)
        {
            ScalePattern(patternDictionary, _scaleFactors);
        }

        /// scale startPose
        startPose = ScalePattern(new PatternDictionary(startPose), _scaleFactors).pattern;
        Actions.OnPatternsScaled?.Invoke();
    }


    private void ResetRecognition()
    {
        if (!recognitionActive) return;
        patterns = LoadFile();
        Actions.OnRecognitionReset?.Invoke();
        InitializePatternDictionaries();
        InitializeTrackerDictionaries();
        recognitionActive = false;
    }

    public void ToggleTrail(bool _status)
    {
        trailHelper = _status;
    }

    public void ToggleTolerance(bool _status)
    {
        toleranceHelper = _status;
    }

    public void ToggleVelocity(bool _status)
    {
        velocityHelper = _status;
    }

    /// <summary>
    /// Read patterns from file in project folder.
    /// </summary>
    public List<Pattern> LoadFile()
    {
        if (!File.Exists(saveFile))
        {
            Debug.Log("No savefile found, creating new file.");
            return new List<Pattern>();
        }

        using (FileStream stream = File.Open(saveFile, FileMode.Open))
        {
            using (StreamReader reader = new StreamReader(stream))
            {
                string json = reader.ReadToEnd();
                return JsonUtility.FromJson<SerializableList<Pattern>>(json).list;
            }
        }
    }

    /// <summary>
    /// Read patterns from file after building the application.
    /// </summary>
    public List<Pattern> LoadFromAssets()
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>("patterns.json");
        return JsonUtility.FromJson<SerializableList<Pattern>>(jsonAsset.text).list;
    }

    /// <summary>
    /// Write patterns to file.
    /// </summary>
    public void WriteFile(string _json)
    {
        File.WriteAllText(saveFile, _json);
        Debug.Log("Saved to " + saveFile);
    }
}
