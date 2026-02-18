using UnityEngine;
using System.Collections;

public class PixelCards : MonoBehaviour
{
    [SettingsHeader("makes static cards come to life")]
    [Header("Rotation Settings")]
    [Tooltip("Enable X axis rotation")]
    public bool rotateX = false;
    public float minX = -1f;
    public float maxX = 1f;
    
    [Tooltip("Enable Y axis rotation")]
    public bool rotateY = true;
    public float minY = -1f;
    public float maxY = 1f;
    
    [Tooltip("Enable Z axis rotation")]
    public bool rotateZ = false;
    public float minZ = -1f;
    public float maxZ = 1f;
    
    [Header("Animation Settings")]
    [Tooltip("Duration of one complete rotation cycle")]
    public float duration = 2f;
    
    [Tooltip("Start animating on start")]
    public bool animateOnStart = true;
    
    [Tooltip("Loop the animation")]
    public bool loop = true;

    private RectTransform rectTransform;
    private Vector3 startRotation;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        startRotation = rectTransform.localEulerAngles;
        
        if (animateOnStart)
        {
            StartRotating();
        }
    }

    public void StartRotating()
    {
        StartCoroutine(RotateRoutine());
    }

    private IEnumerator RotateRoutine()
    {
        do
        {
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Ping pong for smooth back and forth
                float pingPong = Mathf.PingPong(t * 2f, 1f);
                
                Vector3 newRotation = startRotation;
                
                if (rotateX)
                {
                    newRotation.x = Mathf.Lerp(minX, maxX, pingPong);
                }
                
                if (rotateY)
                {
                    newRotation.y = Mathf.Lerp(minY, maxY, pingPong);
                }
                
                if (rotateZ)
                {
                    newRotation.z = Mathf.Lerp(minZ, maxZ, pingPong);
                }
                
                rectTransform.localEulerAngles = newRotation;
                
                yield return null;
            }
            
        } while (loop);
    }
} 