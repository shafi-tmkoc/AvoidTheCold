using UnityEngine;

namespace AvoidTheCold
{
    /// <summary>
    /// Drives the hand-tutorial hint through the first few pieces of level 1
    /// (in level-data order), advancing to the next piece each time the
    /// current one is correctly placed, then stopping for good. Only ever
    /// runs once - the very first time the player reaches level 1.
    /// </summary>
    public class LevelTutorialSequencer : MonoBehaviour
    {
        [SerializeField] private HandTutorialUI handTutorialUI;
        [Tooltip("How many pieces (in level data order) get the hand hint before it stops for good")]
        [SerializeField] private int piecesToHint = 2;

        private DraggableShape[] _pieces;
        private ShapeDropSlot[] _slots;
        private ShapeDropSlot _subscribedSlot;
        private int _hintIndex;
        private bool _active;

        /// <summary>
        /// Call after spawning a level's pieces/slots. Does nothing unless
        /// this is level 1 and the hint has never been shown before.
        /// </summary>
        public void BeginForFirstLevel(int levelNumber, DraggableShape[] pieces, ShapeDropSlot[] slots)
        {
            StopSequence();

            if (levelNumber != 1 || LevelProgressStore.HasSeenHandTutorial) return;
            if (handTutorialUI == null || pieces == null || slots == null || pieces.Length == 0) return;

            LevelProgressStore.HasSeenHandTutorial = true;
            LevelProgressStore.Save();

            _pieces = pieces;
            _slots = slots;
            _hintIndex = 0;
            _active = true;

            Debug.Log("[LevelTutorialSequencer] Starting hand tutorial");
            ShowHintFor(_hintIndex);
        }

        private void ShowHintFor(int index)
        {
            if (!_active || index >= _pieces.Length || index >= piecesToHint)
            {
                StopSequence();
                return;
            }

            var piece = _pieces[index];
            var slot = _slots[index];
            if (piece == null || slot == null)
            {
                StopSequence();
                return;
            }

            _subscribedSlot = slot;
            _subscribedSlot.OnFilled += HandleSlotFilled;

            var pieceRect = (RectTransform)piece.transform;
            var slotRect = (RectTransform)slot.transform;
            handTutorialUI.Show(pieceRect.anchoredPosition, slotRect.anchoredPosition);
        }

        private void HandleSlotFilled(ShapeDropSlot filledSlot)
        {
            if (_subscribedSlot != null) _subscribedSlot.OnFilled -= HandleSlotFilled;
            _subscribedSlot = null;

            if (!_active) return;

            Debug.Log($"[LevelTutorialSequencer] Hinted piece {_hintIndex} placed correctly");
            _hintIndex++;
            ShowHintFor(_hintIndex);
        }

        private void StopSequence()
        {
            if (_subscribedSlot != null)
            {
                _subscribedSlot.OnFilled -= HandleSlotFilled;
                _subscribedSlot = null;
            }

            _active = false;
            if (handTutorialUI != null) handTutorialUI.Hide();
        }

        private void OnDisable() => StopSequence();
    }
}
