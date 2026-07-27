using TMPro;
using UnityEngine;

namespace AvoidTheCold
{
    /// <summary>
    /// Displays the CountdownTimer's remaining time as text. Single job:
    /// format seconds and push them to a TextMeshProUGUI label.
    /// </summary>
    public class TimerDisplayUI : MonoBehaviour
    {
        [SerializeField] private CountdownTimer timer;
        [SerializeField] private TextMeshProUGUI timerText;

        private void OnEnable()
        {
            if (timer != null) timer.OnTick += HandleTick;
        }

        private void OnDisable()
        {
            if (timer != null) timer.OnTick -= HandleTick;
        }

        private void HandleTick(float remainingSeconds)
        {
            if (timerText == null) return;

            int whole = Mathf.CeilToInt(remainingSeconds);
            timerText.text = $"{whole}s";
        }
    }
}
