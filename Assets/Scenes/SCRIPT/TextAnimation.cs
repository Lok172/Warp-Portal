using System.Collections;
using TMPro;
using UnityEngine;

public class TextAnimation : MonoBehaviour
{
    public TextMeshProUGUI textDisplay;

    [TextArea]
    public string[] messages;

    public float typingSpeed = 0.05f;

    private int currentMessage = 0;
    private Coroutine typingCoroutine;

    public void PlayNextText()
    {
        if (messages.Length == 0)
            return;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(messages[currentMessage]));

        currentMessage++;

        if (currentMessage >= messages.Length)
            currentMessage = 0; // Loop back to first message
    }

    IEnumerator TypeText(string text)
    {
        textDisplay.text = "";

        foreach (char letter in text)
        {
            textDisplay.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
    }
}