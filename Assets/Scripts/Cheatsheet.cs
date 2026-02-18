using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class Cheatsheet : MonoBehaviour
{
    [CoolHeader("CHEATSHEET DISPLAY")]
    [Header("Navigation Buttons")]
    [SerializeField] private Button pageNextButton;
    [SerializeField] private Button pageBackButton;
    [SerializeField] private Button typeNextButton;
    [SerializeField] private Button typeBackButton;

    [Header("Page Label")]
    [SerializeField] private TextMeshProUGUI pageIndexLabel;

    [Header("Sections")]
    [SerializeField] private GameObject tableSection;
    [SerializeField] private GameObject squareSection;
    [SerializeField] private GameObject cubeSection;

    [Header("Tables Setup")]
    [SerializeField] private GameObject tableItemPrefab;
    [SerializeField] private Transform tableParent;

    [Header("Squares Setup")]
    [SerializeField] private GameObject squareItemPrefab;
    [SerializeField] private Transform squareParent;

    [Header("Cubes Setup")]
    [SerializeField] private GameObject cubeItemPrefab;
    [SerializeField] private Transform cubeParent;

    [Header("Settings")]
    [SerializeField] private int tablesPerPage = 6;
    [SerializeField] private int squaresPerPage = 20;
    [SerializeField] private int cubesPerPage = 20;

    private enum MathType { Tables, Squares, Cubes }
    private MathType currentType = MathType.Tables;
    private int currentPage = 0;

    private List<Transform> allTablePages = new List<Transform>();
    private List<Transform> allSquarePages = new List<Transform>();
    private List<Transform> allCubePages = new List<Transform>();

    void Start()
    {
        SetupButtons();
        GenerateAllContent();
        ShowCurrentPage();
    }

    void SetupButtons()
    {
        pageNextButton.onClick.AddListener(NextPage);
        pageBackButton.onClick.AddListener(PrevPage);
        typeNextButton.onClick.AddListener(NextType);
        typeBackButton.onClick.AddListener(PrevType);
    }

    void GenerateAllContent()
    {
        GenerateTables();
        GenerateSquares();
        GenerateCubes();
    }

    // Helper method to clone RectTransform properties
    void CloneRectTransform(RectTransform source, RectTransform target)
    {
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
        target.pivot = source.pivot;
        target.offsetMin = source.offsetMin;
        target.offsetMax = source.offsetMax;
        target.localScale = source.localScale;
        target.localRotation = source.localRotation;
    }

    // Helper method to create a new page with all properties cloned
    Transform CreateNewPage(Transform originalParent, string pageName)
    {
        GameObject newPage = new GameObject(pageName);
        newPage.transform.SetParent(originalParent.parent);
        
        // Add RectTransform and clone all properties
        RectTransform newRT = newPage.AddComponent<RectTransform>();
        RectTransform originalRT = originalParent.GetComponent<RectTransform>();
        CloneRectTransform(originalRT, newRT);

        // Clone GridLayoutGroup if it exists
        GridLayoutGroup originalGrid = originalParent.GetComponent<GridLayoutGroup>();
        if (originalGrid != null)
        {
            GridLayoutGroup newGrid = newPage.AddComponent<GridLayoutGroup>();
            newGrid.cellSize = originalGrid.cellSize;
            newGrid.spacing = originalGrid.spacing;
            newGrid.padding = originalGrid.padding;
            newGrid.constraint = originalGrid.constraint;
            newGrid.constraintCount = originalGrid.constraintCount;
            newGrid.startCorner = originalGrid.startCorner;
            newGrid.startAxis = originalGrid.startAxis;
            newGrid.childAlignment = originalGrid.childAlignment;
        }

        // Clone ContentSizeFitter if it exists
        ContentSizeFitter originalFitter = originalParent.GetComponent<ContentSizeFitter>();
        if (originalFitter != null)
        {
            ContentSizeFitter newFitter = newPage.AddComponent<ContentSizeFitter>();
            newFitter.horizontalFit = originalFitter.horizontalFit;
            newFitter.verticalFit = originalFitter.verticalFit;
        }

        // Clone LayoutElement if it exists
        LayoutElement originalLayout = originalParent.GetComponent<LayoutElement>();
        if (originalLayout != null)
        {
            LayoutElement newLayout = newPage.AddComponent<LayoutElement>();
            newLayout.ignoreLayout = originalLayout.ignoreLayout;
            newLayout.minWidth = originalLayout.minWidth;
            newLayout.minHeight = originalLayout.minHeight;
            newLayout.preferredWidth = originalLayout.preferredWidth;
            newLayout.preferredHeight = originalLayout.preferredHeight;
            newLayout.flexibleWidth = originalLayout.flexibleWidth;
            newLayout.flexibleHeight = originalLayout.flexibleHeight;
        }

        return newPage.transform;
    }

    void GenerateTables()
    {
        allTablePages.Clear();
        Transform currentParent = tableParent;
        allTablePages.Add(currentParent);
        int itemsInCurrentPage = 0;

        for (int num = 2; num <= 30; num++)
        {
            if (itemsInCurrentPage >= tablesPerPage)
            {
                currentParent = CreateNewPage(tableParent, $"TablePage_{allTablePages.Count + 1}");
                allTablePages.Add(currentParent);
                itemsInCurrentPage = 0;
            }

            GameObject item = Instantiate(tableItemPrefab, currentParent);
            Transform elementsParent = item.transform.Find("elements");
            
            if (elementsParent != null)
            {
                TextMeshProUGUI[] texts = elementsParent.GetComponentsInChildren<TextMeshProUGUI>();
                for (int i = 0; i < texts.Length && i < 10; i++)
                {
                    int multiplier = i + 1;
                    texts[i].text = $"{num} × {multiplier} = {num * multiplier}";
                }
            }

            itemsInCurrentPage++;
        }
    }

    void GenerateSquares()
    {
        allSquarePages.Clear();
        Transform currentParent = squareParent;
        allSquarePages.Add(currentParent);
        int itemsInCurrentPage = 0;

        for (int num = 2; num <= 30; num++)
        {
            if (itemsInCurrentPage >= squaresPerPage)
            {
                currentParent = CreateNewPage(squareParent, $"SquarePage_{allSquarePages.Count + 1}");
                allSquarePages.Add(currentParent);
                itemsInCurrentPage = 0;
            }

            GameObject item = Instantiate(squareItemPrefab, currentParent);
            TextMeshProUGUI tmp = item.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = $"{num}² = {num * num}";
            }

            itemsInCurrentPage++;
        }
    }

    void GenerateCubes()
    {
        allCubePages.Clear();
        Transform currentParent = cubeParent;
        allCubePages.Add(currentParent);
        int itemsInCurrentPage = 0;

        for (int num = 2; num <= 20; num++)
        {
            if (itemsInCurrentPage >= cubesPerPage)
            {
                currentParent = CreateNewPage(cubeParent, $"CubePage_{allCubePages.Count + 1}");
                allCubePages.Add(currentParent);
                itemsInCurrentPage = 0;
            }

            GameObject item = Instantiate(cubeItemPrefab, currentParent);
            TextMeshProUGUI tmp = item.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = $"{num}³ = {num * num * num}";
            }

            itemsInCurrentPage++;
        }
    }

    void NextPage()
    {
        if (currentPage < GetMaxPage())
        {
            currentPage++;
            ShowCurrentPage();
        }
    }

    void PrevPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            ShowCurrentPage();
        }
    }

    void NextType()
    {
        currentType = (MathType)(((int)currentType + 1) % 3);
        currentPage = 0;
        ShowCurrentPage();
    }

    void PrevType()
    {
        currentType = (MathType)(((int)currentType - 1 + 3) % 3);
        currentPage = 0;
        ShowCurrentPage();
    }

    int GetMaxPage()
    {
        switch (currentType)
        {
            case MathType.Tables:
                return allTablePages.Count - 1;
            case MathType.Squares:
                return allSquarePages.Count - 1;
            case MathType.Cubes:
                return allCubePages.Count - 1;
            default:
                return 0;
        }
    }

    void ShowCurrentPage()
    {
        // Hide all sections
        tableSection.SetActive(false);
        squareSection.SetActive(false);
        cubeSection.SetActive(false);

        // Hide all pages in all sections
        foreach (var page in allTablePages)
            page.gameObject.SetActive(false);
        foreach (var page in allSquarePages)
            page.gameObject.SetActive(false);
        foreach (var page in allCubePages)
            page.gameObject.SetActive(false);

        // Show current section and page based on type
        switch (currentType)
        {
            case MathType.Tables:
                tableSection.SetActive(true);
                if (currentPage < allTablePages.Count)
                    allTablePages[currentPage].gameObject.SetActive(true);
                break;
            case MathType.Squares:
                squareSection.SetActive(true);
                if (currentPage < allSquarePages.Count)
                    allSquarePages[currentPage].gameObject.SetActive(true);
                break;
            case MathType.Cubes:
                cubeSection.SetActive(true);
                if (currentPage < allCubePages.Count)
                    allCubePages[currentPage].gameObject.SetActive(true);
                break;
        }

        UpdatePageLabel();
    }

    void UpdatePageLabel()
    {
        pageIndexLabel.text = $"Page: {currentPage + 1} / {GetMaxPage() + 1}";
    }
}