using System.Collections;
using UnityEngine;

public class VoltixCamShaker : MonoBehaviour
{
    [SettingsHeader("imvoltix's shaking machine :skull:")]
    [Header("Shake Settings")]
    [SerializeField] private float defaultIntensity = 10f;
    [SerializeField] private float defaultDuration = 0.3f;
    [SerializeField] private float comboIntensity = 20f;
    [SerializeField] private float comboDuration = 0.5f;
    
    [Header("Target")]
    [SerializeField] private Transform shakeTarget; // Can be camera or UI panel
    
    private Vector3 originalPosition;
    private bool isShaking = false;

    void Start()
    {
        // If no target assigned, use this transform
        if (shakeTarget == null)
            shakeTarget = transform;
        
        // Store original position
        if (shakeTarget.GetComponent<RectTransform>() != null)
        {
            // For UI elements
            originalPosition = shakeTarget.GetComponent<RectTransform>().anchoredPosition;
        }
        else
        {
            // For world space objects (like camera)
            originalPosition = shakeTarget.localPosition;
        }
    }

    /// <summary>
    /// Trigger a normal shake effect
    /// </summary>
    public void ShakeNormal()
    {
        StartCoroutine(Shake(defaultIntensity, defaultDuration));
    }

    /// <summary>
    /// Trigger a combo shake effect (more intense)
    /// </summary>
    public void ShakeCombo()
    {
        StartCoroutine(Shake(comboIntensity, comboDuration));
    }

    /// <summary>
    /// Trigger a custom shake with specific parameters
    /// </summary>
    public void ShakeCustom(float intensity, float duration)
    {
        StartCoroutine(Shake(intensity, duration));
    }

    /// <summary>
    /// Stop any ongoing shake and reset position
    /// </summary>
    public void StopShake()
    {
        StopAllCoroutines();
        ResetPosition();
        isShaking = false;
    }

    private IEnumerator Shake(float intensity, float duration)
    {
        if (shakeTarget == null || isShaking) yield break;
        
        isShaking = true;
        float elapsed = 0f;
        
        RectTransform rectTransform = shakeTarget.GetComponent<RectTransform>();
        bool isUIElement = rectTransform != null;
        
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;
            
            Vector3 offset = new Vector3(x, y, 0);
            
            if (isUIElement)
            {
                // For UI elements
                rectTransform.anchoredPosition = (Vector2)originalPosition + new Vector2(x, y);
            }
            else
            {
                // For world space objects
                shakeTarget.localPosition = originalPosition + offset;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        ResetPosition();
        isShaking = false;
    }

    private void ResetPosition()
    {
        if (shakeTarget == null) return;
        
        RectTransform rectTransform = shakeTarget.GetComponent<RectTransform>();
        
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = originalPosition;
        }
        else
        {
            shakeTarget.localPosition = originalPosition;
        }
    }

    public bool IsShaking()
    {
        return isShaking;
    }
}