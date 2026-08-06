using System.Collections;
using UnityEngine;

namespace AvoidTheCold
{
    /// <summary>
    /// Loops a simple placeholder hint indicator back and forth between two
    /// anchored positions (a piece's tray spot and its target slot) until
    /// told to stop. Swap the Image sprite for real hand-pointer art later -
    /// nothing else needs to change.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class HandTutorialUI : MonoBehaviour
    {
        [SerializeField] private float moveDuration = 0.9f;
        [SerializeField] private float pauseAtEnds = 0.3f;

        private RectTransform _rectTransform;
        private Coroutine _loopRoutine;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
        }

        /// <summary>Starts (or restarts) the looping hint between these two points.</summary>
        public void Show(Vector2 fromPos, Vector2 toPos)
        {
            Debug.Log("[HandTutorialUI] Showing hint");
            //transform.SetAsLastSibling(); // stay above pieces/slots spawned after this
            gameObject.SetActive(true);

            if (_loopRoutine != null) StopCoroutine(_loopRoutine);
            _loopRoutine = StartCoroutine(LoopBetween(fromPos, toPos));
        }

        /// <summary>Stops looping and hides the hint.</summary>
        public void Hide()
        {
            Debug.Log("[HandTutorialUI] Hiding hint");

            if (_loopRoutine != null)
            {
                StopCoroutine(_loopRoutine);
                _loopRoutine = null;
            }
            gameObject.SetActive(false);
        }

        private IEnumerator LoopBetween(Vector2 fromPos, Vector2 toPos)
        {
            while (true)
            {
                _rectTransform.anchoredPosition = fromPos;
                yield return new WaitForSeconds(pauseAtEnds);

                float t = 0f;
                while (t < moveDuration)
                {
                    t += Time.deltaTime;
                    _rectTransform.anchoredPosition = Vector2.Lerp(fromPos, toPos, t / moveDuration);
                    yield return null;
                }

                _rectTransform.anchoredPosition = toPos;
                yield return new WaitForSeconds(pauseAtEnds);
            }
        }
    }
}
