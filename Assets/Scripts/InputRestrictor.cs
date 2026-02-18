using UnityEngine;
using TMPro;

public class InputRestrictor : MonoBehaviour
{
    [CoolHeader("Input Restrictor")]
    [SerializeField] private TMP_InputField inputField;

    void Start()
    {
        inputField = GetComponent<TMP_InputField>();
        inputField.onValidateInput += ValidateInput;
    }

    private char ValidateInput(string text, int charIndex, char addedChar)
    {
        // Allow numbers 0-9
        if (addedChar >= '0' && addedChar <= '9')
            return addedChar;

        // Allow + and - signs
        if (addedChar == '+' || addedChar == '-')
            return addedChar;

        // Block all other characters
        return '\0';
    }
}