using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    [CoolHeader("G A M E   M A N A G E R")]
    [Header("Card Prefabs")]
    [SerializeField] private GameObject animCardPrefab;
    [SerializeField] private GameObject coverCardPrefab;
    [SerializeField] private GameObject[] cardPrefabs;
    
    [Header("Transform Parents")]
    [SerializeField] public Transform currentCardParent;
    [SerializeField] public Transform doneCardParent;
    [SerializeField] public Transform currentDeckParent;
    
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI roundText;
    [SerializeField] private Slider timerSlider;
    [SerializeField] private Image timerSliderFill;
    [SerializeField] private GameObject roundCompletePanel;
    
    [Header("Managers")]
    [SerializeField] private HealthManager healthManager;
    [SerializeField] private VFXManager vfxManager;
    [SerializeField] private ProgressionTracker progressionTracker; // NEW!
    
    [Header("Game Settings")]
    [SerializeField] private float baseCardSolveTime = 30f;
    [SerializeField] private float roundStartDelay = 2f;
    [SerializeField] private float deckChooseAnimDuration = 1f;
    [SerializeField] private float baseCardMoveAnimDuration = 0.4f;
    [SerializeField] private float speedMultiplierPerRound = 0.05f;
    
    [Header("Timer Colors")]
    [SerializeField] private Color fullTimeColor = Color.green;
    [SerializeField] private Color halfTimeColor = Color.yellow;
    [SerializeField] private Color lowTimeColor = Color.red;
    [SerializeField] private float timerLerpSpeed = 5f;
    
    [Header("Events")]
    [SerializeField] private UnityEvent OnLevel5Reached;
    
    // Components
    private CardStackManager stackManager;
    private AnswerChecker answerChecker;
    private Wrong wrongSystem;
    
    // Game State
    private int currentRound = 1;
    private float currentTime;
    private bool isRoundActive = false;
    private bool isCoverCardActive = false;
    private bool isAnimatingCard = false;
    private bool hasReachedLevel5 = false;
    
    // Card Management
    private Card currentCard;
    private GameObject coverCardObject;
    private List<GameObject> currentDeckCards = new List<GameObject>();
    private List<GameObject> currentDeckCardPrefabs = new List<GameObject>();
    private string currentDifficulty = "Easy Deck";
    
    // Dynamic difficulty
    private float cardSolveTime;
    private float cardMoveAnimDuration;
    
    // SOLVE TIME TRACKING (NEW!)
    private float cardStartTime;
    private bool isTimingCard = false;

    void Start()
    {
        stackManager = GetComponent<CardStackManager>();
        answerChecker = GetComponent<AnswerChecker>();
        wrongSystem = GetComponent<Wrong>();

        if (stackManager != null)
            stackManager.Initialize(this);
    
        if (healthManager != null)
            healthManager.Initialize(vfxManager);

        SetupParentAlignments();
    
        if (roundCompletePanel != null)
            roundCompletePanel.SetActive(false);

        // LOAD SAVED ROUND
        currentRound = PlayerPrefs.GetInt("CurrentRound", 1);

        StartRound();
    }

    void Update()
    {
        if (isRoundActive && currentCard != null && !isCoverCardActive && !isAnimatingCard)
        {
            if (wrongSystem != null && wrongSystem.IsPlayingAnimation())
                return;
            
            UpdateTimer();
        }
    }
    
    void SetupParentAlignments()
    {
        SetRectTransformAlignment(currentCardParent);
        SetRectTransformAlignment(doneCardParent);
        SetRectTransformAlignment(currentDeckParent);
    }
    
    void SetRectTransformAlignment(Transform parent)
    {
        if (parent == null) return;
        
        RectTransform rt = parent.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
        }
    }
    
    void UpdateTimer()
    {
        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            timerSlider.value = Mathf.Lerp(timerSlider.value, currentTime / cardSolveTime, Time.deltaTime * timerLerpSpeed);
            
            float timePercent = currentTime / cardSolveTime;
            Color targetColor = GetTimerColor(timePercent);
            
            if (timerSliderFill != null)
            {
                timerSliderFill.color = targetColor;
            }
        }
        else
        {
            OnWrongAnswer();
        }
    }
    
    Color GetTimerColor(float timePercent)
    {
        if (timePercent > 0.5f)
        {
            return Color.Lerp(halfTimeColor, fullTimeColor, (timePercent - 0.5f) * 2f);
        }
        else
        {
            return Color.Lerp(lowTimeColor, halfTimeColor, timePercent * 2f);
        }
    }

    public void StartRound()
    {
        if (currentRound == 5 && !hasReachedLevel5)
        {
            hasReachedLevel5 = true;
            OnLevel5Reached?.Invoke();
            Debug.Log("Level 5 reached! Multiply and Division unlocked!");
        }

        // SAVE CURRENT ROUND
        PlayerPrefs.SetInt("CurrentRound", currentRound);
        PlayerPrefs.Save();

        if (healthManager != null)
            healthManager.ResetHearts();
    
        if (vfxManager != null)
            vfxManager.ResetCombo();

        // NOTIFY PROGRESSION TRACKER — round is starting
        if (progressionTracker != null)
            progressionTracker.OnRoundStarted();

        cardMoveAnimDuration = CalculateAnimationSpeed();
        StartCoroutine(RoundStartSequence());
    }
    
    float CalculateTimeLimit()
    {
        // SMART TIMER: Calculate based on actual question difficulty
        if (currentCard == null) return 20f;
        
        float difficulty = currentCard.GetQuestionDifficulty();
        
        // Map difficulty (0-1) to time range
        // Super easy (0.0-0.2): 4-6 seconds
        // Easy (0.2-0.4): 6-10 seconds
        // Medium (0.4-0.6): 10-15 seconds
        // Hard (0.6-0.8): 15-20 seconds
        // Very hard (0.8-1.0): 20-25 seconds
        
        float baseTime;
        if (difficulty < 0.2f)
            baseTime = Mathf.Lerp(4f, 6f, difficulty / 0.2f); // Super easy: 4-6s
        else if (difficulty < 0.4f)
            baseTime = Mathf.Lerp(6f, 10f, (difficulty - 0.2f) / 0.2f); // Easy: 6-10s
        else if (difficulty < 0.6f)
            baseTime = Mathf.Lerp(10f, 15f, (difficulty - 0.4f) / 0.2f); // Medium: 10-15s
        else if (difficulty < 0.8f)
            baseTime = Mathf.Lerp(15f, 20f, (difficulty - 0.6f) / 0.2f); // Hard: 15-20s
        else
            baseTime = Mathf.Lerp(20f, 25f, (difficulty - 0.8f) / 0.2f); // Very hard: 20-25s
        
        // Apply level scaling (gets slightly faster as levels increase)
        float levelMultiplier = Mathf.Max(0.7f, 1f - (currentRound * 0.03f));
        
        return Mathf.Max(3f, baseTime * levelMultiplier);
    }
    
    float CalculateAnimationSpeed()
    {
        float speedIncrease = (currentRound - 1) * speedMultiplierPerRound;
        return Mathf.Max(0.1f, baseCardMoveAnimDuration - speedIncrease);
    }

    IEnumerator RoundStartSequence()
    {
        isRoundActive = false;
        isCoverCardActive = false;
        
        ClearAllCards();
        
        // Reset timer UI
        currentTime = 0;
        if (timerSlider != null)
            timerSlider.value = 0;
        
        // Hide solve time text immediately
        if (answerChecker != null)
            answerChecker.HideSolveTimeText();
        
        if (roundText != null)
        {
            roundText.text = $"Round {currentRound}";
            roundText.gameObject.SetActive(true);
        }
        
        yield return new WaitForSeconds(roundStartDelay);
        
        if (roundText != null)
        {
            roundText.gameObject.SetActive(false);
        }
        
        SetupDeck();
        yield return StartCoroutine(DeckChooseAnimation());
        
        SpawnCoverCard();
    }

    void SetupDeck()
    {
        currentDeckCards.Clear();
        currentDeckCardPrefabs.Clear();
        if (stackManager != null)
            stackManager.ClearStack();
    
        // Determine difficulty name
        if (currentRound <= 4)
            currentDifficulty = "Easy Deck";
        else if (currentRound <= 7)
            currentDifficulty = "Mid Deck";
        else if (currentRound <= 10)
            currentDifficulty = "Hard Deck";
        else
            currentDifficulty = "??? Deck";
    
        int cardCount = 5 + (currentRound * 2);
    
        for (int i = 0; i < cardCount; i++)
        {
            GameObject cardPrefab = GetAppropriateCardForRound();
            currentDeckCardPrefabs.Add(cardPrefab);
        }
    }
    
    GameObject GetAppropriateCardForRound()
    {
        List<CardType> allowedTypes = new List<CardType>();
        
        // Levels 1-4: ONLY Add and Subtract (VERY EASY)
        if (currentRound <= 4)
        {
            allowedTypes.Add(CardType.Add);
            allowedTypes.Add(CardType.Subtract);
        }
        // Levels 5-7: Add, Subtract, Multiply, Division (easy versions)
        else if (currentRound <= 7)
        {
            // 40% Add/Subtract, 60% Multiply/Division
            int rand = Random.Range(0, 100);
            if (rand < 40)
            {
                allowedTypes.Add(CardType.Add);
                allowedTypes.Add(CardType.Subtract);
            }
            else
            {
                allowedTypes.Add(CardType.Multiply);
                allowedTypes.Add(CardType.Division);
            }
        }
        // Level 8+: All operations
        else
        {
            allowedTypes.Add(CardType.Add);
            allowedTypes.Add(CardType.Subtract);
            allowedTypes.Add(CardType.Multiply);
            allowedTypes.Add(CardType.Division);
            allowedTypes.Add(CardType.Square);
            allowedTypes.Add(CardType.Cube);
            allowedTypes.Add(CardType.SquareRoot);
            allowedTypes.Add(CardType.CubeRoot);
            allowedTypes.Add(CardType.Absolute);
            allowedTypes.Add(CardType.Double);
            allowedTypes.Add(CardType.Half);
        }
        
        List<GameObject> validPrefabs = new List<GameObject>();
        foreach (GameObject prefab in cardPrefabs)
        {
            Card card = prefab.GetComponent<Card>();
            if (card != null && allowedTypes.Contains(card.GetCardType()))
            {
                validPrefabs.Add(prefab);
            }
        }
        
        if (validPrefabs.Count > 0)
            return validPrefabs[Random.Range(0, validPrefabs.Count)];
        else
            return cardPrefabs[Random.Range(0, cardPrefabs.Length)];
    }

    IEnumerator DeckChooseAnimation()
    {
        if (animCardPrefab == null || currentCardParent == null)
        {
            yield return new WaitForSeconds(deckChooseAnimDuration);
            yield break;
        }
        
        GameObject animCard = Instantiate(animCardPrefab, currentCardParent);
        ResetCardTransform(animCard);
        
        DeckChooseAnim deckAnim = animCard.GetComponent<DeckChooseAnim>();
        
        if (deckAnim != null)
        {
            bool animComplete = false;
            deckAnim.PlayAnimation(currentDifficulty, () => { animComplete = true; });
            yield return new WaitUntil(() => animComplete);
        }
        else
        {
            yield return new WaitForSeconds(deckChooseAnimDuration);
        }
        
        Destroy(animCard);
    }
    
    void SpawnCoverCard()
    {
        if (coverCardPrefab == null || currentCardParent == null)
        {
            CreateInitialDeckStack();
            DrawNextCardFromDeck();
            isRoundActive = true;
            return;
        }
    
        if (stackManager != null)
        {
            stackManager.CreateVisualStack(currentDeckCardPrefabs, currentCardParent);
        
            int childIndex = 0;
            foreach (Transform child in currentCardParent)
            {
                Card cardComponent = child.GetComponent<Card>();
                if (cardComponent != null)
                {
                    cardComponent.SetLevel(currentRound);
                    cardComponent.SetHiddenSprite(childIndex % 2 == 0);
                    childIndex++;
                }
            }
        }
    
        coverCardObject = Instantiate(coverCardPrefab, currentCardParent);
        ResetCardTransform(coverCardObject);
    
        CoverCardController coverController = coverCardObject.GetComponent<CoverCardController>();
        if (coverController != null)
        {
            coverController.Initialize(this);
        }
    
        coverCardObject.transform.SetAsLastSibling();
    
        currentTime = 0;
        timerSlider.value = 0;
        isCoverCardActive = true;
    }

    public void OnCoverCardSwiped(GameObject coverCard)
    {
        isCoverCardActive = false;
        StartCoroutine(HandleCoverCardSwipe(coverCard));
    }
    
    IEnumerator HandleCoverCardSwipe(GameObject coverCard)
    {
        isAnimatingCard = true;

        if (coverCard != null && currentDeckParent != null)
        {
            coverCard.transform.SetParent(currentDeckParent, false);
        }

        if (stackManager != null && currentCardParent != null && currentDeckParent != null)
        {
            List<Transform> cardsToMove = new List<Transform>();
            foreach (Transform child in currentCardParent)
            {
                Card cardComponent = child.GetComponent<Card>();
                if (cardComponent != null)
                {
                    cardsToMove.Add(child);
                }
            }
    
            for (int i = 0; i < cardsToMove.Count; i++)
            {
                Transform cardTransform = cardsToMove[i];
                if (cardTransform != null)
                {
                    CreateDeckCard(i);
                    float animSpeed = Mathf.Max(0.1f, cardMoveAnimDuration - (i * 0.02f));
                    yield return StartCoroutine(AnimateCardToDeck(cardTransform.gameObject, animSpeed, i));
                    yield return new WaitForSeconds(0.03f);
                }
            }
        }

        yield return new WaitForSeconds(0.15f);
        DrawNextCardFromDeck();
        isAnimatingCard = false;
        isRoundActive = true;
    }
    
    void CreateDeckCard(int index)
    {
        if (currentDeckParent == null || index >= currentDeckCardPrefabs.Count) return;

        GameObject cardPrefab = currentDeckCardPrefabs[index];
        GameObject card = Instantiate(cardPrefab, currentDeckParent);

        RectTransform rt = card.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = stackManager.GetStackPosition(index);
            rt.localRotation = Quaternion.Euler(0, 0, stackManager.GetStackRotation(index, currentDeckCardPrefabs.Count));
            rt.localScale = Vector3.one;
        }

        Card cardComponent = card.GetComponent<Card>();
        if (cardComponent != null)
        {
            cardComponent.SetLevel(currentRound);
            cardComponent.DisableAllText();
            cardComponent.SetHiddenSprite(index % 2 == 0);
        }

        currentDeckCards.Add(card);
    }
    
    void CreateInitialDeckStack()
    {
        if (currentDeckParent == null) return;

        for (int i = 0; i < currentDeckCardPrefabs.Count; i++)
        {
            GameObject card = Instantiate(currentDeckCardPrefabs[i], currentDeckParent);
    
            RectTransform rt = card.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = stackManager.GetStackPosition(i);
                rt.localRotation = Quaternion.Euler(0, 0, stackManager.GetStackRotation(i, currentDeckCardPrefabs.Count));
                rt.localScale = Vector3.one;
            }
    
            Card cardComponent = card.GetComponent<Card>();
            if (cardComponent != null)
            {
                cardComponent.SetLevel(currentRound);
                cardComponent.DisableAllText();
                cardComponent.SetHiddenSprite(i % 2 == 0);
            }
    
            currentDeckCards.Add(card);
        }
    }
    
    IEnumerator AnimateCardToDeck(GameObject card, float duration, int cardIndex)
    {
        if (card == null || currentDeckParent == null) yield break;

        RectTransform rt = card.GetComponent<RectTransform>();
        if (rt == null) yield break;

        Vector3 worldPosStart = rt.position;
        Quaternion worldRotStart = rt.rotation;
    
        Vector3 targetLocalPos = stackManager.GetStackPosition(cardIndex);
        float targetRotZ = stackManager.GetStackRotation(cardIndex, currentDeckCardPrefabs.Count);
        Quaternion targetLocalRot = Quaternion.Euler(0, 0, targetRotZ);
    
        card.transform.SetParent(currentDeckParent, false);
        rt.anchoredPosition = targetLocalPos;
        rt.localRotation = targetLocalRot;
        Vector3 worldPosTarget = rt.position;
        Quaternion worldRotTarget = rt.rotation;
    
        card.transform.SetParent(currentCardParent, true);
        rt.position = worldPosStart;
        rt.rotation = worldRotStart;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smoothT = 1f - Mathf.Pow(1f - t, 3f);
        
            rt.position = Vector3.Lerp(worldPosStart, worldPosTarget, smoothT);
            rt.rotation = Quaternion.Lerp(worldRotStart, worldRotTarget, smoothT);
        
            yield return null;
        }

        card.transform.SetParent(currentDeckParent, false);
        rt.anchoredPosition = targetLocalPos;
        rt.localRotation = targetLocalRot;

        Destroy(card);
    }
    
    void DrawNextCardFromDeck()
    {
        if (currentDeckCards.Count == 0)
        {
            WinRound();
            return;
        }
        
        GameObject cardToDraw = currentDeckCards[currentDeckCards.Count - 1];
        currentDeckCards.RemoveAt(currentDeckCards.Count - 1);
        
        StartCoroutine(AnimateCardToCurrentPosition(cardToDraw));
    }
    
    IEnumerator AnimateCardToCurrentPosition(GameObject card)
    {
        isAnimatingCard = true;
        
        if (card == null || currentCardParent == null)
        {
            isAnimatingCard = false;
            yield break;
        }
        
        RectTransform rt = card.GetComponent<RectTransform>();
        if (rt == null)
        {
            isAnimatingCard = false;
            yield break;
        }
        
        Transform originalParent = card.transform.parent;
        Vector3 worldPosStart = rt.position;
        Quaternion worldRotStart = rt.rotation;
        
        Vector3 targetLocalPos = Vector3.zero;
        Quaternion targetLocalRot = Quaternion.identity;
        
        Transform tempParent = card.transform.parent;
        card.transform.SetParent(currentCardParent, false);
        rt.anchoredPosition = targetLocalPos;
        rt.localRotation = targetLocalRot;
        rt.localScale = Vector3.one;
    
        currentCard = card.GetComponent<Card>();
        if (currentCard != null)
        {
            currentCard.SetLevel(currentRound);
            currentCard.RestoreOriginalSprite();
            currentCard.EnableAllText();
            currentCard.GenerateQuestion();
        }
    
        // Calculate time AFTER question is generated
        cardSolveTime = CalculateTimeLimit();
        currentTime = cardSolveTime;
        timerSlider.value = 1f;
        
        // START TIMING THE CARD (NEW!)
        StartCardTimer();
    
        isAnimatingCard = false; // Animation done!
    
        // NOW start the timer - card is fully visible
        if (vfxManager != null)
            vfxManager.StartCardTimer();
    
        if (answerChecker != null)
        {
            answerChecker.StartCardTimer();
            answerChecker.ClearInput();
        }
    }
    
    // NEW METHODS FOR TIMING
    void StartCardTimer()
    {
        cardStartTime = Time.time;
        isTimingCard = true;
    }
    
    void StopCardTimer()
    {
        isTimingCard = false;
    }
    
    public float GetCurrentSolveTime()
    {
        if (!isTimingCard) return 0f;
        return Time.time - cardStartTime;
    }
    
    public void OnCorrectAnswer()
    {
        if (currentCard == null) return;
        
        // GET THE SOLVE TIME (NEW!)
        float solveTime = GetCurrentSolveTime();
        StopCardTimer();
        
        // Track XP with ProgressionTracker (NEW!)
        if (progressionTracker != null)
        {
            progressionTracker.OnCardSolved(solveTime);
        }
        
        if (vfxManager != null)
            vfxManager.PlayCorrectAnswerEffects(currentCard.transform);
        
        StartCoroutine(MoveCardToDone());
    }
    
    public void OnWrongAnswer()
    {
        // Stop timing the card (NEW!)
        StopCardTimer();
        
        if (vfxManager != null)
            vfxManager.ResetCombo();
        
        if (wrongSystem != null)
        {
            wrongSystem.PlayWrongAnswerSequence(healthManager != null ? healthManager.GetCurrentHearts() : 0);
        }
        else
        {
            DeductHeart();
        }
    }
    
    public void DeductHeart()
    {
        // Track failed card (NEW!)
        if (progressionTracker != null)
            progressionTracker.OnCardFailed();
        
        if (healthManager != null)
            healthManager.DeductHeart();
        
        if (healthManager != null && healthManager.HasNoHearts())
        {
            FailRound();
        }
        else
        {
            if (currentCard != null)
                Destroy(currentCard.gameObject);
            currentCard = null;
            DrawNextCardFromDeck();
        }
    }
    
    IEnumerator MoveCardToDone()
    {
        isAnimatingCard = true;
    
        GameObject cardObject = currentCard.gameObject;
        RectTransform rt = cardObject.GetComponent<RectTransform>();
    
        if (rt == null || doneCardParent == null)
        {
            currentCard = null;
            isAnimatingCard = false;
            yield break;
        }
    
        Vector3 worldPosStart = rt.position;
        Quaternion worldRotStart = rt.rotation;
    
        int doneCardCount = doneCardParent.childCount;
        float offset = doneCardCount * 5f;
        Vector3 targetLocalPos = new Vector3(-offset, -offset, 0);
        Quaternion targetLocalRot = Quaternion.identity;
    
        cardObject.transform.SetParent(doneCardParent, false);
        rt.anchoredPosition = targetLocalPos;
        rt.localRotation = targetLocalRot;
        Vector3 worldPosTarget = rt.position;
        Quaternion worldRotTarget = rt.rotation;
    
        cardObject.transform.SetParent(currentCardParent, true);
        rt.position = worldPosStart;
        rt.rotation = worldRotStart;
    
        float elapsed = 0f;
    
        while (elapsed < cardMoveAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / cardMoveAnimDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
        
            rt.position = Vector3.Lerp(worldPosStart, worldPosTarget, smoothT);
            rt.rotation = Quaternion.Lerp(worldRotStart, worldRotTarget, smoothT);
        
            yield return null;
        }
    
        cardObject.transform.SetParent(doneCardParent, false);
        rt.anchoredPosition = targetLocalPos;
        rt.localRotation = targetLocalRot;
        rt.localScale = Vector3.one;
    
        currentCard = null;
        isAnimatingCard = false;
        DrawNextCardFromDeck();
    }

    void WinRound()
    {
        isRoundActive = false;
    
        if (roundCompletePanel != null)
        {
            roundCompletePanel.SetActive(true);
            if (progressionTracker != null)
                progressionTracker.OnRoundWon();
        }
        else
        {
            NextRoundOk();
        }
    }
    

    public void NextRoundOk()
    {
        if (roundCompletePanel != null)
            roundCompletePanel.SetActive(false);
        
        currentRound++;

        // SAVE ADVANCED ROUND
        PlayerPrefs.SetInt("CurrentRound", currentRound);
        PlayerPrefs.Save();

        ClearAllCards();
        StartRound();
    }

    void FailRound()
    {
        isRoundActive = false;
    
        if (progressionTracker != null)
            progressionTracker.OnRoundLost();
    
        if (currentRound == 1)
        {
            Invoke(nameof(RestartRound), 2f);
        }
        else
        {
            Invoke(nameof(ReturnToMenu), 2f);
        }
    }

    void RestartRound()
    {
        currentRound = 1;
        hasReachedLevel5 = false;

        // RESET SAVED ROUND
        PlayerPrefs.SetInt("CurrentRound", 1);
        PlayerPrefs.Save();

        ClearAllCards();
        StartRound();
    }

    void ReturnToMenu()
    {
        // CLEAR ROUND ON DEATH-RETURN (optional: keep round or reset depending on your design)
        PlayerPrefs.SetInt("CurrentRound", 1);
        PlayerPrefs.Save();

        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
    
    void ClearAllCards()
    {
        if (currentCard != null)
        {
            Destroy(currentCard.gameObject);
            currentCard = null;
        }
        
        if (coverCardObject != null)
        {
            Destroy(coverCardObject);
            coverCardObject = null;
        }
        
        foreach (GameObject card in currentDeckCards)
        {
            if (card != null)
                Destroy(card);
        }
        currentDeckCards.Clear();
        currentDeckCardPrefabs.Clear();
        
        if (doneCardParent != null)
        {
            foreach (Transform child in doneCardParent)
            {
                Destroy(child.gameObject);
            }
        }
        
        if (currentDeckParent != null)
        {
            foreach (Transform child in currentDeckParent)
            {
                Destroy(child.gameObject);
            }
        }
        
        if (stackManager != null)
            stackManager.ClearStack();
    }
    
    void ResetCardTransform(GameObject card)
    {
        if (card == null) return;
        
        RectTransform rt = card.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector3.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }
    }
    
    public bool IsRoundActive() => isRoundActive;
    public Card GetCurrentCard() => currentCard;
    public int GetCurrentRound() => currentRound;
}