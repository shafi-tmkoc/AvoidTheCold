using System.Collections.Generic;
using UnityEngine;

namespace AvoidTheCold
{
    /// <summary>
    /// Builds the world-space board for a given LevelData under Board2D
    /// (never Canvas - Canvas is UI-overlay only now): clears any previously
    /// spawned pieces/slots, constructs fresh SpriteRenderer+Collider2D
    /// pieces/slots at the data's positions, and resets the timer, progress
    /// tracker, freeze meter, mission resolver and result banners for a
    /// clean attempt.
    /// </summary>
    public class LevelLoader : MonoBehaviour
    {
        [Header("Board2D wiring")]
        [Tooltip("Parent for all spawned pieces/slots - must be under Board2D, never Canvas")]
        [SerializeField] private Transform boardRoot;

        [Tooltip("Slot positions are placed relative to this transform's world position (normally OutsideEnvironment)")]
        [SerializeField] private Transform slotAnchor;

        [Tooltip("Converts the old UI pixel-space position/size numbers already authored in LevelData into world units (100px = 1 unit)")]
        [SerializeField] private float pixelsToWorldScale = 0.01f;

        [Header("Bottom piece tray")]
        [SerializeField] private float trayY = -8.5f;
        [SerializeField] private float traySpacing = 2.2f;

        [Header("Sorting")]
        [SerializeField] private string slotSortingLayer = "Slots";
        [SerializeField] private string pieceSortingLayer = "Pieces";

        [Header("Systems")]
        [SerializeField] private PuzzleProgressTracker progressTracker;
        [SerializeField] private FreezeMeterValue freezeMeterValue;
        [SerializeField] private CountdownTimer countdownTimer;
        [SerializeField] private MissionResolver missionResolver;
        [SerializeField] private ResultScreenUI resultScreenUI;
        [SerializeField] private GameplayVoiceOverTrigger gameplayVoiceOverTrigger;
        [SerializeField] private LevelTutorialSequencer tutorialSequencer;
        [SerializeField] private IdleHandTutorial idleHandTutorial;

        private readonly List<GameObject> _spawned = new List<GameObject>();
        private static Sprite _placeholderSprite;

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

            Vector3 anchorPos = slotAnchor != null ? slotAnchor.position : Vector3.zero;
            float trayStartX = -(data.pieces.Length - 1) * traySpacing / 2f;

            for (int i = 0; i < data.pieces.Length; i++)
            {
                var def = data.pieces[i];
                Vector2 slotSizeWorld = data.GetSlotSize(i) * pixelsToWorldScale;

                Vector3 slotWorldPos = anchorPos + new Vector3(def.slotPosition.x, def.slotPosition.y, 0f) * pixelsToWorldScale;
                var slotGO = CreateSpriteObject($"Slot_{def.shapeId}", def.sprite, def.placeholderColor, slotSizeWorld, slotWorldPos, slotSortingLayer, -1, slotAnchor != null ? slotAnchor : boardRoot);
                slotGO.GetComponent<Collider2D>().isTrigger = true; // slots aren't solid obstacles, just drop-detection zones
                var slot = slotGO.AddComponent<ShapeDropSlot>();
                slot.Initialize(def.shapeId, slotWorldPos);
                _spawned.Add(slotGO);
                slots[i] = slot;

                Vector3 pieceWorldPos = new Vector3(trayStartX + i * traySpacing, trayY, 0f);
                Vector2 pieceNativeSize = def.pieceSprite != null
                    ? new Vector2(def.pieceSprite.rect.width, def.pieceSprite.rect.height) / def.pieceSprite.pixelsPerUnit
                    : slotSizeWorld;
                Vector2 pieceSizeWorld = AspectFitUtility.FitWithinBox(pieceNativeSize, slotSizeWorld);
                var pieceGO = CreateSpriteObject($"Piece_{def.shapeId}", def.pieceSprite, def.placeholderColor, pieceSizeWorld, pieceWorldPos, pieceSortingLayer, 0, boardRoot);
                var draggable = pieceGO.AddComponent<DraggableShape>();
                draggable.Initialize(def.shapeId, pieceWorldPos);
                _spawned.Add(pieceGO);
                pieces[i] = draggable;

                Debug.Log($"[LevelLoader] {def.shapeId}: slotPos={slotWorldPos} slotSize={slotSizeWorld} piecePos={pieceWorldPos}");
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
        /// Builds a plain SpriteRenderer + BoxCollider2D GameObject under the
        /// given parent, sized in world units. Falls back to a solid-color
        /// placeholder square when no sprite is assigned yet.
        /// </summary>
        private GameObject CreateSpriteObject(string name, Sprite sprite, Color placeholderColor, Vector2 size, Vector3 position, string sortingLayer, int order, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = order;

            Vector2 nativeSize;
            if (sprite != null)
            {
                sr.sprite = sprite;
                sr.color = Color.white;
                nativeSize = new Vector2(sprite.rect.width, sprite.rect.height) / sprite.pixelsPerUnit;
            }
            else
            {
                sr.sprite = GetPlaceholderSprite();
                sr.color = placeholderColor;
                nativeSize = Vector2.one;
            }

            float safeW = Mathf.Max(0.0001f, nativeSize.x);
            float safeH = Mathf.Max(0.0001f, nativeSize.y);

            // Compensate for the parent's own scale so the sprite renders at the
            // requested WORLD size regardless of how the parent (e.g. a hand-tuned
            // OutsideEnvironment) is scaled.
            Vector3 parentScale = parent != null ? parent.lossyScale : Vector3.one;
            float safeParentX = Mathf.Max(0.0001f, parentScale.x);
            float safeParentY = Mathf.Max(0.0001f, parentScale.y);

            go.transform.localScale = new Vector3(
                (size.x / safeW) / safeParentX,
                (size.y / safeH) / safeParentY,
                1f);

            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = nativeSize;

            return go;
        }

        private static Sprite GetPlaceholderSprite()
        {
            if (_placeholderSprite != null) return _placeholderSprite;
            var tex = Texture2D.whiteTexture;
            _placeholderSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
            return _placeholderSprite;
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
