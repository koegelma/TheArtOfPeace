using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyMe : MonoBehaviour
{
    /// <summary>
    /// Destroys the game object after the specified amount of seconds.
    /// </summary>
    public float seconds = 1f;
    private void Start()
    {
        StartCoroutine(DestroyMeAfterSeconds());
    }

    private IEnumerator DestroyMeAfterSeconds()
    {
        yield return new WaitForSeconds(seconds);
        Destroy(gameObject);
    }
}
