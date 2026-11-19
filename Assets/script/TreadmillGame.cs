using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TreadmillGame : MonoBehaviour
{
    public Slider progressBar;
    public Slider staminaBar;
    public TMP_Text resultText;
    public GameObject startPanel;
    public GameObject retryButton;

    string lastKey = "";
    bool isPlaying = false;

    void Start()
    {
        ResetValues();
        startPanel.SetActive(true);
    }

    void ResetValues()
    {
        progressBar.value = 0f;
        staminaBar.value = 1f;
        resultText.text = "";
        lastKey = "";
        retryButton.SetActive(false);
    }

    // 給 START 按鈕用
    public void StartGame()
    {
        startPanel.SetActive(false);
        isPlaying = true;
    }

    // 給 RETRY 按鈕用
    public void RetryGame()
    {
        ResetValues();
        isPlaying = true;
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
            EndGame("SUCCESS!");
        }
        else if (staminaBar.value <= 0f)
        {
            EndGame("FAIL...");
        }
    }

    void Step(string key)
    {
        lastKey = key;
        progressBar.value += 0.03f;
        staminaBar.value += 0.01f;
    }

    void EndGame(string message)
    {
        isPlaying = false;
        resultText.text = message;
        retryButton.SetActive(true);
    }
}