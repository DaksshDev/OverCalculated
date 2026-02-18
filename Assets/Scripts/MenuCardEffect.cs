using UnityEngine;
using System.Collections;

public class MenuCardEffect : MonoBehaviour
{
    [CoolHeader("MENU CARD EFFECT")]
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private float delayBetweenCards = 0.1f;
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("References")]
    [SerializeField] private Transform origin;
    
    private Transform[] cardPanels;
    private Vector3[] originalPositions;

    void Start()
    {
        // Get all child transforms (the card panels)
        int childCount = transform.childCount;
        cardPanels = new Transform[childCount];
        originalPositions = new Vector3[childCount];
        
        // Store original positions and move cards to origin
        for (int i = 0; i < childCount; i++)
        {
            cardPanels[i] = transform.GetChild(i);
            originalPositions[i] = cardPanels[i].localPosition;
            
            // Move card to origin position
            if (origin != null)
            {
                cardPanels[i].position = origin.position;
            }
        }
        
        // Start the animation
        StartCoroutine(AnimateCards());
    }

    IEnumerator AnimateCards()
    {
        // Animate each card one by one
        for (int i = 0; i < cardPanels.Length; i++)
        {
            StartCoroutine(AnimateCard(cardPanels[i], originalPositions[i]));
            yield return new WaitForSeconds(delayBetweenCards);
        }
    }

    IEnumerator AnimateCard(Transform card, Vector3 targetLocalPos)
    {
        Vector3 startPos = card.position;
        Vector3 targetWorldPos = transform.TransformPoint(targetLocalPos);
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            float curveValue = movementCurve.Evaluate(t);
            
            card.position = Vector3.Lerp(startPos, targetWorldPos, curveValue);
            yield return null;
        }

        // Ensure final position is exact
        card.localPosition = targetLocalPos;
    }

    // Optional: Call this to replay the animation
    public void ReplayAnimation()
    {
        StopAllCoroutines();
        
        // Reset all cards to origin
        for (int i = 0; i < cardPanels.Length; i++)
        {
            if (origin != null)
            {
                cardPanels[i].position = origin.position;
            }
        }
        
        StartCoroutine(AnimateCards());
    }
}