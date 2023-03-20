using System.Collections;
using UnityEngine;
using TMPro;
using System;

public class SimilarityVisualization : MonoBehaviour
{
    public TrackingProfile profile;
    public PatternDictionary patternDictionary;
    private PatternManager manager;
    private Gradient gradient;
    private GradientColorKey[] colorKey;
    private GradientAlphaKey[] alphaKey;
    private Material material;
    public Material redEmMat;
    public Material blueEmMat;
    public TextMeshProUGUI simText;
    public TextMeshProUGUI patternText;
    private bool initialized;

    private void Awake()
    {
        manager = PatternManager.instance;
    }

    private void Start()
    {
        StartCoroutine(Initialize(0.1f));
        simText.text = "";
    }

    private void OnEnable()
    {
        Actions.OnRecognition += PatternRecognized;
        Actions.OnRecognitionReset += Reset;
    }

    private void OnDisable()
    {
        Actions.OnRecognition -= PatternRecognized;
        Actions.OnRecognitionReset -= Reset;
    }

    private void Update()
    {
        if (!initialized) return;
        if (patternText != null) patternText.text = "Take Start Pose";
        if (!manager.recognitionActive) return;
        if (patternText != null) patternText.text = "Recognition Active";
        if (manager.patternWithLowestEuclideanDifference == null) return;
        if (patternText != null) patternText.text = "Pattern Dictionary Updated";
        patternDictionary = manager.patternWithLowestEuclideanDifference;

        if (profile == TrackingProfile.Waist)
        {
            UpdateParticleColor(patternDictionary.EuclideanDifference);
            float dtwAverage = patternDictionary.GetDTWAverage();
            if (manager.useDTW)
            {
                simText.text = "DTW: " + dtwAverage.ToString("F2");
            }
            else
            {
                simText.text = "ED: " + patternDictionary.EuclideanDifference.ToString("F2") + Environment.NewLine + "DTW: " + dtwAverage.ToString("F2"); // 2 decimal places
            }
            patternText.text = patternDictionary.pattern.name.ToString();
            return;
        }

        UpdateParticleColor(patternDictionary.euclideanMap[profile]);

        if (manager.useDTW)
        {
            simText.text = "DTW: " + patternDictionary.dtwMap[profile].ToString("F2");
        }
        else
        {
            simText.text = "ED: " + patternDictionary.euclideanMap[profile].ToString("F2") + Environment.NewLine + "DTW: " + patternDictionary.dtwMap[profile].ToString("F2"); // 2 decimal places
        }
    }

    private IEnumerator Initialize(float _seconds)
    {
        yield return new WaitForSeconds(_seconds);
        gradient = new Gradient();
        colorKey = new GradientColorKey[2];
        colorKey[0].color = redEmMat.color;
        colorKey[0].time = 0.0f;
        colorKey[1].color = blueEmMat.color;
        colorKey[1].time = 1.0f;

        alphaKey = new GradientAlphaKey[2];
        alphaKey[0].alpha = 1.0f;
        alphaKey[0].time = 0.0f;
        alphaKey[1].alpha = 1.0f;
        alphaKey[1].time = 1.0f;

        gradient.SetKeys(colorKey, alphaKey);
        material = transform.GetComponent<MeshRenderer>().material;
        UpdateParticleColor(0.5f);
        initialized = true;
    }

    public void UpdateParticleColor(float _euclidValue)
    {
        float t = _euclidValue;

        if (t < 0) t = 0;

        Color color = gradient.Evaluate(t);
        material.color = color;
    }

    private void PatternRecognized(PatternDictionary _patternDictionary)
    {
        initialized = false;
        Color color = Color.green;
        transform.GetComponent<MeshRenderer>().material.color = color;
        StartCoroutine(Initialize(1f));
    }

    private void Reset()
    {
        if (patternText != null) patternText.text = "";
        gradient = new Gradient();
        colorKey = new GradientColorKey[2];
        colorKey[0].color = redEmMat.color;
        colorKey[0].time = 0.0f;
        colorKey[1].color = blueEmMat.color;
        colorKey[1].time = 1.0f;

        alphaKey = new GradientAlphaKey[2];
        alphaKey[0].alpha = 1.0f;
        alphaKey[0].time = 0.0f;
        alphaKey[1].alpha = 1.0f;
        alphaKey[1].time = 1.0f;

        gradient.SetKeys(colorKey, alphaKey);
        material = transform.GetComponent<MeshRenderer>().material;
        UpdateParticleColor(0.5f);
        initialized = true;
    }

}
