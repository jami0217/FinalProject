using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TreadmillGame : MonoBehaviour
{
    public Slider progressBar;
    public Slider staminaBar;
    public TMP_Text resultText;

    string lastKey = "";
    bool isPlaying = true;

    void Start()
    {
        progressBar.value = 0f;
        staminaBar.value = 1f;
        resultText.text = "";
    }

    void Update()
    {
        if (!isPlaying) return;

        staminaBar.value -= 0.5f * Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.A) && lastKey != "A")
        {
            Step("A");
        }
        if (Input.GetKeyDown(KeyCode.D) && lastKey != "D")
        {
            Step("D");
        }

        if (progressBar.value >= 1f)
        {
            isPlaying = false;
            resultText.text = "SUCCESS!";
        }
        else if (staminaBar.value <= 0f)
        {
            isPlaying = false;
            resultText.text = "FAIL...";
        }
    }

    void Step(string key)
    {
        lastKey = key;
        progressBar.value += 0.03f;
        staminaBar.value += 0.01f;
    }
}