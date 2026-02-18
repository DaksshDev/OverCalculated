using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class Card : MonoBehaviour
{
    [CoolHeader("CARD!")]
    [Header("Card Type")]
    [SerializeField] private CardType cardType;
    
    [Header("Question Range Settings")]
    [SerializeField] private int minValue = 1;
    [SerializeField] private int maxValue = 20;
    
    [Header("Hidden Sprite")]
    [SerializeField] private Image hiddenSpriteWhite;
    [SerializeField] private Image hiddenSpriteYellow;

    private CardTextComponents textComponents;
    private float correctAnswer;
    private int currentLevel = 1;
    private float questionDifficulty = 1f; // 0-1 scale, used for timer calculation
    
    // For better randomization
    private static System.Random randomGen = new System.Random();
    
    void Awake()
    {
        FindTextComponents();
    }

    public void SetLevel(int level)
    {
        currentLevel = level;
    }
    
    public float GetQuestionDifficulty()
    {
        return questionDifficulty;
    }

    public void SetHiddenSprite(bool useWhite)
    {
        if (useWhite)
        {
            if (hiddenSpriteWhite != null)
                hiddenSpriteWhite.gameObject.SetActive(true);
            if (hiddenSpriteYellow != null)
                hiddenSpriteYellow.gameObject.SetActive(false);
        }
        else
        {
            if (hiddenSpriteWhite != null)
                hiddenSpriteWhite.gameObject.SetActive(false);
            if (hiddenSpriteYellow != null)
                hiddenSpriteYellow.gameObject.SetActive(true);
        }
    }

    public void RestoreOriginalSprite()
    {
        if (hiddenSpriteWhite != null)
            hiddenSpriteWhite.gameObject.SetActive(false);
        if (hiddenSpriteYellow != null)
            hiddenSpriteYellow.gameObject.SetActive(false);
    }
    
    public void GenerateQuestion()
    {
        switch (cardType)
        {
            case CardType.Add:
                GenerateAddQuestion();
                break;
            case CardType.Subtract:
                GenerateSubtractQuestion();
                break;
            case CardType.Multiply:
                GenerateMultiplyQuestion();
                break;
            case CardType.Division:
                GenerateDivisionQuestion();
                break;
            case CardType.Square:
                GenerateSquareQuestion();
                break;
            case CardType.Cube:
                GenerateCubeQuestion();
                break;
            case CardType.SquareRoot:
                GenerateSquareRootQuestion();
                break;
            case CardType.CubeRoot:
                GenerateCubeRootQuestion();
                break;
            case CardType.Absolute:
                GenerateAbsoluteQuestion();
                break;
            case CardType.Double:
                GenerateDoubleQuestion();
                break;
            case CardType.Half:
                GenerateHalfQuestion();
                break;
        }
    }
    
    public float GetCorrectAnswer()
    {
        return correctAnswer;
    }
    
    public CardType GetCardType()
    {
        return cardType;
    }
    
    public void DisableAllText()
    {
        foreach (Transform child in transform)
        {
            TextMeshProUGUI tmp = child.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.enabled = false; 
            }
        }
    }
    
    void FindTextComponents()
    {
        textComponents = new CardTextComponents();
        
        foreach (Transform child in transform)
        {
            TextMeshProUGUI tmp = child.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                switch (child.name)
                {
                    case "q": textComponents.question = tmp; break;
                    case "1": textComponents.topLeft = tmp; break;
                    case "2": textComponents.bottomRight = tmp; break;
                    case "n": textComponents.numerator = tmp; break;
                    case "d": textComponents.denominator = tmp; break;
                    case "h": textComponents.hint = tmp; break;
                }
            }
        }
    }
    
    int GetHumanRandom(int min, int max)
    {
        return randomGen.Next(min, max);
    }
    
    // Evaluate how hard a question is for humans (0 = super easy, 1 = hard)
    float EvaluateAdditionDifficulty(int num1, int num2)
    {
        // Super easy: single digit, or multiples of 10, or one number is 1
        if (num1 == 1 || num2 == 1) return 0.1f; // 18 + 1 = super easy
        if ((num1 < 10 && num2 < 10)) return 0.2f; // 5 + 3 = very easy
        if (num1 % 10 == 0 && num2 % 10 == 0) return 0.2f; // 10 + 20 = easy
        if (num1 % 10 == 0 || num2 % 10 == 0) return 0.3f; // 10 + 15 = fairly easy
        if (num1 < 20 && num2 < 20) return 0.4f; // 15 + 12 = medium-easy
        if (num1 < 50 && num2 < 50) return 0.6f; // 35 + 42 = medium
        return 0.8f; // 75 + 88 = harder
    }
    
    float EvaluateSubtractionDifficulty(int num1, int num2)
    {
        if (num2 == 1) return 0.1f; // 18 - 1, 20 - 1 = super easy
        if (num1 < 10 && num2 < 10) return 0.2f; // 9 - 3 = very easy
        if (num1 % 10 == 0 && num2 % 10 == 0) return 0.2f; // 50 - 20 = easy
        if (num1 % 10 == 0 || num2 % 10 == 0) return 0.3f; // 50 - 15 = fairly easy
        if (num1 < 30 && num2 < 20) return 0.4f; // 25 - 12 = medium-easy
        if (num1 < 60 && num2 < 40) return 0.6f; // 55 - 32 = medium
        return 0.8f; // Larger numbers = harder
    }
    
    float EvaluateMultiplicationDifficulty(int num1, int num2)
    {
        // Super easy: multiply by 1 or 2
        if (num1 == 1 || num2 == 1) return 0.1f; // 7 x 1 = instant
        if (num1 == 2 || num2 == 2) return 0.2f; // 2 x 10 = very easy
        
        // Easy: multiply by 5 or 10, or small numbers (3-7) x single digit
        if (num1 == 5 || num2 == 5 || num1 == 10 || num2 == 10) return 0.25f; // 5 x 4, 10 x 3 = easy
        
        // Medium-easy: 3-7 times single digit
        if ((num1 >= 3 && num1 <= 7 && num2 <= 10) || (num2 >= 3 && num2 <= 7 && num1 <= 10)) 
            return 0.35f; // 6 x 7 = medium-easy
        
        // Medium: 8-9 times single digit
        if ((num1 <= 9 && num2 <= 9)) return 0.5f; // 8 x 9 = medium
        
        // Medium-hard: double digit x single digit
        if ((num1 >= 11 && num1 <= 15 && num2 <= 10) || (num2 >= 11 && num2 <= 15 && num1 <= 10))
            return 0.65f; // 12 x 5 = medium-hard
        
        // Hard: double digit x double digit
        return 0.85f; // 13 x 14 = hard
    }
    
    float EvaluateDivisionDifficulty(int dividend, int divisor)
    {
        // Super easy: divide by 1 or 2
        if (divisor == 1) return 0.1f; // anything ÷ 1 = instant
        if (divisor == 2) return 0.2f; // 20 ÷ 2 = very easy
        
        // Easy: divide by 5 or 10
        if (divisor == 5 || divisor == 10) return 0.25f; // 50 ÷ 10 = easy
        
        // Medium-easy: divide by 3-7, small dividends
        if (divisor >= 3 && divisor <= 7 && dividend <= 50) return 0.4f; // 24 ÷ 4 = medium-easy
        
        // Medium: divide by 3-10, medium dividends
        if (divisor <= 10 && dividend <= 100) return 0.5f; // 72 ÷ 8 = medium
        
        // Medium-hard: divide by 11-20
        if (divisor <= 20 && dividend <= 150) return 0.7f; // 96 ÷ 12 = medium-hard
        
        return 0.85f; // Larger numbers = hard
    }
    
    void GenerateAddQuestion()
    {
        int num1, num2;
        
        if (currentLevel == 1)
        {
            num1 = GetHumanRandom(1, 10);
            num2 = GetHumanRandom(1, 10);
        }
        else if (currentLevel == 2)
        {
            int chance = GetHumanRandom(0, 100);
            if (chance < 70)
            {
                num1 = GetHumanRandom(1, 10);
                num2 = GetHumanRandom(1, 10);
            }
            else
            {
                num1 = GetHumanRandom(10, 30);
                num2 = GetHumanRandom(1, 20);
            }
        }
        else if (currentLevel <= 4)
        {
            int chance = GetHumanRandom(0, 100);
            if (chance < 30)
            {
                num1 = GetHumanRandom(1, 10);
                num2 = GetHumanRandom(1, 10);
            }
            else
            {
                num1 = GetHumanRandom(10, 50);
                num2 = GetHumanRandom(10, 50);
            }
        }
        else if (currentLevel <= 7)
        {
            num1 = GetHumanRandom(10, 70);
            num2 = GetHumanRandom(10, 70);
        }
        else
        {
            num1 = GetHumanRandom(50, 150);
            num2 = GetHumanRandom(50, 150);
        }
        
        correctAnswer = num1 + num2;
        questionDifficulty = EvaluateAdditionDifficulty(num1, num2);
        
        if (textComponents.topLeft != null) textComponents.topLeft.text = num1.ToString();
        if (textComponents.bottomRight != null) textComponents.bottomRight.text = num2.ToString();
        if (textComponents.question != null) textComponents.question.text = $"{num1} + {num2}";
    }
    
    void GenerateSubtractQuestion()
    {
        int num1, num2;
        
        if (currentLevel == 1)
        {
            num1 = GetHumanRandom(5, 10);
            num2 = GetHumanRandom(1, num1);
        }
        else if (currentLevel == 2)
        {
            int chance = GetHumanRandom(0, 100);
            if (chance < 70)
            {
                num1 = GetHumanRandom(5, 10);
                num2 = GetHumanRandom(1, num1);
            }
            else
            {
                num1 = GetHumanRandom(10, 30);
                num2 = GetHumanRandom(1, num1);
            }
        }
        else if (currentLevel <= 4)
        {
            int chance = GetHumanRandom(0, 100);
            if (chance < 30)
            {
                num1 = GetHumanRandom(5, 10);
                num2 = GetHumanRandom(1, num1);
            }
            else
            {
                num1 = GetHumanRandom(10, 60);
                num2 = GetHumanRandom(1, num1);
            }
        }
        else if (currentLevel <= 7)
        {
            num1 = GetHumanRandom(20, 80);
            num2 = GetHumanRandom(1, num1);
        }
        else
        {
            num1 = GetHumanRandom(100, 200);
            num2 = GetHumanRandom(1, num1);
        }
        
        correctAnswer = num1 - num2;
        questionDifficulty = EvaluateSubtractionDifficulty(num1, num2);
        
        if (textComponents.topLeft != null) textComponents.topLeft.text = num1.ToString();
        if (textComponents.bottomRight != null) textComponents.bottomRight.text = num2.ToString();
        if (textComponents.question != null) textComponents.question.text = $"{num1} - {num2}";
    }
    
    void GenerateMultiplyQuestion()
    {
        int num1, num2;
        
        if (currentLevel <= 7)
        {
            num1 = GetHumanRandom(2, 10);
            num2 = GetHumanRandom(2, 10);
        }
        else
        {
            int chance = GetHumanRandom(0, 100);
            if (chance < 50)
            {
                num1 = GetHumanRandom(2, 10);
                num2 = GetHumanRandom(2, 10);
            }
            else
            {
                num1 = GetHumanRandom(11, 16);
                num2 = GetHumanRandom(11, 16);
            }
        }
        
        correctAnswer = num1 * num2;
        questionDifficulty = EvaluateMultiplicationDifficulty(num1, num2);
        
        if (textComponents.topLeft != null) textComponents.topLeft.text = num1.ToString();
        if (textComponents.bottomRight != null) textComponents.bottomRight.text = num2.ToString();
        if (textComponents.question != null) textComponents.question.text = $"{num1} × {num2}";
    }
    
    void GenerateDivisionQuestion()
    {
        int divisor, result;
        
        if (currentLevel <= 7)
        {
            divisor = GetHumanRandom(2, 11);
            result = GetHumanRandom(2, 10);
        }
        else
        {
            divisor = GetHumanRandom(2, 21);
            result = GetHumanRandom(2, 15);
        }
        
        int dividend = divisor * result;
        correctAnswer = result;
        questionDifficulty = EvaluateDivisionDifficulty(dividend, divisor);
        
        if (textComponents.numerator != null) textComponents.numerator.text = dividend.ToString();
        if (textComponents.denominator != null) textComponents.denominator.text = divisor.ToString();
        if (textComponents.topLeft != null) textComponents.topLeft.text = dividend.ToString();
        if (textComponents.bottomRight != null) textComponents.bottomRight.text = divisor.ToString();
        if (textComponents.hint != null) textComponents.hint.text = "";
    }
    
    void GenerateSquareQuestion()
    {
        int num = GetHumanRandom(2, 12);
        correctAnswer = num * num;
        
        // Squares are medium difficulty
        if (num <= 5) questionDifficulty = 0.3f; // 2² to 5² = easy
        else if (num <= 10) questionDifficulty = 0.5f; // 6² to 10² = medium
        else questionDifficulty = 0.7f; // 11²+ = harder
        
        if (textComponents.question != null) textComponents.question.text = $"{num}<sup>2</sup>";
    }
    
    void GenerateCubeQuestion()
    {
        int num = GetHumanRandom(2, 7);
        correctAnswer = num * num * num;
        
        // Cubes are harder
        if (num <= 3) questionDifficulty = 0.4f; // 2³, 3³ = medium
        else if (num <= 5) questionDifficulty = 0.6f; // 4³, 5³ = medium-hard
        else questionDifficulty = 0.8f; // 6³+ = hard
        
        if (textComponents.question != null) textComponents.question.text = $"{num}<sup>3</sup>";
    }
    
    void GenerateSquareRootQuestion()
    {
        int num = GetHumanRandom(2, 13);
        correctAnswer = num;
        int square = num * num;
        
        // Roots require recall
        if (num <= 5) questionDifficulty = 0.3f;
        else if (num <= 10) questionDifficulty = 0.5f;
        else questionDifficulty = 0.7f;
        
        if (textComponents.question != null) textComponents.question.text = $"√{square}";
    }
    
    void GenerateCubeRootQuestion()
    {
        int num = GetHumanRandom(2, 7);
        correctAnswer = num;
        int cube = num * num * num;
        
        questionDifficulty = 0.6f; // Cube roots are medium-hard
        
        if (textComponents.question != null) textComponents.question.text = $"<sup>3</sup>√{cube}";
    }
    
    void GenerateAbsoluteQuestion()
    {
        int num = GetHumanRandom(-50, 51);
        correctAnswer = Mathf.Abs(num);
        
        questionDifficulty = 0.2f; // Absolute value is easy
        
        if (textComponents.question != null) textComponents.question.text = $"|{num}|";
    }
    
    void GenerateDoubleQuestion()
    {
        int num = GetHumanRandom(5, 40);
        correctAnswer = num * 2;
        
        questionDifficulty = 0.25f; // Doubling is easy
        
        if (textComponents.question != null) textComponents.question.text = $"{num}";
    }
    
    void GenerateHalfQuestion()
    {
        int num = GetHumanRandom(10, 50) * 2;
        correctAnswer = num / 2;
        
        questionDifficulty = 0.3f; // Halving is fairly easy
        
        if (textComponents.question != null) textComponents.question.text = $"{num} ½";
    }
    
    public void EnableAllText()
    {
        foreach (Transform child in transform)
        {
            TextMeshProUGUI tmp = child.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.enabled = true;
            }
        }
    }
    
    struct CardTextComponents
    {
        public TextMeshProUGUI question;
        public TextMeshProUGUI topLeft;
        public TextMeshProUGUI bottomRight;
        public TextMeshProUGUI numerator;
        public TextMeshProUGUI denominator;
        public TextMeshProUGUI hint;
    }
}

public enum CardType
{
    Add,
    Subtract,
    Multiply,
    Division,
    Square,
    Cube,
    SquareRoot,
    CubeRoot,
    Absolute,
    Double,
    Half
}