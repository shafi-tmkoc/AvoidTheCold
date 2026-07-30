using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AvoidTheCold
{
    /// <summary>
    /// Builds the scene for a given LevelData: clears any previously spawned
    /// pieces/slots, instantiates fresh ones at the data's positions, and
    /// resets the timer, progress tracker, freeze meter, mission resolver
    /// and result banners for a clean attempt.
    /// </summary>
    public class LevelLoader : MonoBehaviour
    {
        [SerializeField] private GameObject piecePrefab;
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private RectTransform spawnParent, pieceSpawnParent;

        [SerializeField] private PuzzleProgressTracker progressTracker;
        [SerializeField] private FreezeMeterValue freezeMeterValue;
        [SerializeField] private CountdownTimer countdownTimer;
        [SerializeField] private MissionResolver missionResolver;
        [SerializeField] private ResultScreenUI resultScreenUI;
        [SerializeField] private GameplayVoiceOverTrigger gameplayVoiceOverTrigger;
        [SerializeField] private LevelTutorialSequencer tutorialSequencer;
        [SerializeField] private IdleHandTutorial idleHandTutorial;

        private readonly List<GameObject> _spawned = new List<GameObject>();

        public void LoadLevel(LevelData data)
        {
            if (data == null)
            {
                Debug.Log("[LevelLoader] LoadLevel called with null data - ignoring");
                return;
            }

            Debug.Log($"[LevelLoader] Loading level {data.levelNumber} ({data.pieces.Length} pieces, {data.timeLimitSeconds}s)");

            ClearSpawned();

            var slots = new ShapeDropSlot[data.pieces.Length];
            var pieces = new DraggableShape[data.pieces.Length];

            for (int i = 0; i < data.pieces.Length; i++)
            {
                var def = data.pieces[i];
                Vector2 slotSize = data.GetSlotSize(i);

                var slotGO = Instantiate(slotPrefab, spawnParent);
                slotGO.name = $"Slot_{def.shapeId}";
                var slot = slotGO.GetComponent<ShapeDropSlot>();
                var slotRect = (RectTransform)slotGO.transform;
                slotRect.sizeDelta = slotSize;
                slot.Initialize(def.shapeId, ClampToVisibleArea(def.slotPosition, slotSize));
                ApplyArt(slotGO.GetComponent<Image>(), def.sprite, def.placeholderColor);
                _spawned.Add(slotGO);
                slots[i] = slot;

                var pieceGO = Instantiate(piecePrefab, pieceSpawnParent);
                pieceGO.name = $"Piece_{def.shapeId}";
                var draggable = pieceGO.GetComponent<DraggableShape>();
                var pieceRect = (RectTransform)pieceGO.transform;
                Vector2 nativeSize = def.pieceSprite != null ? new Vector2(def.pieceSprite.rect.width, def.pieceSprite.rect.height) : pieceRect.sizeDelta;
                Vector2 fittedPieceSize = AspectFitUtility.FitWithinBox(nativeSize, slotSize);
                pieceRect.sizeDelta = fittedPieceSize;
                draggable.Initialize(def.shapeId, ClampToVisibleArea(def.trayPosition, fittedPieceSize));
                ApplyArt(pieceGO.GetComponent<Image>(), def.pieceSprite, def.placeholderColor);
                _spawned.Add(pieceGO);
                pieces[i] = draggable;

                Debug.Log($"[LevelLoader] {def.shapeId}: slotSize={slotSize} pieceSize={fittedPieceSize}");
            }

            if (resultScreenUI != null) resultScreenUI.HideAll();
            if (progressTracker != null) progressTracker.SetSlots(slots);
            if (freezeMeterValue != null) freezeMeterValue.ResetValue();
            if (missionResolver != null) missionResolver.ResetForNewAttempt();
            if (gameplayVoiceOverTrigger != null) gameplayVoiceOverTrigger.ResetForNewAttempt(pieces);
            if (tutorialSequencer != null) tutorialSequencer.BeginForFirstLevel(data.levelNumber, pieces, slots);
            if (idleHandTutorial != null) idleHandTutorial.SetPieces(pieces, slots);

            if (countdownTimer != null)
            {
                countdownTimer.SetDuration(data.timeLimitSeconds);
                countdownTimer.StartTimer();
            }
        }

        /// <summary>
        /// Shows the given sprite on this Image if one is assigned (reset to
        /// white so the art isn't tinted), otherwise falls back to the
        /// placeholder color square.
        /// </summary>
        private static void ApplyArt(Image image, Sprite sprite, Color placeholderColor)
        {
            if (image == null) return;

            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
            }
            else
            {
                image.color = placeholderColor;
            }
        }

        /// <summary>
        /// Pulls a designed position back inside the actual visible canvas
        /// area so a level authored against one aspect ratio (e.g. 16:9)
        /// never places a piece off-screen on a wider/narrower device.
        /// </summary>
        private Vector2 ClampToVisibleArea(Vector2 designPosition, Vector2 objectSize)
        {
            if (spawnParent == null) return designPosition;

            Rect area = spawnParent.rect;

            // Canvas hasn't laid out yet (e.g. LoadLevel fired on frame 1 before the
            // first layout pass) - area would be zero-size, which would otherwise
            // clamp every position down to (0,0). Safer to skip clamping this call
            // than to silently collapse every piece onto the same spot.
            if (area.width <= 0f || area.height <= 0f)
            {
                Debug.Log("[LevelLoader] spawnParent area not ready yet - skipping clamp for this position");
                return designPosition;
            }

            float halfW = Mathf.Max(0f, area.width / 2f - objectSize.x / 2f);
            float halfH = Mathf.Max(0f, area.height / 2f - objectSize.y / 2f);

            return new Vector2(
                Mathf.Clamp(designPosition.x, -halfW, halfW),
                Mathf.Clamp(designPosition.y, -halfH, halfH));
        }

        private void ClearSpawned()
        {
            foreach (var go in _spawned)
            {
                if (go != null) Destroy(go);
            }
            _spawned.Clear();
        }
    }
}
