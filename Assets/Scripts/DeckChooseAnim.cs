using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeckChooseAnim : MonoBehaviour
{
    [CoolHeader("Choose Animation")]
    [Header("Animation Settings")]
    [SerializeField] private Sprite[] deckSprites;
    [SerializeField] private Image targetImage;
    [SerializeField] private float frameRate = 0.1f;
    [SerializeField] private int cycleCount = 10;
    [SerializeField] private bool slowDownAtEnd = true;
    [SerializeField] private float finalPauseDuration = 0.5f;
    
    private bool isAnimating = false;
    private TextMeshProUGUI difficultyText;
    
    void Start()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();
        
        // Find the "t" text child
        Transform textChild = transform.Find("t");
        if (textChild != null)
        {
            difficultyText = textChild.GetComponent<TextMeshProUGUI>();
        }
    }
    
    public void PlayAnimation(string difficulty, System.Action onComplete = null)
    {
        if (!isAnimating)
        {
            StartCoroutine(AnimateSelection(difficulty, onComplete));
        }
    }
    
    IEnumerator AnimateSelection(string difficulty, System.Action onComplete)
    {
        isAnimating = true;
        
        // Build sprite array for animation
        Sprite[] animSprites = new Sprite[deckSprites.Length];
        for (int i = 0; i < deckSprites.Length; i++)
        {
            animSprites[i] = deckSprites[i];
        }
        
        int totalFrames = cycleCount;
        float currentFrameRate = frameRate;
        
        for (int i = 0; i < totalFrames; i++)
        {
            // Cycle through sprites
            targetImage.sprite = animSprites[i % animSprites.Length];
            
            // Slow down at the end for dramatic effect
            if (slowDownAtEnd && i > totalFrames - 5)
            {
                currentFrameRate = Mathf.Lerp(frameRate, frameRate * 3f, (i - (totalFrames - 5)) / 5f);
            }
            
            yield return new WaitForSeconds(currentFrameRate);
        }
        
        // Set final difficulty text
        if (difficultyText != null)
        {
            difficultyText.text = difficulty;
        }
        
        // Pause on final selection
        yield return new WaitForSeconds(finalPauseDuration);
        
        isAnimating = false;
        
        // Callback when animation completes
        onComplete?.Invoke();
    }
    
    public void StopAnimation()
    {
        StopAllCoroutines();
        isAnimating = false;
    }
}