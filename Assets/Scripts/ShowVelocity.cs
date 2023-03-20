using UnityEngine;
using TMPro;

public class ShowVelocity : MonoBehaviour
{
    public TrackingProfile profile;
    private TextMeshProUGUI velText;

    private void Start()
    {
        velText = transform.GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Update()
    {
        if (profile == TrackingProfile.Waist) return;
        velText.text = PatternManager.instance.GetDeviceVelocity(profile).magnitude.ToString();
    }
}
