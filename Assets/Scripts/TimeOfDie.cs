using UnityEngine;
using TMPro;

public class TimeOfDie : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI timerText;
    [SerializeField] UIScript ui;
    [SerializeField] int start = 10;
    [SerializeField] int end = 0;

    private float timer;
    private bool isCounting;

    void Start()
    {
        timer = start;
        isCounting = true;
        UpdateTimerText();
    }

    void Update()
    {
        if (isCounting && timer > end)
        {
            timer -= Time.deltaTime;
            UpdateTimerText();

            if (timer <= end)
            {
                timer = end;
                isCounting = false;
                ui.GameOver();
            }
        }
    }

    void UpdateTimerText()
    {
        timerText.text = Mathf.CeilToInt(timer).ToString();
    }

    public void ResetTimer()
    {
        timer = start;
        isCounting = true;
        UpdateTimerText();
    }
}
