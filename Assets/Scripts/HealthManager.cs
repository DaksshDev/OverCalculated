using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthManager : MonoBehaviour
{
    [CoolHeader("HEALTH MANAGER")]
    [Header("Heart Settings")]
    [SerializeField] private GameObject heartPrefab;
    [SerializeField] private Transform heartsContainer;
    [SerializeField] private Sprite heartEnabledSprite;
    [SerializeField] private Sprite heartDisabledSprite;
    [SerializeField] private float heartSpawnDelay = 0.2f;
    [SerializeField] private int maxHearts = 3;
    [SerializeField] private float disabledTransparancy = 0.2f;
    
    [Header("Heart Spawn Animation")]
    [SerializeField] private float spawnScaleFrom = 0f;
    [SerializeField] private float spawnScaleTo = 1f;
    [SerializeField] private float spawnAnimDuration = 0.3f;
    
    private List<GameObject> hearts = new List<GameObject>();
    private int currentHearts;
    private VFXManager vfxManager;

    public void Initialize(VFXManager vfx)
    {
        vfxManager = vfx;
        currentHearts = maxHearts;
        StartCoroutine(SpawnHeartsSequence());
    }

    IEnumerator SpawnHeartsSequence()
    {
        // Clear any existing hearts
        foreach (GameObject heart in hearts)
        {
            if (heart != null)
                Destroy(heart);
        }
        hearts.Clear();

        // Spawn hearts one by one with animation
        for (int i = 0; i < maxHearts; i++)
        {
            GameObject heart = Instantiate(heartPrefab, heartsContainer);
            Image heartImage = heart.GetComponent<Image>();
            CanvasGroup canvasGroup = heart.GetComponent<CanvasGroup>();
            
            if (heartImage != null)
            {
                heartImage.sprite = heartEnabledSprite;
            }

            // Reset canvas group alpha to 1
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            hearts.Add(heart);
            
            // Play spawn animation and wait for it to complete
            yield return StartCoroutine(AnimateHeartSpawn(heart));
            
            yield return new WaitForSeconds(heartSpawnDelay);
        }
    }

    IEnumerator AnimateHeartSpawn(GameObject heart)
    {
        if (heart == null) yield break;

        RectTransform rt = heart.GetComponent<RectTransform>();
        if (rt == null) yield break;

        rt.localScale = Vector3.one * spawnScaleFrom;
        
        float elapsed = 0f;
        
        while (elapsed < spawnAnimDuration)
        {
            // Check if the heart still exists
            if (heart == null || rt == null)
                yield break;
                
            elapsed += Time.deltaTime;
            float t = elapsed / spawnAnimDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            
            rt.localScale = Vector3.one * Mathf.Lerp(spawnScaleFrom, spawnScaleTo, smoothT);
            
            yield return null;
        }
        
        // Final check before setting final scale
        if (heart != null && rt != null)
        {
            rt.localScale = Vector3.one * spawnScaleTo;
        }
    }

    public void DeductHeart()
    {
        if (currentHearts <= 0) return;

        currentHearts--;
        
        // Get the heart that should be disabled (from right to left)
        int heartIndex = currentHearts;
        
        if (heartIndex >= 0 && heartIndex < hearts.Count)
        {
            GameObject heart = hearts[heartIndex];
            Image heartImage = heart.GetComponent<Image>();
            CanvasGroup canvasGroup = heart.GetComponent<CanvasGroup>();
            
            if (heartImage != null)
            {
                heartImage.sprite = heartDisabledSprite;
            }
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = disabledTransparancy;
            }
            
            // Trigger heart loss VFX
            if (vfxManager != null)
            {
                vfxManager.PlayHeartLossEffect(heart.transform);
            }
        }
    }

    public void ResetHearts()
    {
        currentHearts = maxHearts;
        
        // Stop any ongoing spawn coroutines
        StopAllCoroutines();
        
        // Properly clear existing hearts
        for (int i = hearts.Count - 1; i >= 0; i--)
        {
            if (hearts[i] != null)
            {
                Destroy(hearts[i]);
            }
        }
        hearts.Clear();
        
        // Start new spawn sequence
        StartCoroutine(SpawnHeartsSequence());
    }

    public int GetCurrentHearts()
    {
        return currentHearts;
    }

    public bool HasNoHearts()
    {
        return currentHearts <= 0;
    }
}