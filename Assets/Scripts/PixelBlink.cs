using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PixelBlink : MonoBehaviour
{
    [SettingsHeader("Blink Effect")]
    [Tooltip("How many times to blink")]
    public int totalBlinks = 2;
    
    [Tooltip("Time between each blink")]
    public float blinkSpeed = 0.1f;
    
    [Tooltip("Start blinking on start")]
    public bool blinkOnStart = true;

    private CanvasGroup canvasGroup;

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (blinkOnStart)
        {
            StartBlinking();
        }
    }

    public void StartBlinking()
    {
        StartCoroutine(BlinkRoutine());
    }

    private IEnumerator BlinkRoutine()
    {
        for (int i = 0; i < totalBlinks; i++)
        {
            canvasGroup.alpha = 0;
            yield return new WaitForSeconds(blinkSpeed);
            
            canvasGroup.alpha = 1;
            yield return new WaitForSeconds(blinkSpeed);
        }
    }
}