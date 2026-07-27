using UnityEngine;
using UnityEngine.InputSystem;

namespace AvoidTheCold
{
    /// <summary>
    /// Nudges the player with the hand hint whenever the screen has been idle
    /// (no touch/pointer input) for idleThreshold seconds, pointing at the
    /// next unplaced piece. Hides the instant the player touches the screen,
    /// and shows again after another idleThreshold seconds of inactivity.
    /// Works every level. Stays out of the way while LevelTutorialSequencer's
    /// one-time first-run sequence is still actively guiding the player, so
    /// the two never fight over the shared hand image.
    /// </summary>
    public class IdleHandTutorial : MonoBehaviour
    {
        [SerializeField] private HandTutorialUI handTutorialUI;
        [SerializeField] private LevelTutorialSequencer tutorialSequencer;
        [SerializeField] private float idleThreshold = 7f;

        private DraggableShape[] _pieces = System.Array.Empty<DraggableShape>();
        private ShapeDropSlot[] _slots = System.Array.Empty<ShapeDropSlot>();
        private bool[] _filled = System.Array.Empty<bool>();

        private float _idleTimer;
        private bool _isShowingHint;

        /// <summary>Call after spawning a fresh set of pieces/slots for a level attempt.</summary>
        public void SetPieces(DraggableShape[] pieces, ShapeDropSlot[] slots)
        {
            _pieces = pieces ?? System.Array.Empty<DraggableShape>();
            _slots = slots ?? System.Array.Empty<ShapeDropSlot>();
            _filled = new bool[_slots.Length];

            for (int i = 0; i < _slots.Length; i++)
            {
                int capturedIndex = i;
                if (_slots[i] != null) _slots[i].OnFilled += _ => HandleSlotFilled(capturedIndex);
            }

            _idleTimer = 0f;
            HideHint();
        }

        private void OnDisable()
        {
            HideHint();
        }

        private void Update()
        {
            if (AnyTouchThisFrame())
            {
                _idleTimer = 0f;
                if (_isShowingHint) HideHint();
                return;
            }

            if (_isShowingHint) return; // already showing - wait for a touch or the piece being placed
            if (tutorialSequencer != null && tutorialSequencer.IsActive) return; // let the first-run tutorial finish first

            _idleTimer += Time.deltaTime;
            if (_idleTimer >= idleThreshold)
            {
                ShowHintForNextUnplacedPiece();
            }
        }

        private bool AnyTouchThisFrame()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
            return false;
        }

        private void ShowHintForNextUnplacedPiece()
        {
            int index = FindNextUnplacedIndex();
            if (index < 0 || handTutorialUI == null) return;

            var piece = _pieces[index];
            var slot = _slots[index];
            if (piece == null || slot == null) return;

            Debug.Log($"[IdleHandTutorial] Idle for {idleThreshold}s - nudging piece {index}");
            _isShowingHint = true;

            var pieceRect = (RectTransform)piece.transform;
            var slotRect = (RectTransform)slot.transform;
            handTutorialUI.Show(pieceRect.anchoredPosition, slotRect.anchoredPosition);
        }

        private int FindNextUnplacedIndex()
        {
            for (int i = 0; i < _filled.Length; i++)
            {
                if (!_filled[i]) return i;
            }
            return -1;
        }

        private void HandleSlotFilled(int index)
        {
            if (index >= 0 && index < _filled.Length) _filled[index] = true;

            _idleTimer = 0f;
            if (_isShowingHint) HideHint();
        }

        private void HideHint()
        {
            _isShowingHint = false;
            if (handTutorialUI != null) handTutorialUI.Hide();
        }
    }
}
