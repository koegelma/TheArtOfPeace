using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class is used to store the pattern data in a more accessible way - not serializable.
/// </summary>
public class PatternDictionary
{
    public Pattern pattern;
    public string Name { get { return pattern.name; } }
    public Tier Tier { get { return (Tier)pattern.tier; } }
    public float Tolerance { get { return pattern.tolerance; } }
    public float SamplingRate { get { return pattern.samplingRate; } }
    public float EuclideanDifference { get { return pattern.euclideanDifference; } set { pattern.euclideanDifference = value; } }
    public float EuclideanThreshold { get { return pattern.euclideanThreshold; } }
    public float DtwDifference { get { return pattern.dtwDifference; } set { pattern.dtwDifference = value; } }
    public float DtwThreshold { get { return pattern.dtwThreshold; } }
    public Dictionary<TrackingProfile, float> euclideanMap;
    public Dictionary<TrackingProfile, float> dtwMap;

    public Dictionary<TrackingProfile, Vector3[]> localCoordinatesMap;
    public Dictionary<TrackingProfile, Vector3[]> worldCoordinatesMap;
    public Dictionary<TrackingProfile, Vector3[]> velocitiesMap;
    public Dictionary<TrackingProfile, Quaternion[]> rotationsMap;
    public Dictionary<TrackingProfile, Quaternion> rotationOffsetsMap;

    public Vector3 averageVelocity;
    public Vector3[] playerPositions;
    public Dictionary<TrackingProfile, Vector3[]> playerPositionsMap;

    public PatternDictionary(Pattern _pattern)
    {
        pattern = _pattern;

        euclideanMap = new Dictionary<TrackingProfile, float>();
        dtwMap = new Dictionary<TrackingProfile, float>();
        foreach (GameObject device in PatternManager.instance.devices)
        {
            euclideanMap.Add(device.GetComponent<PatternRecognition>().profile, EuclideanDifference);
            dtwMap.Add(device.GetComponent<PatternRecognition>().profile, DtwDifference);
        }

        localCoordinatesMap = new Dictionary<TrackingProfile, Vector3[]>();
        worldCoordinatesMap = new Dictionary<TrackingProfile, Vector3[]>();
        velocitiesMap = new Dictionary<TrackingProfile, Vector3[]>();
        rotationsMap = new Dictionary<TrackingProfile, Quaternion[]>();
        rotationOffsetsMap = new Dictionary<TrackingProfile, Quaternion>();

        localCoordinatesMap.Add(TrackingProfile.Waist, pattern.waistPatternCoords);
        localCoordinatesMap.Add(TrackingProfile.LeftArm, pattern.leftArmPatternCoords);
        localCoordinatesMap.Add(TrackingProfile.RightArm, pattern.rightArmPatternCoords);
        localCoordinatesMap.Add(TrackingProfile.LeftLeg, pattern.leftLegPatternCoords);
        localCoordinatesMap.Add(TrackingProfile.RightLeg, pattern.rightLegPatternCoords);

        worldCoordinatesMap.Add(TrackingProfile.Waist, pattern.waistPatternCoordsWorld);
        worldCoordinatesMap.Add(TrackingProfile.LeftArm, pattern.leftArmPatternCoordsWorld);
        worldCoordinatesMap.Add(TrackingProfile.RightArm, pattern.rightArmPatternCoordsWorld);
        worldCoordinatesMap.Add(TrackingProfile.LeftLeg, pattern.leftLegPatternCoordsWorld);
        worldCoordinatesMap.Add(TrackingProfile.RightLeg, pattern.rightLegPatternCoordsWorld);
        if (pattern.headPatternCoordsWorld != null) worldCoordinatesMap.Add(TrackingProfile.Head, pattern.headPatternCoordsWorld);

        velocitiesMap.Add(TrackingProfile.LeftArm, pattern.leftArmVelocity);
        velocitiesMap.Add(TrackingProfile.RightArm, pattern.rightArmVelocity);
        velocitiesMap.Add(TrackingProfile.LeftLeg, pattern.leftLegVelocity);
        velocitiesMap.Add(TrackingProfile.RightLeg, pattern.rightLegVelocity);

        rotationsMap.Add(TrackingProfile.Waist, pattern.waistRotation);
        rotationsMap.Add(TrackingProfile.LeftArm, pattern.leftArmRotation);
        rotationsMap.Add(TrackingProfile.RightArm, pattern.rightArmRotation);
        rotationsMap.Add(TrackingProfile.LeftLeg, pattern.leftLegRotation);
        rotationsMap.Add(TrackingProfile.RightLeg, pattern.rightLegRotation);
        if (pattern.headRotation != null) rotationsMap.Add(TrackingProfile.Head, pattern.headRotation);

        rotationOffsetsMap.Add(TrackingProfile.Waist, pattern.waistRotationOffset);
        rotationOffsetsMap.Add(TrackingProfile.LeftArm, pattern.leftArmRotationOffset);
        rotationOffsetsMap.Add(TrackingProfile.RightArm, pattern.rightArmRotationOffset);
        rotationOffsetsMap.Add(TrackingProfile.LeftLeg, pattern.leftLegRotationOffset);
        rotationOffsetsMap.Add(TrackingProfile.RightLeg, pattern.rightLegRotationOffset);
        if (pattern.headRotationOffset != null) rotationOffsetsMap.Add(TrackingProfile.Head, pattern.headRotationOffset);

        playerPositions = new Vector3[1];
        playerPositions[0] = Vector3.zero;
        playerPositionsMap = new Dictionary<TrackingProfile, Vector3[]>();
        playerPositionsMap.Add(TrackingProfile.LeftArm, playerPositions);
        playerPositionsMap.Add(TrackingProfile.RightArm, playerPositions);
        playerPositionsMap.Add(TrackingProfile.LeftLeg, playerPositions);
        playerPositionsMap.Add(TrackingProfile.RightLeg, playerPositions);

        pattern.euclideanThreshold = GetEuclideanThreshold();
        pattern.dtwThreshold = GetDtwThreshold();
    }

