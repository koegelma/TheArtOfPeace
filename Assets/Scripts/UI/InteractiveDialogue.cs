using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

[System.Serializable]
public class TextBlock
{
    [Header("General")]
    public string blockName;
    public TMP_FontAsset font;
    public FontStyles fontStyle;
    public int maxFontSize;
    [Header("Texts within this block")]
    public string[] texts;
    [Header("Block Forward / Backward interaction within this block")]
    public bool noForwardText;
    public bool noBackwardText;
    [Header("Block Forward / Backward interaction from this block")]
    public bool noForwardStep;
    public bool noBackwardStep;
    [Header("Event to trigger when reaching this block")]
    public UnityEvent onReach;
}

public class InteractiveDialogue : MonoBehaviour
{
    [Header("Setup")]
    public TextMeshProUGUI textElement;
    public TextMeshProUGUI nextTextIndicator;
    public GameObject leftTracker;
    public GameObject rightTracker;
    public GameObject uiHelperPrefab;
    private GameObject leftUIHelper;
    private GameObject rightUIHelper;
    private PatternRecognition leftPatternRecognition;
    private PatternRecognition rightPatternRecognition;
    [Header("Dialogue")]
    public TextBlock[] textBlocks;
    private int blockIndex;
    private int textIndex;
    private bool interactionBlocked;
    private float timeBetweenInteractions = 2f;

    private bool countPatternInterpolations = false;
    private int patternInterpolations = 0;
    private int patternInterpolationsToNextText;

    private void Start()
    {
        leftPatternRecognition = leftTracker.GetComponent<PatternRecognition>();
        rightPatternRecognition = rightTracker.GetComponent<PatternRecognition>();

        interactionBlocked = false;

        for (int i = 0; i <= textBlocks.Length - 1; i++)
        {
            for (int j = 0; j <= textBlocks[i].texts.Length - 1; j++)
            {
                textBlocks[i].texts[j] = textBlocks[i].texts[j].Replace("NEWLINE", "\n");
            }
        }

        blockIndex = 0;
        textIndex = 0;
        UpdateText();

        StartCoroutine(InstantiateUIHelpers());
    }

    private void OnEnable()
    {
        Actions.OnDialogueToggle += ToggleDialogue;
        Actions.OnDialogueNext += NextText;
        Actions.OnDialoguePrevious += PreviousText;
        Actions.OnPatternRecognized += PatternRecognized;
        Actions.OnGameLost += OnGameLost;
        Actions.OnGameWon += OnGameWon;
    }

    private void OnDisable()
    {
        Actions.OnDialogueToggle -= ToggleDialogue;
        Actions.OnDialogueNext -= NextText;
        Actions.OnDialoguePrevious -= PreviousText;
        Actions.OnPatternRecognized -= PatternRecognized;
        Actions.OnGameLost -= OnGameLost;
        Actions.OnGameWon -= OnGameWon;
    }

    private void Update()
    {
        if (interactionBlocked || leftUIHelper == null || rightUIHelper == null) return;
        UpdateHelperStatus();
        if (AssertHelper(leftUIHelper, leftPatternRecognition))
        {
            PreviousText();
            StartCoroutine(BlockInteraction(timeBetweenInteractions, _condition: !AssertHelperPosition(leftUIHelper, leftPatternRecognition)));
        }
        if (AssertHelper(rightUIHelper, rightPatternRecognition))
        {
            NextText();
            StartCoroutine(BlockInteraction(timeBetweenInteractions, _condition: !AssertHelperPosition(rightUIHelper, rightPatternRecognition)));
        }
    }

    private void ToggleDialogue(bool _enabled)
    {
        textElement.gameObject.SetActive(_enabled);
        if (_enabled)
        {
            UpdateHelperStatus();
            interactionBlocked = false;
        }
        else
        {
            leftUIHelper.SetActive(false);
            rightUIHelper.SetActive(false);
            interactionBlocked = true;
        }
    }

    private IEnumerator InstantiateUIHelpers()
    {
        yield return new WaitForSeconds(3f);
        leftUIHelper = Instantiate(uiHelperPrefab, Vector3.zero, Quaternion.identity);
        leftUIHelper.GetComponentInChildren<HelperMovement>().SetProfile(TrackingProfile.LeftArm);
        rightUIHelper = Instantiate(uiHelperPrefab, Vector3.zero, Quaternion.identity);
        rightUIHelper.GetComponentInChildren<HelperMovement>().SetProfile(TrackingProfile.RightArm);
        leftUIHelper.SetActive(false);
    }

    private bool AssertHelper(GameObject _helper, PatternRecognition _patternRecognition)
    {
        return _helper != null && _helper.activeSelf && AssertHelperPosition(_helper, _patternRecognition);
    }

    /// <summary>
    /// returns true if the corresponding tracker is within tolerance range of the helper
    /// </summary>
    private bool AssertHelperPosition(GameObject _helper, PatternRecognition _patternRecognition)
    {
        return _patternRecognition.DistanceOutOfToleranceRange(_helper.transform.GetChild(0).position, PatternManager.instance.startPose.tolerance) <= 0;
    }

