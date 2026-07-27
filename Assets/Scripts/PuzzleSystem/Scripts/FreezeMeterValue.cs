using System;
using UnityEngine;

namespace AvoidTheCold
{
    /// <summary>
    /// Single source of truth for the freeze meter's 0..1 value. Drains
    /// continuously over time (paced by CountdownTimer.Duration). Placing
    /// pieces no longer refills it - all pieces must be placed before it
    /// runs out.
    /// </summary>
    public class FreezeMeterValue : MonoBehaviour
    {
        [SerializeField] private CountdownTimer timer;

        public event Action<float> OnValueChanged; // 0..1
        public event Action OnDepleted;

        private float _value = 1f;
        private bool _isActive = true;

        public float Value => _value;

        /// <summary>Resets the meter to full for a fresh level attempt.</summary>
        public void ResetValue()
        {
            _isActive = true;
            _value = 1f;
            OnValueChanged?.Invoke(_value);
        }

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
            if (!_isActive || timer == null || timer.Duration <= 0f) return;

            SetValue(_value - (Time.deltaTime / timer.Duration));
        }

        private void SetValue(float newValue)
        {
            newValue = Mathf.Clamp01(newValue);
            if (Mathf.Approximately(newValue, _value)) return;

            _value = newValue;
            OnValueChanged?.Invoke(_value);

            if (_value <= 0f && _isActive)
            {
                _isActive = false;
                Debug.Log("[FreezeMeterValue] Depleted");
                OnDepleted?.Invoke();
            }
        }
    }
}
