using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TouchCanvas : MonoBehaviour
{
    private GameObject leftTracker;
    private GameObject rightTracker;
    private Collider col;
    public Image fillImage;
    private Coroutine touchTimerCoroutine;
    public UnityEvent eventToTrigger;

    private const float DISTANCE_THRESHOLD = 0.2f;

    private void Start()
    {
        col = GetComponent<Collider>();
        leftTracker = PatternManager.instance.GetDevice(TrackingProfile.LeftArm);
        rightTracker = PatternManager.instance.GetDevice(TrackingProfile.RightArm);
    }

    private void OnTriggerEnter(Collider _collider)
    {
        if (touchTimerCoroutine != null) return;
        if (_collider.gameObject == leftTracker || _collider.gameObject == rightTracker) HndTouchEvent(_collider);
    }

    private void HndTouchEvent(Collider _collider)
    {
        if (touchTimerCoroutine == null) touchTimerCoroutine = StartCoroutine(TouchTimer(_collider));
    }

    private IEnumerator TouchTimer(Collider _collider)
    {
        float timeToWait = 2f;
        float timer = 0;
        while (timer < timeToWait)
        {
            if (Vector3.Distance(col.ClosestPoint(_collider.transform.position), _collider.transform.position) > DISTANCE_THRESHOLD)
            {
                touchTimerCoroutine = null;
                fillImage.fillAmount = 0;
                yield break;
            }
            timer += Time.deltaTime;
            fillImage.fillAmount = timer / timeToWait;
            yield return null;
        }
        eventToTrigger.Invoke();
    }

}
