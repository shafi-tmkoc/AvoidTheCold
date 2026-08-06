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

        [Tooltip("Suppress the hint while a win/lose banner (with its Retry/Next buttons) is showing")]
        [SerializeField] private ResultScreenUI resultScreenUI;
        [Tooltip("Suppress the hint while the Game Complete banner is showing")]
        [SerializeField] private GameCompleteUI gameCompleteUI;

        [SerializeField] private float idleThreshold = 7f;

        [Tooltip("Canvas that handTutorialUI lives under - used to convert pieces/slots' world positions (Board2D) into the hint's anchored-position space")]
        [SerializeField] private RectTransform canvasRect;
        [SerializeField] private Camera worldCamera;

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
            _isShowingHint = false;

            // LevelLoader calls tutorialSequencer.BeginForFirstLevel() right
            // before this, which may have just shown the one-time hint for
            // level 1's first piece - don't immediately hide it out from
            // under that sequencer.
            if (tutorialSequencer != null && tutorialSequencer.IsActive)
            {
                Debug.Log("[IdleHandTutorial] Sequencer is showing the first-run hint - leaving it visible");
                return;
            }

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

            if (IsAnyResultScreenActive())
            {
                _idleTimer = 0f;
                if (_isShowingHint) HideHint();
                return; // a win/lose/game-complete banner is up - don't nudge toward pieces the player can't reach
            }

            if (_isShowingHint) return; // already showing - wait for a touch or the piece being placed
            if (tutorialSequencer != null && tutorialSequencer.IsActive) return; // let the first-run tutorial finish first

            _idleTimer += Time.deltaTime;
            Debug.Log(resultScreenUI.IsAnyBannerShowing);
            if (_idleTimer >= idleThreshold && !resultScreenUI.IsAnyBannerShowing)
            {
                ShowHintForNextUnplacedPiece();
            }
        }

        private bool IsAnyResultScreenActive()
        {
            if (resultScreenUI != null && resultScreenUI.IsAnyBannerShowing) return true;
            if (gameCompleteUI != null && gameCompleteUI.IsShowing) return true;
            return false;
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

            var cam = worldCamera != null ? worldCamera : Camera.main;
            Vector2 fromPos = WorldToCanvasUtility.WorldToCanvasPoint(piece.transform.position, canvasRect, cam);
            Vector2 toPos = WorldToCanvasUtility.WorldToCanvasPoint(slot.transform.position, canvasRect, cam);
            handTutorialUI.Show(fromPos, toPos);
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