    private void NextText()
    {
        textIndex++;
        if (textIndex >= textBlocks[blockIndex].texts.Length)
        {
            if (blockIndex >= textBlocks.Length - 1)
            {
                textIndex--;
                return;
            }
            blockIndex++;
            textIndex = 0;

            if (textBlocks[blockIndex].onReach != null) textBlocks[blockIndex].onReach.Invoke();
        }
        UpdateText();
    }

    private void PreviousText()
    {
        textIndex--;
        if (textIndex < 0)
        {
            if (blockIndex == 0 || textBlocks[blockIndex - 1].noBackwardStep)
            {
                textIndex++;
                return;
            }
            blockIndex--;
            textIndex = textBlocks[blockIndex].texts.Length - 1;
        }
        UpdateText();
    }

    private void UpdateText()
    {
        textElement.font = textBlocks[blockIndex].font;
        textElement.fontStyle = textBlocks[blockIndex].fontStyle;
        textElement.fontSizeMax = textBlocks[blockIndex].maxFontSize;
        textElement.text = textBlocks[blockIndex].texts[textIndex];
    }

    private IEnumerator BlockInteraction(float _time = 0, bool _condition = false)
    {
        interactionBlocked = true;
        leftUIHelper.SetActive(false);
        rightUIHelper.SetActive(false);
        if (_time > 0) yield return new WaitForSeconds(_time);

        else if (_condition)
        {
            while (_condition)
            {
                yield return null;
            }
        }
        interactionBlocked = false;
        UpdateHelperStatus();
    }

    private void UpdateHelperStatus()
    {
        leftUIHelper.SetActive(blockIndex > 0 && !textBlocks[blockIndex].noBackwardText && !(textIndex == 0 && textBlocks[blockIndex].noBackwardStep));
        rightUIHelper.SetActive(blockIndex < textBlocks.Length - 1 && !textBlocks[blockIndex].noForwardText && !(textIndex == textBlocks[blockIndex].texts.Length - 1 && textBlocks[blockIndex].noForwardStep));
    }

    public void ToggleGameObject(GameObject _gameObject)
    {
        _gameObject.SetActive(!_gameObject.activeSelf);
    }

    public void StartTimerToNextText(float _time)
    {
        StartCoroutine(TimerToNextText(_time));
    }

    private IEnumerator TimerToNextText(float _time)
    {
        nextTextIndicator.text = _time.ToString("F0") + "s";

        float time = _time;
        while (time > 0)
        {
            time -= Time.deltaTime;
            nextTextIndicator.text = time.ToString("F0") + "s";
            yield return null;
        }
        nextTextIndicator.text = "";
        NextText();
    }

    public void MoveUpwards(float _upwardsFactor)
    {
        transform.position += Vector3.up * _upwardsFactor;
    }

    public void MoveDownwards(float _downwardsFactor)
    {
        transform.position += Vector3.down * _downwardsFactor;
    }

    public void ActivateStartPositionWait()
    {
        StartCoroutine(WaitForStartPositionReached());
    }

    public IEnumerator WaitForStartPositionReached()
    {
        nextTextIndicator.text = "Warte auf Start-Position";
        yield return new WaitUntil(() => PatternManager.instance.recognitionActive);
        nextTextIndicator.text = "";
        NextText();
    }

    public void TogglePatternInterpolationCounting(int _patternInterpolationsToNextText)
    {
        countPatternInterpolations = true;
        patternInterpolationsToNextText = _patternInterpolationsToNextText;
        patternInterpolations = 0;
        nextTextIndicator.text = "Noch " + patternInterpolations + " mal";
    }

    private void PatternRecognized(TrackingProfile _profile, PatternDictionary _patternDictionary)
    {
        if (!countPatternInterpolations) return;
        patternInterpolations++;
        float completeInterpolations = patternInterpolations / PatternManager.instance.devices.Count;
        nextTextIndicator.text = "Noch " + (patternInterpolationsToNextText - completeInterpolations) + " mal";
        if (completeInterpolations >= patternInterpolationsToNextText)
        {
            nextTextIndicator.text = "";
            patternInterpolations = 0;
            countPatternInterpolations = false;
            NextText();
        }
    }

    private void OnGameLost()
    {
        blockIndex = textBlocks.Length - 1;
        textElement.font = textBlocks[blockIndex].font;
        textElement.fontStyle = textBlocks[blockIndex].fontStyle;
        textElement.fontSizeMax = textBlocks[blockIndex].maxFontSize;
        textElement.text = "Game Over!";
        if (textBlocks[blockIndex].onReach != null) textBlocks[blockIndex].onReach.Invoke();
    }

    private void OnGameWon()
    {
        blockIndex = textBlocks.Length - 1;
        textElement.font = textBlocks[blockIndex].font;
        textElement.fontStyle = textBlocks[blockIndex].fontStyle;
        textElement.fontSizeMax = textBlocks[blockIndex].maxFontSize;
        textElement.text = "You Won!";
        if (textBlocks[blockIndex].onReach != null) textBlocks[blockIndex].onReach.Invoke();
    }
}
