using UnityEngine;
using UnityEngine.UI;

namespace Puzzle
{
    /// <summary>
    /// Visualizes the freeze meter as a fill-type Image. Single job: keep the
    /// bar's fillAmount in sync with FreezeMeterValue.
    /// </summary>
    public class FreezeMeterUI : MonoBehaviour
    {
        [SerializeField] private FreezeMeterValue meterValue;
        [SerializeField] private Image fillImage;

        private void OnEnable()
        {
            if (meterValue != null) meterValue.OnValueChanged += SetFill;
        }

        private void OnDisable()
        {
            if (meterValue != null) meterValue.OnValueChanged -= SetFill;
        }

        private void SetFill(float normalized01)
        {
            //Debug.Log($"[FreezeMeterUI] Fill set to {normalized01:0.00}");
            if (fillImage != null) fillImage.fillAmount = normalized01;
        }
    }
}
