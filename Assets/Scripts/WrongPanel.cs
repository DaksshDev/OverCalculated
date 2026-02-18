using UnityEngine;
using System.Collections;

public class WrongPanel : MonoBehaviour
{
    [CoolHeader("WRONG STAMP ANIMATION")]
    [Header("Animation Settings")]
    [SerializeField] private float animDuration = 0.5f;
    [SerializeField] private float startScale = 2f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Camera Shake")]
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeStrength = 0.5f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip stampSFX;
    [SerializeField] private AudioSource audioSource;
    
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Coroutine animCoroutine;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        
        // Add CanvasGroup if it doesn't exist
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        // Get or create AudioSource
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        originalScale = rectTransform.localScale;
        
        // Set default bounce curve if not customized
        if (scaleCurve.keys.Length <= 2)
        {
            scaleCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.6f, 1.1f),
                new Keyframe(0.8f, 0.95f),
                new Keyframe(1f, 1f)
            );
        }
    }
    
    private void OnEnable()
    {
        if (animCoroutine != null)
            StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(PlayStampAnimation());
    }
    
    private IEnumerator PlayStampAnimation()
    {
        // Set initial state
        rectTransform.localScale = originalScale * startScale;
        canvasGroup.alpha = 0f;
        
        float elapsed = 0f;
        bool soundPlayed = false;
        bool shakeTriggered = false;
        
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / animDuration;
            
            // Apply scale with curve
            float curveValue = scaleCurve.Evaluate(progress);
            rectTransform.localScale = Vector3.Lerp(originalScale * startScale, originalScale, curveValue);
            
            // Quick fade in (first 30% of animation)
            if (progress < 0.3f)
                canvasGroup.alpha = progress / 0.3f;
            else
                canvasGroup.alpha = 1f;
            
            // Play sound and shake at 80% progress
            if (progress >= 0.8f && !soundPlayed)
            {
                soundPlayed = true;
                
                if (stampSFX != null && audioSource != null)
                    audioSource.PlayOneShot(stampSFX);
            }
            
            if (progress >= 0.8f && !shakeTriggered)
            {
                shakeTriggered = true;
                StartCoroutine(ShakeCamera());
            }
            
            yield return null;
        }
        
        // Ensure final state
        rectTransform.localScale = originalScale;
        canvasGroup.alpha = 1f;
    }
    
    private IEnumerator ShakeCamera()
    {
        // Get the canvas and its camera
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null || canvas.worldCamera == null) yield break;
        
        Camera uiCam = canvas.worldCamera;
        Vector3 originalPos = uiCam.transform.localPosition;
        float elapsed = 0f;
        
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float strength = shakeStrength * (1f - elapsed / shakeDuration); // Decay over time
            
            float x = Random.Range(-1f, 1f) * strength;
            float y = Random.Range(-1f, 1f) * strength;
            
            uiCam.transform.localPosition = originalPos + new Vector3(x, y, 0);
            
            yield return null;
        }
        
        uiCam.transform.localPosition = originalPos;
    }
    
    private void OnDisable()
    {
        // Stop animations when disabled
        if (animCoroutine != null)
            StopCoroutine(animCoroutine);
        
        // Reset to original state
        rectTransform.localScale = originalScale;
        canvasGroup.alpha = 1f;
    }
}