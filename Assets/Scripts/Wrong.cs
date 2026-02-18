using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Wrong : MonoBehaviour
{
    [CoolHeader("WRONG MANAGER")]
    [Header("UI References")]
    [SerializeField] private GameObject wrongOverlay;
    [SerializeField] private CanvasGroup wrongOverlayCanvasGroup;
    [SerializeField] private GameObject wrongPanel;
    [SerializeField] private TextMeshProUGUI insultText;
    [SerializeField] private GameObject heartsParent;
    [SerializeField] private Image[] wrongPanelHearts;
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private Button continueButton;
    
    [Header("Post Processing")]
    [SerializeField] private Volume postProcessVolume;
    [SerializeField] private float chromaticAberrationStrength = 0.8f;
    [SerializeField] private float vignetteIntensityAdd = 0.3f;
    [SerializeField] private float postExposureDecrease = -3f; // NEW: Exposure decrease amount
    [SerializeField] private float postProcessingSpeed = 1f;
    
    [Header("Animation Settings")]
    [SerializeField] private float slowMotionScale = 0.3f;
    [SerializeField] private float timeSlowSpeed = 2f;
    [SerializeField] private float overlayFadeSpeed = 1.5f;
    [SerializeField] private float overlayDisplayTime = 3f;
    [SerializeField] private float heartDeductAnimTime = 0.8f;
    [SerializeField] private float delayBeforeOverlay = 0.5f;
    
    [Header("Insult Settings")]
    [SerializeField] private string[] insults = {
        "Skill issue?",
        "Idk bro try again I guess?",
        "TRY AGAIN!",
        "ouch!",
        "uh oh!",
        "GET OU-",
        "Sybau",
        ":(",
        "Math aint mathing"
    };
    
    private GameManager gameManager;
    private ChromaticAberration chromaticAberration;
    private Vignette vignette;
    private ColorAdjustments colorAdjustments; // NEW
    private float originalVignetteIntensity = 0f;
    private float originalPostExposure = 0f; // NEW
    private bool isPlayingAnimation = false;
    
    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        
        // Setup post processing effects
        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            postProcessVolume.profile.TryGet(out chromaticAberration);
            postProcessVolume.profile.TryGet(out vignette);
            postProcessVolume.profile.TryGet(out colorAdjustments); // NEW
            
            // Store original vignette intensity
            if (vignette != null)
            {
                originalVignetteIntensity = vignette.intensity.value;
            }
            
            // Store original post exposure
            if (colorAdjustments != null)
            {
                originalPostExposure = colorAdjustments.postExposure.value;
            }
        }
        
        // Setup continue button
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }
        
        // Hide panels initially
        if (wrongOverlay != null)
            wrongOverlay.SetActive(false);
        if (wrongPanel != null)
            wrongPanel.SetActive(false);
        if (gameOverText != null)
            gameOverText.gameObject.SetActive(false);
    }
    
    public void PlayWrongAnswerSequence(int currentHearts)
    {
        if (isPlayingAnimation)
            return;
        
        StartCoroutine(WrongAnswerSequence(currentHearts));
    }
    
    IEnumerator WrongAnswerSequence(int heartsLeft)
    {
        isPlayingAnimation = true;
        
        // Phase 1: Apply post processing and slow time, THEN show overlay after delay
        yield return StartCoroutine(SlowTimeAndApplyPostProcessing());
        
        // Phase 2: Display overlay for set time
        yield return new WaitForSecondsRealtime(overlayDisplayTime);
        
        // Phase 3: Fade out overlay
        yield return StartCoroutine(FadeOutOverlay());
        
        // Phase 4: Show wrong panel with hearts and insult
        yield return StartCoroutine(ShowWrongPanel(heartsLeft));
        
        // Wait for continue button click (pauses here)
    }
    
    IEnumerator SlowTimeAndApplyPostProcessing()
    {
        float elapsed = 0f;
        float duration = 1.5f;
        float totalDuration = duration + delayBeforeOverlay;
        
        bool overlayActivated = false;
        
        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            
            // Time scale progress (faster)
            float timeT = Mathf.Clamp01(elapsed / (duration * 0.6f));
            timeT = Mathf.SmoothStep(0f, 1f, timeT);
            Time.timeScale = Mathf.Lerp(1f, slowMotionScale, timeT);
            
            // POST EXPOSURE - Starts first, faster than everything (NEW)
            float exposureT = Mathf.Clamp01(elapsed / (duration * 0.4f)); // Faster than chromatic
            exposureT = Mathf.SmoothStep(0f, 1f, exposureT);
            if (colorAdjustments != null)
            {
                colorAdjustments.postExposure.value = Mathf.Lerp(
                    originalPostExposure,
                    originalPostExposure + postExposureDecrease,
                    exposureT
                );
            }
            
            // CHROMATIC ABERRATION - Starts after exposure
            float chromaticDelay = duration * 0.2f;
            float chromaticT = Mathf.Clamp01((elapsed - chromaticDelay) / (duration * 0.6f));
            chromaticT = Mathf.SmoothStep(0f, 1f, chromaticT);
            if (chromaticAberration != null)
            {
                chromaticAberration.intensity.value = Mathf.Lerp(0f, chromaticAberrationStrength, chromaticT);
            }
            
            // VIGNETTE - Starts last, smooth fade in
            float vignetteDelay = duration * 0.3f;
            float vignetteT = Mathf.Clamp01((elapsed - vignetteDelay) / (duration * 0.7f));
            vignetteT = Mathf.SmoothStep(0f, 1f, vignetteT);
            if (vignette != null)
            {
                vignette.intensity.value = Mathf.Lerp(
                    originalVignetteIntensity, 
                    originalVignetteIntensity + vignetteIntensityAdd, 
                    vignetteT
                );
            }
            
            // Activate overlay after delay
            if (!overlayActivated && elapsed >= delayBeforeOverlay)
            {
                overlayActivated = true;
                if (wrongOverlay != null)
                {
                    wrongOverlay.SetActive(true);
                    if (wrongOverlayCanvasGroup != null)
                        wrongOverlayCanvasGroup.alpha = 0f;
                }
            }
            
            // Fade in overlay if active
            if (overlayActivated && wrongOverlayCanvasGroup != null)
            {
                float overlayElapsed = elapsed - delayBeforeOverlay;
                float overlayDuration = totalDuration - delayBeforeOverlay;
                float overlayT = Mathf.Clamp01(overlayElapsed / overlayDuration);
                overlayT = Mathf.SmoothStep(0f, 1f, overlayT);
                wrongOverlayCanvasGroup.alpha = overlayT;
            }
            
            yield return null;
        }
        
        // Ensure final values
        Time.timeScale = slowMotionScale;
        if (wrongOverlayCanvasGroup != null)
            wrongOverlayCanvasGroup.alpha = 1f;
        if (chromaticAberration != null)
            chromaticAberration.intensity.value = chromaticAberrationStrength;
        if (vignette != null)
            vignette.intensity.value = originalVignetteIntensity + vignetteIntensityAdd;
        if (colorAdjustments != null)
            colorAdjustments.postExposure.value = originalPostExposure + postExposureDecrease;
    }
    
    IEnumerator FadeOutOverlay()
    {
        float elapsed = 0f;
        float duration = 1f / overlayFadeSpeed;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            
            if (wrongOverlayCanvasGroup != null)
            {
                wrongOverlayCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            }
            
            yield return null;
        }
        
        if (wrongOverlay != null)
            wrongOverlay.SetActive(false);
    }
    
    IEnumerator ShowWrongPanel(int heartsLeft)
    {
        if (wrongPanel == null)
            yield break;
        
        wrongPanel.SetActive(true);
        
        // Set random insult
        if (insultText != null)
        {
            insultText.text = insults[Random.Range(0, insults.Length)];
        }
        
        // Setup hearts display
        if (wrongPanelHearts != null && wrongPanelHearts.Length > 0)
        {
            for (int i = 0; i < wrongPanelHearts.Length; i++)
            {
                wrongPanelHearts[i].gameObject.SetActive(i < heartsLeft);
            }
        }
        
        // Check if this is game over
        bool isGameOver = heartsLeft <= 1;
        
        if (isGameOver)
        {
            if (heartsParent != null)
                heartsParent.SetActive(false);
            if (gameOverText != null)
                gameOverText.gameObject.SetActive(true);
        }
        else
        {
            if (heartsParent != null)
                heartsParent.SetActive(true);
            if (gameOverText != null)
                gameOverText.gameObject.SetActive(false);
        }
        
        // Animate heart deduction
        yield return StartCoroutine(AnimateHeartDeduction(heartsLeft));
        
        // Pause the game
        Time.timeScale = 0f;
        
        // Show continue button
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
            continueButton.interactable = true;
        }
    }
    
    IEnumerator AnimateHeartDeduction(int heartsLeft)
    {
        if (wrongPanelHearts == null || heartsLeft <= 0 || heartsLeft > wrongPanelHearts.Length)
            yield break;
        
        Image heartToDeduct = wrongPanelHearts[heartsLeft - 1];
        
        if (heartToDeduct == null)
            yield break;
        
        float elapsed = 0f;
        Vector3 originalScale = heartToDeduct.transform.localScale;
        Color originalColor = heartToDeduct.color;
        
        while (elapsed < heartDeductAnimTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / heartDeductAnimTime;
            
            heartToDeduct.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            
            Color newColor = originalColor;
            newColor.a = Mathf.Lerp(1f, 0f, t);
            heartToDeduct.color = newColor;
            
            yield return null;
        }
        
        heartToDeduct.gameObject.SetActive(false);
        heartToDeduct.transform.localScale = originalScale;
        heartToDeduct.color = originalColor;
    }
    
    public void OnContinueClicked()
    {
        StartCoroutine(ContinueGame());
    }
    
    IEnumerator ContinueGame()
    {
        if (continueButton != null)
        {
            continueButton.interactable = false;
        }
    
        if (wrongPanel != null)
            wrongPanel.SetActive(false);
    
        // Reset post processing effects smoothly
        float elapsed = 0f;
        float duration = 0.8f;
    
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0f, 1f, t);
        
            // Reset post exposure (NEW)
            if (colorAdjustments != null)
            {
                colorAdjustments.postExposure.value = Mathf.Lerp(
                    originalPostExposure + postExposureDecrease,
                    originalPostExposure,
                    t
                );
            }
        
            // Reset chromatic aberration
            if (chromaticAberration != null)
            {
                chromaticAberration.intensity.value = Mathf.Lerp(chromaticAberrationStrength, 0f, t);
            }
        
            // Reset vignette
            if (vignette != null)
            {
                vignette.intensity.value = Mathf.Lerp(
                    originalVignetteIntensity + vignetteIntensityAdd,
                    originalVignetteIntensity,
                    t
                );
            }
            
            yield return null;
        }
    
        // Ensure effects are reset
        if (colorAdjustments != null)
            colorAdjustments.postExposure.value = originalPostExposure;
        if (chromaticAberration != null)
            chromaticAberration.intensity.value = 0f;
        if (vignette != null)
            vignette.intensity.value = originalVignetteIntensity;
    
        Time.timeScale = 1f;
    
        if (gameManager != null)
        {
            gameManager.DeductHeart();
        }
    
        isPlayingAnimation = false;
    }
    
    public bool IsPlayingAnimation()
    {
        return isPlayingAnimation;
    }
}