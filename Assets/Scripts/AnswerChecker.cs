using UnityEngine;
using TMPro;
using System.Collections;

public class AnswerChecker : MonoBehaviour
{
    [CoolHeader("Answer Checking")]
    [Header("UI References")]
    [SerializeField] private TMP_InputField answerInputField;
    [SerializeField] private TextMeshProUGUI solveTimeText;
    
    [Header("Solve Time Colors")]
    [SerializeField] private Color fastColor = new Color(0.2f, 1f, 0.2f); // Green for < 3s
    [SerializeField] private Color quickColor = new Color(0.3f, 0.6f, 1f); // Blue for < 5s
    [SerializeField] private Color normalColor = new Color(0.4f, 0.2f, 0.6f); // Dark purple for > 5s
    
    [Header("Display Settings")]
    [SerializeField] private float displayDuration = 2.5f;
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    
    private GameManager gameManager;
    private float cardStartTime;
    private bool isTimingCard = false;
    
    void Start()
    {
        gameManager = GetComponent<GameManager>();
        
        if (answerInputField != null)
        {
            answerInputField.onSubmit.AddListener((string value) => CheckAnswer());
        }
        
        if (solveTimeText != null)
        {
            solveTimeText.gameObject.SetActive(false);
        }
    }
    
    public void StartCardTimer()
    {
        cardStartTime = Time.time;
        isTimingCard = true;
    }
    
    public void StopCardTimer()
    {
        isTimingCard = false;
    }
    
    float GetSolveTime()
    {
        if (!isTimingCard) return 0f;
        return Time.time - cardStartTime;
    }
    
    public void CheckAnswer()
    {
        if (gameManager == null || !gameManager.IsRoundActive())
            return;
        
        Card currentCard = gameManager.GetCurrentCard();
        if (currentCard == null || answerInputField == null)
            return;
        
        string userInput = answerInputField.text.Trim();
        
        if (string.IsNullOrEmpty(userInput))
            return;
        
        if (float.TryParse(userInput, out float userAnswer))
        {
            float correctAnswer = currentCard.GetCorrectAnswer();
            
            if (Mathf.Approximately(userAnswer, correctAnswer))
            {
                // Calculate solve time
                float solveTime = GetSolveTime();
                StopCardTimer();
                
                // Show solve time
                StartCoroutine(ShowSolveTime(solveTime));
                
                gameManager.OnCorrectAnswer();
            }
            else
            {
                StopCardTimer();
                gameManager.OnWrongAnswer();
            }
        }
        else
        {
            StopCardTimer();
            gameManager.OnWrongAnswer();
        }
    }
    
    IEnumerator ShowSolveTime(float time)
    {
        if (solveTimeText == null) yield break;
        
        // Determine color based on solve time
        Color targetColor;
        string speedText;
        
        if (time <= 3f)
        {
            targetColor = fastColor;
            speedText = "EXCELLENT!";
        }
        else if (time <= 5f)
        {
            targetColor = quickColor;
            speedText = "GOOD!";
        }
        else
        {
            targetColor = normalColor;
            speedText = "";
        }
        
        // Set text
        solveTimeText.text = $"{speedText}\n{time:F2}s";
        solveTimeText.color = new Color(targetColor.r, targetColor.g, targetColor.b, 0f);
        solveTimeText.gameObject.SetActive(true);
        
        // Fade in
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            solveTimeText.color = new Color(targetColor.r, targetColor.g, targetColor.b, alpha);
            yield return null;
        }
        
        solveTimeText.color = targetColor;
        
        // Hold
        yield return new WaitForSeconds(displayDuration);
        
        // Fade out
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            solveTimeText.color = new Color(targetColor.r, targetColor.g, targetColor.b, alpha);
            yield return null;
        }
        
        solveTimeText.gameObject.SetActive(false);
    }
    
    public void ClearInput()
    {
        if (answerInputField != null)
        {
            answerInputField.text = "";
            answerInputField.ActivateInputField();
        }
    }
    
    public void HideSolveTimeText()
    {
        // Stop any running coroutine and immediately hide the text
        StopAllCoroutines();
        if (solveTimeText != null)
        {
            solveTimeText.gameObject.SetActive(false);
        }
    }
}