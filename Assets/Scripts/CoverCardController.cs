using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class CoverCardController : MonoBehaviour
{
    [CoolHeader("cover card")]
    [Header("Swipe Settings")]
    [SerializeField] private float swipeThreshold = 100f;
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private float dragMultiplier = 0.3f;
    
    private Vector2 swipeStartPos;
    private bool isDragging = false;
    private RectTransform rectTransform;
    private GameManager gameManager;
    private CanvasGroup canvasGroup;
    
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // Add canvas group for fade effect
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        AddSwipeDetection();
    }
    
    public void Initialize(GameManager gm)
    {
        gameManager = gm;
    }
    
    void Update()
    {
        if (isDragging && Input.GetMouseButton(0))
        {
            Vector2 currentPos = Input.mousePosition;
            Vector2 delta = currentPos - swipeStartPos;
            
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(delta.x * dragMultiplier, delta.y * dragMultiplier);
                
                // Slight rotation based on drag
                float rotation = (delta.x * dragMultiplier) * 0.05f;
                rectTransform.localRotation = Quaternion.Euler(0, 0, -rotation);
            }
        }
    }
    
    void AddSwipeDetection()
    {
        EventTrigger trigger = GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = gameObject.AddComponent<EventTrigger>();
        }
        
        EventTrigger.Entry pointerDown = new EventTrigger.Entry();
        pointerDown.eventID = EventTriggerType.PointerDown;
        pointerDown.callback.AddListener((data) => { OnPointerDown((PointerEventData)data); });
        trigger.triggers.Add(pointerDown);
        
        EventTrigger.Entry pointerUp = new EventTrigger.Entry();
        pointerUp.eventID = EventTriggerType.PointerUp;
        pointerUp.callback.AddListener((data) => { OnPointerUp((PointerEventData)data); });
        trigger.triggers.Add(pointerUp);
    }
    
    void OnPointerDown(PointerEventData eventData)
    {
        swipeStartPos = eventData.position;
        isDragging = true;
    }
    
    void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging) return;
        
        Vector2 swipeEndPos = eventData.position;
        Vector2 swipeDelta = swipeEndPos - swipeStartPos;
        
        if (swipeDelta.magnitude >= swipeThreshold)
        {
            // Determine swipe direction and animate accordingly
            StartCoroutine(AnimateSwipeAway(swipeDelta));
        }
        else
        {
            // Reset position if swipe wasn't big enough
            StartCoroutine(ResetPosition());
        }
        
        isDragging = false;
    }
    
    IEnumerator AnimateSwipeAway(Vector2 swipeDirection)
    {
        isDragging = false;
        
        if (rectTransform == null) yield break;
        
        Vector3 startPos = rectTransform.anchoredPosition;
        Quaternion startRot = rectTransform.localRotation;
        
        // Calculate target based on swipe direction
        Vector3 targetPos = startPos + (Vector3)swipeDirection.normalized * 1500f;
        
        // Rotate based on direction
        float targetRotation = swipeDirection.x < 0 ? -30f : 30f;
        Quaternion targetRot = Quaternion.Euler(0, 0, targetRotation);
        
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            
            rectTransform.anchoredPosition = Vector3.Lerp(startPos, targetPos, smoothT);
            rectTransform.localRotation = Quaternion.Lerp(startRot, targetRot, smoothT);
            rectTransform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.8f, smoothT);
            
            // Fade out
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, smoothT);
            }
            
            yield return null;
        }
        
        // Notify game manager that swipe is complete
        if (gameManager != null)
        {
            gameManager.OnCoverCardSwiped(gameObject);
        }
    }
    
    IEnumerator ResetPosition()
    {
        if (rectTransform == null) yield break;
        
        Vector3 startPos = rectTransform.anchoredPosition;
        Quaternion startRot = rectTransform.localRotation;
        
        float duration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            
            rectTransform.anchoredPosition = Vector3.Lerp(startPos, Vector3.zero, smoothT);
            rectTransform.localRotation = Quaternion.Lerp(startRot, Quaternion.identity, smoothT);
            
            yield return null;
        }
        
        rectTransform.anchoredPosition = Vector3.zero;
        rectTransform.localRotation = Quaternion.identity;
    }
}