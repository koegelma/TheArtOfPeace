using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.InputSystem;

public static class Actions
{
    public static Action<TrackingProfile, bool> OnTPosePositionReached;
    public static Action OnCalibrationStarted;
    public static Action<TrackingProfile, bool> OnStartPositionReached;
    public static Action<PatternDictionary> OnEuclideanDifferenceChanged;
    public static Action<PatternDictionary> OnThresholdCrossed;
    public static Action<PatternDictionary> OnDTWDifferenceChanged;
    public static Action<PatternDictionary> OnDTWThresholdCrossed;
    public static Action OnRecognitionReset;
    public static Action<TrackingProfile, PatternDictionary> OnPatternRecognized;
    public static Action<PatternDictionary> OnRecognition; // if all devices have recognized a pattern
    public static Action OnRecognitionFailed;
    public static Action OnPlayerMovementStopped;
    /// <summary>
    /// Called when the Rig is calibrated. First parameter is true if it is the player avatar, false if it is a animated character. 
    /// Second parameter is a dictionary of trackingprofiles to floats representing the scale of the limbs. 
    /// </summary>
    public static Action<bool, Dictionary<TrackingProfile, float>> OnPlayerRigReady; // true = player avatar, false = animated pattern character. 0: leftArm, 1: rightArm, 2: leftLeg, 3: rightLeg
    public static Action<InputAction.CallbackContext> OnRecordingStarted;
    public static Action OnPatternsScaled;
    public static Action<bool> OnDialogueToggle;
    public static Action OnDialogueNext;
    public static Action OnDialoguePrevious;

    [Header("Game States")]
    public static Action OnGameStarted;
    public static Action OnGamePaused;
    public static Action OnGameResumed;
    public static Action OnGameEnded;
    public static Action OnGameLost;
    public static Action OnGameWon;

    [Header("Game Events")]
    public static Action OnPlayerTargetSet;
    public static Action<OrbMovement> OnPlayerTargetReached;
    public static Action<OrbMovement, Enemy> OnEnemyTargetReached;
    public static Action<float> OnPlayerTakeDamage;
}
