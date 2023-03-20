using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PatternRecording : MonoBehaviour
{
    [SerializeField]
    public SerializableList<Pattern> savedPatterns = new SerializableList<Pattern>();

    [Header("General Settings")]
    public InputActionReference toggleReferenceLeft = null;
    public InputActionReference toggleReferenceRight = null;
    public InputActionReference tPoseReferenceLeft = null;
    public InputActionReference tPoseReferenceRight = null;
    [HideInInspector] public bool recordingActive;
    [HideInInspector] public float nextRecordTime;
    public float samplingRate;
    [HideInInspector] public Dictionary<TrackingProfile, List<Vector3>> patternCoords = new Dictionary<TrackingProfile, List<Vector3>>();
    [HideInInspector] public Dictionary<TrackingProfile, List<Vector3>> patternCoordsWorld = new Dictionary<TrackingProfile, List<Vector3>>();
    [HideInInspector] public Dictionary<TrackingProfile, List<Vector3>> velocities = new Dictionary<TrackingProfile, List<Vector3>>();
    [HideInInspector] public Dictionary<TrackingProfile, List<Quaternion>> rotations = new Dictionary<TrackingProfile, List<Quaternion>>();
    [HideInInspector] public Dictionary<TrackingProfile, Quaternion> tPoseRotations = new Dictionary<TrackingProfile, Quaternion>(); /// rotation of device in t-pose, used to calculate rotation offset

    [HideInInspector] public GameObject waistDevice;
    private Vector3 waistWorldPosition;
    private GameObject headDevice;

    [Header("New Pattern Settings")]
    public string newPatternName;
    public float newPatternTolerance;
    private PatternManager manager;

    private void Awake()
    {
        toggleReferenceLeft.action.started += Toggle;
        toggleReferenceRight.action.started += Toggle;

        Actions.OnRecordingStarted += Toggle;

        tPoseReferenceLeft.action.started += RecordTPoseRotation;
        tPoseReferenceRight.action.started += RecordTPoseRotation;
    }

    private void Start()
    {
        manager = PatternManager.instance;
        recordingActive = false;
        nextRecordTime = 0f;
        waistDevice = manager.waistDevice;

        headDevice = GameObject.Find("Main Camera");

        /// initialize dictionaries
        foreach (GameObject device in manager.devices)
        {
            TrackingProfile profile = device.GetComponent<PatternRecognition>().profile;
            patternCoords.Add(profile, new List<Vector3>());
            patternCoordsWorld.Add(profile, new List<Vector3>());
            velocities.Add(profile, new List<Vector3>());
            rotations.Add(profile, new List<Quaternion>());
        }
        patternCoords.Add(TrackingProfile.Waist, new List<Vector3>());
        patternCoordsWorld.Add(TrackingProfile.Waist, new List<Vector3>());
        rotations.Add(TrackingProfile.Waist, new List<Quaternion>());

        patternCoordsWorld.Add(TrackingProfile.Head, new List<Vector3>()); //
        rotations.Add(TrackingProfile.Head, new List<Quaternion>()); //

        savedPatterns.list = manager.LoadFile();
        Debug.Log("Saved patterns found and loaded: " + savedPatterns.list.Count);
    }

    private void Update()
    {
        if (!recordingActive) return;

        if (Time.time > nextRecordTime)
        {
            nextRecordTime += samplingRate;
            RecordPattern();
        }
    }

    /// <summary>
    /// Toggle recording of pattern on/off.
    /// </summary>
    private void Toggle(InputAction.CallbackContext _context)
    {
        recordingActive = !recordingActive;

        Debug.Log("Recording: " + recordingActive);

        if (!recordingActive)
        {
            if (tPoseRotations.Count == 0)
            {
                Debug.LogWarning("TPose not set, recording has not been saved!");
                ResetDicts();
                return;
            }
            SaveNewPattern();
        }
        else
        {
            waistWorldPosition = waistDevice.transform.position;
            nextRecordTime = Time.time;
        }
    }

    /// <summary>
    /// Record the current device rotation (in t-pose) for each device.
    /// </summary>
    private void RecordTPoseRotation(InputAction.CallbackContext _context)
    {
        if (tPoseRotations.Count > 0) tPoseRotations.Clear();

        foreach (GameObject device in manager.devices)
        {
            TrackingProfile profile = device.GetComponent<PatternRecognition>().profile;
            tPoseRotations.Add(profile, device.transform.rotation);
        }
        tPoseRotations.Add(TrackingProfile.Waist, waistDevice.transform.rotation);
        tPoseRotations.Add(TrackingProfile.Head, headDevice.transform.rotation); //
        Debug.Log("TPose recorded for " + tPoseRotations.Count + " devices");
    }

    /// <summary>
    /// Saves current position to pattern coords list. Gets called x-times per second (float samplingRate).
    /// </summary>
    private void RecordPattern()
    {
        foreach (GameObject device in manager.devices)
        {
            TrackingProfile profile = device.GetComponent<PatternRecognition>().profile;

            patternCoords[profile].Add(GetRelativePosition(device.transform.position));
            patternCoordsWorld[profile].Add(device.transform.position);
            velocities[profile].Add(device.GetComponent<TrackedInput>().velocity);
            rotations[profile].Add(device.transform.rotation);
        }
        //WaistDevice
        patternCoords[TrackingProfile.Waist].Add(waistDevice.transform.position - waistWorldPosition);
        patternCoordsWorld[TrackingProfile.Waist].Add(waistDevice.transform.position);
        rotations[TrackingProfile.Waist].Add(waistDevice.transform.rotation);

        //HeadDevice
        patternCoordsWorld[TrackingProfile.Head].Add(headDevice.transform.position);
        rotations[TrackingProfile.Head].Add(headDevice.transform.rotation);
    }

    /// <summary>
    /// Returns the position relative to the waist position.
    /// </summary>
    private Vector3 GetRelativePosition(Vector3 _position)
    {
        return _position - waistWorldPosition;
    }

    /// <summary>
    /// Saves the new recorded pattern to the list of saved patterns, converts the list to JSON and saves it to file.
    /// </summary>
    private void SaveNewPattern()
    {
        Pattern newPattern = new Pattern(newPatternName, newPatternTolerance, samplingRate,
                patternCoords[TrackingProfile.Waist].ToArray(), patternCoords[TrackingProfile.LeftArm].ToArray(), patternCoords[TrackingProfile.RightArm].ToArray(), patternCoords[TrackingProfile.LeftLeg].ToArray(), patternCoords[TrackingProfile.RightLeg].ToArray(),
                patternCoordsWorld[TrackingProfile.Waist].ToArray(), patternCoordsWorld[TrackingProfile.LeftArm].ToArray(), patternCoordsWorld[TrackingProfile.RightArm].ToArray(), patternCoordsWorld[TrackingProfile.LeftLeg].ToArray(), patternCoordsWorld[TrackingProfile.RightLeg].ToArray(), patternCoordsWorld[TrackingProfile.Head].ToArray(),
                velocities[TrackingProfile.LeftArm].ToArray(), velocities[TrackingProfile.RightArm].ToArray(), velocities[TrackingProfile.LeftLeg].ToArray(), velocities[TrackingProfile.RightLeg].ToArray(),
                rotations[TrackingProfile.Waist].ToArray(), rotations[TrackingProfile.LeftArm].ToArray(), rotations[TrackingProfile.RightArm].ToArray(), rotations[TrackingProfile.LeftLeg].ToArray(), rotations[TrackingProfile.RightLeg].ToArray(), rotations[TrackingProfile.Head].ToArray(),
                tPoseRotations[TrackingProfile.Waist], tPoseRotations[TrackingProfile.LeftArm], tPoseRotations[TrackingProfile.RightArm], tPoseRotations[TrackingProfile.LeftLeg], tPoseRotations[TrackingProfile.RightLeg], tPoseRotations[TrackingProfile.Head]);

        if (savedPatterns.list.Exists(x => x.name == newPatternName))
        {
            Debug.Log("Pattern with name '" + newPatternName + "' already exists, overwriting...");
            savedPatterns.list.Remove(savedPatterns.list.Find(x => x.name == newPatternName));
        }

        savedPatterns.list.Add(newPattern);
        string json = JsonUtility.ToJson(savedPatterns, true);
        manager.WriteFile(json);

        ResetDicts();
    }

    /// <summary>
    /// Resets the dictionaries for the next recording.
    /// </summary>
    private void ResetDicts()
    {
        patternCoords.Clear();
        patternCoordsWorld.Clear();
        velocities.Clear();
        rotations.Clear();
        tPoseRotations.Clear();
    }
}
