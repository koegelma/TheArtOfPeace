using UnityEngine;

/// <summary>
/// Class representing a pattern - serializable for JSON export.
/// </summary>
[System.Serializable]
public class Pattern
{
    [SerializeField] public string name;
    [SerializeField] public int tier;
    [SerializeField] public float tolerance;
    [SerializeField] public float samplingRate;
    /// <summary>
    /// Value representing the result of the euclidean distance calculation between the pattern and the player's movement.
    /// </summary>
    [SerializeField] public float euclideanDifference;
    /// <summary>
    /// Value representing the threshold for the euclidean difference between the pattern and the player's movement.
    /// </summary>
    [SerializeField] public float euclideanThreshold;
    /// <summary>
    /// Value representing the result of the dynamic time warping distance calculation between the pattern and the player's movement.
    /// </summary>
    [SerializeField] public float dtwDifference;
    /// <summary>
    /// Value representing the threshold for the dynamic time warping difference between the pattern and the player's movement.
    /// </summary>
    [SerializeField] public float dtwThreshold;

    [SerializeField] public Vector3[] waistPatternCoords;
    [SerializeField] public Vector3[] leftArmPatternCoords;
    [SerializeField] public Vector3[] rightArmPatternCoords;
    [SerializeField] public Vector3[] leftLegPatternCoords;
    [SerializeField] public Vector3[] rightLegPatternCoords;

    [SerializeField] public Vector3[] waistPatternCoordsWorld;
    [SerializeField] public Vector3[] leftArmPatternCoordsWorld;
    [SerializeField] public Vector3[] rightArmPatternCoordsWorld;
    [SerializeField] public Vector3[] leftLegPatternCoordsWorld;
    [SerializeField] public Vector3[] rightLegPatternCoordsWorld;
    [SerializeField] public Vector3[] headPatternCoordsWorld;

    [SerializeField] public Vector3[] leftArmVelocity;
    [SerializeField] public Vector3[] rightArmVelocity;
    [SerializeField] public Vector3[] leftLegVelocity;
    [SerializeField] public Vector3[] rightLegVelocity;

    [SerializeField] public Quaternion[] waistRotation;
    [SerializeField] public Quaternion[] leftArmRotation;
    [SerializeField] public Quaternion[] rightArmRotation;
    [SerializeField] public Quaternion[] leftLegRotation;
    [SerializeField] public Quaternion[] rightLegRotation;
    [SerializeField] public Quaternion[] headRotation;

    [SerializeField] public Quaternion waistRotationOffset;
    [SerializeField] public Quaternion leftArmRotationOffset;
    [SerializeField] public Quaternion rightArmRotationOffset;
    [SerializeField] public Quaternion leftLegRotationOffset;
    [SerializeField] public Quaternion rightLegRotationOffset;
    [SerializeField] public Quaternion headRotationOffset;

    public Pattern(string _name, float _tolerance, float _samplingRate,
                    Vector3[] _waistPatternCoords, Vector3[] _leftArmPatternCoords, Vector3[] _rightArmPatternCoords, Vector3[] _leftLegPatternCoords, Vector3[] _rightLegPatternCoords,
                    Vector3[] _waistPatternCoordsWorld, Vector3[] _leftArmPatternCoordsWorld, Vector3[] _rightArmPatternCoordsWorld, Vector3[] _leftLegPatternCoordsWorld, Vector3[] _rightLegPatternCoordsWorld, Vector3[] _headPatternCoordsWorld,
                    Vector3[] _leftArmVelocity, Vector3[] _rightArmVelocity, Vector3[] _leftLegVelocity, Vector3[] _rightLegVelocity,
                    Quaternion[] _waistRotation, Quaternion[] _leftArmRotation, Quaternion[] _rightArmRotation, Quaternion[] _leftLegRotation, Quaternion[] _rightLegRotation, Quaternion[] _headRotation,
                    Quaternion _waistRotationOffset, Quaternion _leftArmRotationOffset, Quaternion _rightArmRotationOffset, Quaternion _leftLegRotationOffset, Quaternion _rightLegRotationOffset, Quaternion _headRotationOffset)
    {
        name = _name;
        tolerance = _tolerance;
        samplingRate = _samplingRate;
        waistPatternCoords = _waistPatternCoords;
        leftArmPatternCoords = _leftArmPatternCoords;
        rightArmPatternCoords = _rightArmPatternCoords;
        leftLegPatternCoords = _leftLegPatternCoords;
        rightLegPatternCoords = _rightLegPatternCoords;

        waistPatternCoordsWorld = _waistPatternCoordsWorld;
        leftArmPatternCoordsWorld = _leftArmPatternCoordsWorld;
        rightArmPatternCoordsWorld = _rightArmPatternCoordsWorld;
        leftLegPatternCoordsWorld = _leftLegPatternCoordsWorld;
        rightLegPatternCoordsWorld = _rightLegPatternCoordsWorld;
        headPatternCoordsWorld = _headPatternCoordsWorld;

        leftArmVelocity = _leftArmVelocity;
        rightArmVelocity = _rightArmVelocity;
        leftLegVelocity = _leftLegVelocity;
        rightLegVelocity = _rightLegVelocity;

        waistRotation = _waistRotation;
        leftArmRotation = _leftArmRotation;
        rightArmRotation = _rightArmRotation;
        leftLegRotation = _leftLegRotation;
        rightLegRotation = _rightLegRotation;
        headRotation = _headRotation;

        waistRotationOffset = _waistRotationOffset;
        leftArmRotationOffset = _leftArmRotationOffset;
        rightArmRotationOffset = _rightArmRotationOffset;
        leftLegRotationOffset = _leftLegRotationOffset;
        rightLegRotationOffset = _rightLegRotationOffset;
        headRotationOffset = _headRotationOffset;

        euclideanDifference = 0;
        euclideanThreshold = 0; /// Is set in PatternDictionary.cs
        dtwDifference = 0;
        dtwThreshold = 0; /// Is set in PatternDictionary.cs
    }
}
