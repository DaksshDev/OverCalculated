using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardStackManager : MonoBehaviour
{
    [CoolHeader("Card Stack Manager")]
    [Header("Stack Settings")]
    [SerializeField] private Vector3 stackPosition = new Vector3(600f, -300f, 0f);
    [SerializeField] private Vector2 stackOffset = new Vector2(15f, -15f);
    [SerializeField] private float stackRotation = 3f;
    [SerializeField] private int maxVisibleCards = 10;
    [SerializeField] private float moveAnimDuration = 0.5f;
    
    private GameManager gameManager;
    private List<GameObject> visualStackCards = new List<GameObject>();
    
    public void Initialize(GameManager gm)
    {
        gameManager = gm;
    }
    
    public void CreateVisualStack(List<GameObject> cardPrefabs, Transform parent)
    {
        ClearStack();
        
        if (parent == null || cardPrefabs == null)
            return;
        
        int cardsToShow = Mathf.Min(cardPrefabs.Count, maxVisibleCards);
        
        for (int i = 0; i < cardsToShow; i++)
        {
            GameObject stackCard = Instantiate(cardPrefabs[i], parent);
            
            // Disable text on visual stack cards
            Card cardComponent = stackCard.GetComponent<Card>();
            if (cardComponent != null)
            {
                cardComponent.DisableAllText();
            }
            
            RectTransform rt = stackCard.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector3.zero;
                rt.localScale = Vector3.one;
                
                float rotation = GetStackRotation(i, cardsToShow);
                rt.localRotation = Quaternion.Euler(0, 0, rotation);
                
                // Keep stack cards behind cover card
                rt.SetAsFirstSibling();
            }
            
            visualStackCards.Add(stackCard);
        }
    }
    
    public IEnumerator MoveStackToCurrentDeck(Transform deckParent)
    {
        if (deckParent == null || visualStackCards.Count == 0)
            yield break;
        
        List<Coroutine> animCoroutines = new List<Coroutine>();
        
        for (int i = 0; i < visualStackCards.Count; i++)
        {
            GameObject card = visualStackCards[i];
            if (card != null)
            {
                yield return StartCoroutine(AnimateCardToDeck(card, i, deckParent));
            }
        }
        
        // Clear the visual stack after animation
        ClearStack();
    }
    
    IEnumerator AnimateCardToDeck(GameObject card, int index, Transform deckParent)
    {
        if (card == null) yield break;
        
        RectTransform rt = card.GetComponent<RectTransform>();
        if (rt == null) yield break;
        
        Vector3 startPos = rt.anchoredPosition;
        Quaternion startRot = rt.localRotation;
        
        // Move to deck parent
        card.transform.SetParent(deckParent, false);
        
        Vector3 targetPos = GetStackPosition(index);
        Quaternion targetRot = Quaternion.Euler(0, 0, GetStackRotation(index, visualStackCards.Count));
        
        float elapsed = 0f;
        
        while (elapsed < moveAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveAnimDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            
            rt.anchoredPosition = Vector3.Lerp(startPos, targetPos, smoothT);
            rt.localRotation = Quaternion.Lerp(startRot, targetRot, smoothT);
            
            yield return null;
        }
        
        rt.anchoredPosition = targetPos;
        rt.localRotation = targetRot;
    }
    
    public Vector3 GetStackPosition(int index)
    {
        return stackPosition + new Vector3(
            stackOffset.x * index,
            stackOffset.y * index,
            0
        );
    }
    
    public float GetStackRotation(int index, int totalCards)
    {
        if (totalCards <= 1) return 0f;
        return -stackRotation + (index * (stackRotation * 2 / (totalCards - 1)));
    }
    
    public void ClearStack()
    {
        foreach (GameObject card in visualStackCards)
        {
            if (card != null)
                Destroy(card);
        }
        visualStackCards.Clear();
    }
}