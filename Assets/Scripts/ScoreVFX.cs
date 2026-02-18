using System;
using UnityEngine;
using TMPro;
using System.Collections;

public class ScoreVFX : MonoBehaviour
{
    [CoolHeader("SCORE TEXT ANIMATION")]
    [Header("Animation Settings")]
    public AnimationType animationType = AnimationType.FlyFadeUp;
    public bool playOnAwake = true;
    public float animationDuration = 1.5f;
    public float moveDistance = 100f;
    
    [Header("Scale Settings")]
    public float startScale = 1f;
    public float endScale = 1f;
    
    private TextMeshProUGUI tmpText;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 startPos;
    private Coroutine currentAnimation;
    
    public enum AnimationType
    {
        FlyFadeUp,
        PopFadeOut,
        BounceUp,
        ScaleAndFade,
        WobbleFade,
        SlideLeft,
        SlideRight
    }
    
    void Awake()
    {
        tmpText = GetComponent<TextMeshProUGUI>();
        rectTransform = GetComponent<RectTransform>();
        
        // Add canvas group if not exists
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Configure text alignment and auto-size
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.enableAutoSizing = true;
        tmpText.fontSizeMin = 10;
        tmpText.fontSizeMax = 100;
        
        startPos = rectTransform.anchoredPosition;
        
        if (playOnAwake && gameObject.activeInHierarchy)
        {
            Play();
        }
    }

    private void OnEnable()
    {
        if (playOnAwake && gameObject.activeInHierarchy)
        {
            Play();
        }
    }

    public void Play()
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }
        
        // Reset transform
        rectTransform.anchoredPosition = startPos;
        rectTransform.localScale = Vector3.one * startScale;
        canvasGroup.alpha = 1f;
        
        switch (animationType)
        {
            case AnimationType.FlyFadeUp:
                currentAnimation = StartCoroutine(FlyFadeUpAnim());
                break;
            case AnimationType.PopFadeOut:
                currentAnimation = StartCoroutine(PopFadeOutAnim());
                break;
            case AnimationType.BounceUp:
                currentAnimation = StartCoroutine(BounceUpAnim());
                break;
            case AnimationType.ScaleAndFade:
                currentAnimation = StartCoroutine(ScaleAndFadeAnim());
                break;
            case AnimationType.WobbleFade:
                currentAnimation = StartCoroutine(WobbleFadeAnim());
                break;
            case AnimationType.SlideLeft:
                currentAnimation = StartCoroutine(SlideLeftAnim());
                break;
            case AnimationType.SlideRight:
                currentAnimation = StartCoroutine(SlideRightAnim());
                break;
        }
    }
    
    IEnumerator FlyFadeUpAnim()
    {
        Vector3 endPos = startPos + Vector3.up * moveDistance;
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            
            rectTransform.anchoredPosition = Vector3.Lerp(startPos, endPos, t);
            canvasGroup.alpha = 1f - t;
            
            yield return null;
        }
        
    }
    
    IEnumerator PopFadeOutAnim()
    {
        float elapsed = 0f;
        float popDuration = animationDuration * 0.2f;
        
        // Pop phase
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popDuration;
            float scale = Mathf.Lerp(startScale, startScale * 1.3f, t);
            rectTransform.localScale = Vector3.one * scale;
            yield return null;
        }
        
        // Fade phase
        elapsed = 0f;
        float fadeDuration = animationDuration * 0.8f;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            
            canvasGroup.alpha = 1f - t;
            float scale = Mathf.Lerp(startScale * 1.3f, endScale, t);
            rectTransform.localScale = Vector3.one * scale;
            
            yield return null;
        }
        
    }
    
    IEnumerator BounceUpAnim()
    {
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            
            float bounceHeight = Mathf.Sin(t * Mathf.PI) * moveDistance;
            rectTransform.anchoredPosition = startPos + Vector3.up * bounceHeight;
            canvasGroup.alpha = 1f - t;
            
            yield return null;
        }
        
    }
    
    IEnumerator ScaleAndFadeAnim()
    {
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            
            float scale = Mathf.Lerp(startScale, endScale * 2f, t);
            rectTransform.localScale = Vector3.one * scale;
            canvasGroup.alpha = 1f - t;
            
            yield return null;
        }
        
    }
    
    IEnumerator WobbleFadeAnim()
    {
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            
            float wobble = Mathf.Sin(t * Mathf.PI * 4f) * 20f * (1f - t);
            rectTransform.anchoredPosition = startPos + Vector3.right * wobble + Vector3.up * moveDistance * t;
            canvasGroup.alpha = 1f - t;
            
            yield return null;
        }
        
    }
    
    IEnumerator SlideLeftAnim()
    {
        Vector3 endPos = startPos + Vector3.left * moveDistance + Vector3.up * (moveDistance * 0.3f);
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            
            rectTransform.anchoredPosition = Vector3.Lerp(startPos, endPos, t);
            canvasGroup.alpha = 1f - t;
            
            yield return null;
        }
        
    }
    
    IEnumerator SlideRightAnim()
    {
        Vector3 endPos = startPos + Vector3.right * moveDistance + Vector3.up * (moveDistance * 0.3f);
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            
            rectTransform.anchoredPosition = Vector3.Lerp(startPos, endPos, t);
            canvasGroup.alpha = 1f - t;
            
            yield return null;
        }
        
    }
}