    /// <summary>
    /// Get the euclidean threshold based on the recorded time of the pattern.
    /// </summary>
    public float GetEuclideanThreshold()
    {
        float timeIndepedentThreshold = 0.654f; // value based on results from user testing
        float patternTime = localCoordinatesMap[TrackingProfile.LeftArm].Length * SamplingRate;
        float euclideanThreshold = timeIndepedentThreshold * patternTime;
        return euclideanThreshold;
    }

    /// <summary>
    /// Get the DTW threshold based on the recorded time of the pattern.
    /// </summary>
    public float GetDtwThreshold()
    {
        float timeIndepedentThreshold = 1.230f; // value based on results from user testing
        float patternTime = localCoordinatesMap[TrackingProfile.LeftArm].Length * SamplingRate;
        float dtwThreshold = timeIndepedentThreshold * patternTime;
        Debug.Log("Pattern: " + pattern.name + "DTW threshold: " + dtwThreshold);
        return dtwThreshold;
    }

    public float GetEuclideanAverage()
    {
        float sum = 0f;
        foreach (float value in euclideanMap.Values)
        {
            sum += value;
        }
        float euclideanAverage = sum / euclideanMap.Count;
        EuclideanDifference = euclideanAverage;
        return euclideanAverage;
    }

    public float GetDTWAverage()
    {
        float sum = 0f;
        foreach (float value in dtwMap.Values)
        {
            sum += value;
        }
        float dtwAverage = sum / dtwMap.Count;
        DtwDifference = dtwAverage;
        return dtwAverage;
    }

    public float GetAveragePatternSpeed()
    {
        float sum = 0f;
        foreach (Vector3[] velocity in velocitiesMap.Values)
        {
            foreach (Vector3 v in velocity)
            {
                sum += v.magnitude;
            }
        }
        float averageSpeed = sum / (velocitiesMap.Count * velocitiesMap[TrackingProfile.LeftArm].Length);
        return averageSpeed;
    }

    public float GetTotalPlayerDistanceCrossed()
    {
        float sum = 0;
        foreach (Vector3[] positions in playerPositionsMap.Values)
        {
            for (int i = 0; i < positions.Length - 1; i++)
            {
                sum += Vector3.Distance(positions[i], positions[i + 1]);
            }
        }
        return sum;
    }

    public float GetTotalPatternDistance()
    {
        float sum = 0;
        foreach (TrackingProfile profile in localCoordinatesMap.Keys)
        {
            if (profile == TrackingProfile.Waist) continue;
            for (int i = 0; i < localCoordinatesMap[profile].Length - 1; i++)
            {
                sum += Vector3.Distance(localCoordinatesMap[profile][i], localCoordinatesMap[profile][i + 1]);
            }
        }
        return sum;
    }
}
