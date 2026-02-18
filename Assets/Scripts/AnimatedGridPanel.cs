using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class AnimatedGridPanel : MonoBehaviour
{
    [CoolHeader("anim grid")]
    [Header("Grid Texture")]
    [Tooltip("Small tileable grid texture (e.g., 16x16px with X/Y lines)")]
    public Texture2D gridTexture;
    
    [Header("Scroll Settings")]
    [Tooltip("Scroll speed on X axis")]
    public float scrollSpeedX = 0.02f;
    
    [Tooltip("Scroll speed on Y axis")]
    public float scrollSpeedY = 0.015f;
    
    [Header("Visual Settings")]
    [Range(0f, 1f)]
    [Tooltip("Opacity of the grid overlay")]
    public float gridOpacity = 0.5f;
    
    [Tooltip("Background color (dark greyish)")]
    public Color backgroundColor = new Color(0.1f, 0.1f, 0.12f, 1f);
    
    [Tooltip("X-axis line color")]
    public Color xAxisColor = new Color(0.4f, 0.8f, 1f, 1f); // Blue tint
    
    [Tooltip("Y-axis line color")]
    public Color yAxisColor = new Color(1f, 0.4f, 0.4f, 1f); // Red tint
    
    [Tooltip("Grid line color (white/greyish)")]
    public Color gridLineColor = new Color(0.7f, 0.7f, 0.75f, 1f);
    
    [Header("Pixelation & Dithering")]
    [Tooltip("Enable dithering effect")]
    public bool enableDithering = true;
    
    [Range(0f, 1f)]
    [Tooltip("Dithering intensity")]
    public float ditheringIntensity = 0.5f;
    
    [Tooltip("Apply dithering to grid lines (X-shaped dithered lines)")]
    public bool ditherGridLines = true;
    
    [Tooltip("Optional noise texture for dithering (leave null for procedural)")]
    public Texture2D noiseTexture;
    
    // Internal
    private RawImage rawImage;
    private Vector2 scrollOffset;
    private Material material;
    
    // Dithering pattern (Bayer 4x4 matrix)
    private static readonly float[,] bayerMatrix = new float[4, 4]
    {
        { 0f/16f, 8f/16f, 2f/16f, 10f/16f },
        { 12f/16f, 4f/16f, 14f/16f, 6f/16f },
        { 3f/16f, 11f/16f, 1f/16f, 9f/16f },
        { 15f/16f, 7f/16f, 13f/16f, 5f/16f }
    };

    void Start()
    {
        rawImage = GetComponent<RawImage>();
        
        // Auto-generate grid texture if none assigned
        if (gridTexture == null)
        {
            GenerateSimpleGridTexture(32);
        }
        
        // Set up the RawImage
        rawImage.texture = gridTexture;
        material = rawImage.material;
        
        // Enable tiling
        rawImage.uvRect = new Rect(0, 0, 10, 10); // Initial tiling amount
    }

    void Update()
    {
        // Smooth continuous scrolling - just accumulate offset
        scrollOffset.x += scrollSpeedX * Time.deltaTime;
        scrollOffset.y += scrollSpeedY * Time.deltaTime;
        
        // Update UV rect for smooth scrolling (no modulo, let it run free)
        rawImage.uvRect = new Rect(scrollOffset.x, scrollOffset.y, 10, 10);
    }

    // Helper method to create a simple grid texture if none is assigned
    public void GenerateSimpleGridTexture(int size = 32)
    {
        gridTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        float gridLineThickness = 1f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color pixelColor = backgroundColor;
                
                // Create main axis lines
                bool isXAxis = Mathf.Abs(y - size / 2f) < gridLineThickness;
                bool isYAxis = Mathf.Abs(x - size / 2f) < gridLineThickness;
                bool isEdge = (x < gridLineThickness || y < gridLineThickness);
                
                // Apply dithering to grid lines if enabled
                if (ditherGridLines && (isXAxis || isYAxis || isEdge))
                {
                    float ditherValue = bayerMatrix[x % 4, y % 4];
                    bool ditherOn = ditherValue > 0.4f;
                    
                    if (ditherOn)
                    {
                        if (isXAxis)
                        {
                            pixelColor = Color.Lerp(backgroundColor, xAxisColor, gridOpacity);
                        }
                        else if (isYAxis)
                        {
                            pixelColor = Color.Lerp(backgroundColor, yAxisColor, gridOpacity);
                        }
                        else if (isEdge)
                        {
                            pixelColor = Color.Lerp(backgroundColor, gridLineColor, gridOpacity);
                        }
                    }
                }
                else if (!ditherGridLines)
                {
                    // Solid lines
                    if (isXAxis)
                    {
                        pixelColor = Color.Lerp(backgroundColor, xAxisColor, gridOpacity * 0.8f);
                    }
                    else if (isYAxis)
                    {
                        pixelColor = Color.Lerp(backgroundColor, yAxisColor, gridOpacity * 0.8f);
                    }
                    else if (isEdge)
                    {
                        pixelColor = Color.Lerp(backgroundColor, gridLineColor, gridOpacity);
                    }
                }
                
                // Apply global dithering
                if (enableDithering)
                {
                    float ditherValue = bayerMatrix[x % 4, y % 4];
                    float dither = (ditherValue - 0.5f) * ditheringIntensity * 0.15f;
                    pixelColor.r = Mathf.Clamp01(pixelColor.r + dither);
                    pixelColor.g = Mathf.Clamp01(pixelColor.g + dither);
                    pixelColor.b = Mathf.Clamp01(pixelColor.b + dither);
                }
                
                pixels[y * size + x] = pixelColor;
            }
        }
        
        gridTexture.SetPixels(pixels);
        gridTexture.Apply();
        gridTexture.filterMode = FilterMode.Point;
        gridTexture.wrapMode = TextureWrapMode.Repeat;
    }
    
    void OnDestroy()
    {
        if (gridTexture != null)
        {
            Destroy(gridTexture);
        }
    }
}