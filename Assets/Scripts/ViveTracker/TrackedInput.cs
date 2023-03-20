using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TrackedInput : MonoBehaviour
{
    [Header("References")]
    public TrackingProfile profile;
    public InputActionReference trackingReference = null;
    public InputActionReference positionReference = null;
    public InputActionReference rotationReference = null;
    public InputActionReference velocityReference = null;
    public InputActionReference angularVelocityReference = null;
    public Transform trackingTransform = null; // indipendent transform that is not affected by the !intitial! tracker rotation
    private Vector3 rotationOffset;

    public float trackingValue;
    public bool IsTracked { get { return trackingValue == 1; } }
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 velocity;
    public Vector3 angularVelocity;

    private Coroutine deviceStatusCoroutine;

    [Header("Average Velocity")]
    private Vector3 averageVelocity;
    private List<Vector3> velocitySamples;
    private List<float> sampleAges;
    private float sampleInterval = 0.1f; // time between velocity samples in seconds
    private float maxSampleAge = 10f; // maximum age of a velocity sample in seconds
    private float currentSampleAge = 0f;
    private bool trackingVelocityAverage = false;


    private void Start()
    {
        if (trackingTransform == null) return;
        StartCoroutine(CalculateRotationOffset());
    }

    private void Update() //TODO: getter
    {
        trackingValue = trackingReference.action.ReadValue<float>();
        position = positionReference.action.ReadValue<Vector3>();
        rotation = rotationReference.action.ReadValue<Quaternion>();
        velocity = velocityReference.action.ReadValue<Vector3>();
        angularVelocity = angularVelocityReference.action.ReadValue<Vector3>();

        if (trackingVelocityAverage) UpdateVelocityAverage();

        if (rotationOffset == Vector3.zero || trackingTransform == null) return;
        trackingTransform.position = position;
        trackingTransform.rotation = rotation * Quaternion.Euler(rotationOffset);

        //if (isTracked == 0 && deviceStatusCoroutine == null) deviceStatusCoroutine = StartCoroutine(TryDeviceStatus(5));
    }

    /// <summary>
    /// Deactivates the device after a certain amount of time if no tracking data is received.
    /// </summary>
    private IEnumerator TryDeviceStatus(float _waitForSeconds)
    {
        float time = 0;
        while (time < _waitForSeconds)
        {
            if (IsTracked)
            {
                deviceStatusCoroutine = null;
                yield break;
            }
            time += Time.deltaTime;
            yield return null;
        }
        if (!IsTracked)
        {
            Debug.Log("Device not tracked: " + GetComponent<PatternRecognition>().profile);
            PatternManager.instance.devices.Remove(gameObject);
            gameObject.SetActive(false);
        }
        deviceStatusCoroutine = null;
    }

    public void ActivateVelocityAverage(float _time)
    {
        maxSampleAge = _time;
        velocitySamples = new List<Vector3>();
        sampleAges = new List<float>();
        currentSampleAge = 0f;
        trackingVelocityAverage = true;
    }

    private void UpdateVelocityAverage()
    {
        currentSampleAge += Time.deltaTime;
        if (currentSampleAge >= sampleInterval)
        {
            currentSampleAge = 0f;
            velocitySamples.Add(velocity); // add the current velocity to the list
            sampleAges.Add(Time.time); // add the current time to the list
            while (velocitySamples.Count > 0 && Time.time - sampleAges[0] > maxSampleAge)
            {
                velocitySamples.RemoveAt(0); // remove old samples from the list
                sampleAges.RemoveAt(0);
            }
            // calculate average velocity
            Vector3 sum = Vector3.zero;
            foreach (Vector3 sample in velocitySamples)
            {
                sum += sample;
            }
            averageVelocity = sum / velocitySamples.Count;
        }
    }

    /// <summary>
    /// Returns the average velocity of the tracked object over the given time. If the given time is greater than the maximum sample age, the maximum sample age is used instead.
    /// </summary>
    public Vector3 GetAverageVelocity(float _time)
    {
        float time = Mathf.Clamp(_time, 0f, maxSampleAge);
        Vector3 sum = Vector3.zero;
        int count = 0;
        for (int i = 0; i < velocitySamples.Count; i++)
        {
            if (Time.time - sampleAges[i] <= time)
            {
                sum += velocitySamples[i];
                count++;
            }
        }
        return sum / count;
    }

    private IEnumerator CalculateRotationOffset() // needs to be called in regular intervals? if tracking is lost, the offset is not updated
    {
        yield return new WaitUntil(() => IsTracked);
        position = positionReference.action.ReadValue<Vector3>();
        rotation = rotationReference.action.ReadValue<Quaternion>();

        // sets trackingTransform slightly behind the tracker, aligns forward vector with tracker forward vector, then calculates the offset
        trackingTransform.position = transform.TransformPoint(0, 0, -0.1f);
        trackingTransform.LookAt(transform.position);

        Quaternion offset = Quaternion.Inverse(rotation) * trackingTransform.rotation;
        rotationOffset = offset.eulerAngles;
    }
}
