using System;
using System.Collections;
using UnityEngine;

namespace AvoidTheCold
{
    /// <summary>
    /// Pure countdown timer. Knows nothing about the puzzle - just counts down
    /// from Duration and reports remaining time / expiry. Ticks via a
    /// coroutine that only exists while actively counting down, rather than
    /// polling every frame through Update regardless of game state.
    /// </summary>
    public class CountdownTimer : MonoBehaviour
    {
        [SerializeField] private float duration;
        [SerializeField] private bool autoStart = true;

        public event Action<float> OnTick;   // remaining seconds
        public event Action OnExpired;

        private float _remaining;
        private bool _isRunning;
        private int _lastLoggedSecond = -1;
        private Coroutine _tickRoutine;

        public float RemainingSeconds => _remaining;
        public float Duration => duration;

        private void Start()
        {
            if (autoStart) StartTimer();
        }

        private void OnDisable()
        {
            StopTimer();
        }

        /// <summary>Sets the countdown length for the next StartTimer() call.</summary>
        public void SetDuration(float seconds)
        {
            duration = Mathf.Max(0f, seconds);
        }

        public void StartTimer()
        {
            if (_tickRoutine != null) StopCoroutine(_tickRoutine);

            _remaining = duration;
            _isRunning = true;
            _lastLoggedSecond = -1;
            Debug.Log($"[CountdownTimer] Started: {duration}s");

            _tickRoutine = StartCoroutine(TickLoop());
        }

        

        public void StopTimer()
        {
            _isRunning = false;

            if (_tickRoutine != null)
            {
                StopCoroutine(_tickRoutine);
                _tickRoutine = null;
            }
        }

        /// <summary>
        /// Runs once per frame - same cadence as Update - but the coroutine
        /// only exists between StartTimer() and StopTimer()/expiry, so there
        /// is no per-frame cost while the puzzle isn't actually playable
        /// (before start, after win/lose, between levels).
        /// </summary>
        private IEnumerator TickLoop()
        {
            while (_isRunning)
            {
                yield return null;

                _remaining = Mathf.Max(0f, _remaining - Time.deltaTime);
                OnTick?.Invoke(_remaining);

                int wholeSecond = Mathf.CeilToInt(_remaining);
                if (wholeSecond != _lastLoggedSecond)
                {
                    _lastLoggedSecond = wholeSecond;
                    //Debug.Log($"[CountdownTimer] {wholeSecond}s remaining");
                }

                if (_remaining <= 0f)
                {
                    _isRunning = false;
                    //Debug.Log("[CountdownTimer] Expired");
                    OnExpired?.Invoke();
                }
            }

            _tickRoutine = null;
        }
    }
}
    