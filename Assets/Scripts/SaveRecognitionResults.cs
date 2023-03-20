using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

[System.Serializable]
public class TestResult
{
    [SerializeField] public string dateAndTime;
    [SerializeField] public string testPersonName;
    [SerializeField] public string testPatternName;
    [SerializeField] public int testRound;

    [SerializeField] public float euclideanDifference;
    [SerializeField] public float euclideanDifferenceRightArm;
    [SerializeField] public float euclideanDifferenceLeftArm;
    [SerializeField] public float euclideanDifferenceRightLeg;
    [SerializeField] public float euclideanDifferenceLeftLeg;

    [SerializeField] public float dtwDifference;
    [SerializeField] public float dtwDifferenceRightArm;
    [SerializeField] public float dtwDifferenceLeftArm;
    [SerializeField] public float dtwDifferenceRightLeg;
    [SerializeField] public float dtwDifferenceLeftLeg;

    public TestResult(string _dateAndTime, string _testPatternName, string _testPersonName, int _testRound,
                        float _euclideanDifference, float _euclideanDifferenceRightArm, float _euclideanDifferenceLeftArm, float _euclideanDifferenceRightLeg, float _euclideanDifferenceLeftLeg,
                        float _dtwDifference, float _dtwDifferenceRightArm, float _dtwDifferenceLeftArm, float _dtwDifferenceRightLeg, float _dtwDifferenceLeftLeg)
    {
        dateAndTime = _dateAndTime;
        testPatternName = _testPatternName;
        testPersonName = _testPersonName;
        testRound = _testRound;

        euclideanDifference = _euclideanDifference;
        euclideanDifferenceRightArm = _euclideanDifferenceRightArm;
        euclideanDifferenceLeftArm = _euclideanDifferenceLeftArm;
        euclideanDifferenceRightLeg = _euclideanDifferenceRightLeg;
        euclideanDifferenceLeftLeg = _euclideanDifferenceLeftLeg;

        dtwDifference = _dtwDifference;
        dtwDifferenceRightArm = _dtwDifferenceRightArm;
        dtwDifferenceLeftArm = _dtwDifferenceLeftArm;
        dtwDifferenceRightLeg = _dtwDifferenceRightLeg;
        dtwDifferenceLeftLeg = _dtwDifferenceLeftLeg;
    }
}

public class SaveRecognitionResults : MonoBehaviour
{
    [SerializeField] public SerializableList<TestResult> testResults = new SerializableList<TestResult>();
    public string testPersonName;
    public int testRound = 0;
    private string testPatternName;
    private string dateAndTime { get { return System.DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"); } }
    private string saveFile;

    private void Awake()
    {
        string filePath = Path.Combine(Application.dataPath, "Resources/UserTestResults/" + testPersonName + "_TestResult.json");
        saveFile = filePath;
        testResults.list = ReadFile();
    }


    private void OnEnable()
    {
        Actions.OnRecognition += SaveResults;
    }

    private void OnDisable()
    {
        Actions.OnRecognition -= SaveResults;
    }


    private void SaveResults(PatternDictionary patternDictionary)
    {
        testPatternName = patternDictionary.pattern.name;
        testRound++;

        TestResult newTestResult = new TestResult(dateAndTime, testPatternName, testPersonName, testRound,
            patternDictionary.GetEuclideanAverage(), patternDictionary.euclideanMap[TrackingProfile.RightArm], patternDictionary.euclideanMap[TrackingProfile.LeftArm], patternDictionary.euclideanMap[TrackingProfile.RightLeg], patternDictionary.euclideanMap[TrackingProfile.LeftLeg],
            patternDictionary.GetDTWAverage(), patternDictionary.dtwMap[TrackingProfile.RightArm], patternDictionary.dtwMap[TrackingProfile.LeftArm], patternDictionary.dtwMap[TrackingProfile.RightLeg], patternDictionary.dtwMap[TrackingProfile.LeftLeg]);

        testResults.list.Add(newTestResult);
        string json = JsonUtility.ToJson(testResults, true);
        WriteFile(json);
    }

    /// <summary>
    /// Read patterns from file in project folder.
    /// </summary>
    public List<TestResult> ReadFile()
    {
        if (!File.Exists(saveFile))
        {
            Debug.Log("No savefile found, creating new file.");
            return new List<TestResult>();
        }

        using (FileStream stream = File.Open(saveFile, FileMode.Open))
        {
            using (StreamReader reader = new StreamReader(stream))
            {
                string json = reader.ReadToEnd();
                return JsonUtility.FromJson<SerializableList<TestResult>>(json).list;
            }
        }
    }

    /* /// <summary>
    /// Read patterns from file after building the application.
    /// </summary>
    public List<TestResult> LoadFromAssets()
    {
        // Load the "patterns.json" file from the "Resources" folder
        TextAsset jsonAsset = Resources.Load<TextAsset>("patterns.json");
        // Use the JsonUtility class to parse the JSON string into a Pattern object
        return JsonUtility.FromJson<SerializableList<TestResult>>(jsonAsset.text).list;
    } */

    /// <summary>
    /// Write patterns to file.
    /// </summary>
    public void WriteFile(string _json)
    {
        //string json = JsonUtility.ToJson(savedPatterns, true);
        File.WriteAllText(saveFile, _json);
        Debug.Log("Results saved to " + saveFile);
    }
}
