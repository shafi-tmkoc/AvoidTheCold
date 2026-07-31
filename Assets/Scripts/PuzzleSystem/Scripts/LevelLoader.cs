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

        /// <summary>Read-only access for Editor tooling (e.g. LevelDataEditor's Scene-view slot handles) that needs to convert LevelData's anchor-relative positions into world positions without duplicating this wiring.</summary>
        public Transform SlotAnchor => slotAnchor;

        [Header("Bottom-left piece tray")]
        [Tooltip("Editor-authored Sprite2D backdrop (e.g. Board2D/PieceTray) - repositioned/rescaled at load time to fit the current screen, but its sprite/color stay exactly as set up in the Editor")]
        [SerializeField] private Transform pieceTray;
        [Tooltip("Fraction of the camera's actual visible width the tray container occupies")]
        [SerializeField] private float trayWidthFraction = 0.7f;
        [Tooltip("Tray container height, in world units")]
        [SerializeField] private float trayHeight = 3.4f;
        [Tooltip("Margin from the screen's left/bottom edges to the tray container")]
        [SerializeField] private float trayScreenMargin = 0.3f;
        [Tooltip("Padding inside the tray container around the piece row")]
        [SerializeField] private float trayInnerPadding = 0.3f;
        [Tooltip("Gap between adjacent pieces inside the tray")]
        [SerializeField] private float traySpacing = 0.25f;
        [Tooltip("Upper clamp on the auto-computed piece scale so a single piece can't render huge")]
        [SerializeField] private float maxPieceScale = 1.6f;

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

            // Bottom-left tray container, sized from the camera's ACTUAL visible
            // world bounds (not a guessed constant) so it - and every piece
            // inside it - is guaranteed to stay on screen on any device/aspect.
            Rect screenRect = GetCameraVisibleWorldRect();
            float containerWidth = screenRect.width * trayWidthFraction;
            float containerHeight = trayHeight;
            Vector3 containerBottomLeft = new Vector3(screenRect.xMin + trayScreenMargin, screenRect.yMin + trayScreenMargin, 0f);
            Vector3 containerCenter = containerBottomLeft + new Vector3(containerWidth / 2f, containerHeight / 2f, 0f);

            Transform trayParent = boardRoot;
            if (pieceTray != null)
            {
                // Make sure it's active - if left disabled in the Editor, every
                // piece parented under it would never get Awake() called (Unity
                // defers Awake until a GameObject is active in the hierarchy),
                // causing null refs on the piece's own cached components.
                if (!pieceTray.gameObject.activeSelf) pieceTray.gameObject.SetActive(true);

                // Editor-authored Sprite2D (its sprite/color are left exactly as
                // set up in the Editor) - just reposition/rescale it here so the
                // requested WORLD-space container size is exact on this device.
                var traySr = pieceTray.GetComponent<SpriteRenderer>();
                Vector2 trayNativeSize = traySr != null && traySr.sprite != null
                    ? new Vector2(traySr.sprite.rect.width, traySr.sprite.rect.height) / traySr.sprite.pixelsPerUnit
                    : Vector2.one;
                Vector3 trayParentScale = pieceTray.parent != null ? pieceTray.parent.lossyScale : Vector3.one;

                pieceTray.position = containerCenter;
                pieceTray.localScale = new Vector3(
                    (containerWidth / Mathf.Max(0.0001f, trayNativeSize.x)) / Mathf.Max(0.0001f, trayParentScale.x),
                    (containerHeight / Mathf.Max(0.0001f, trayNativeSize.y)) / Mathf.Max(0.0001f, trayParentScale.y),
                    1f);

                trayParent = pieceTray;
            }
            else
            {
                Debug.Log("[LevelLoader] pieceTray not assigned - pieces will parent under boardRoot and the tray backdrop won't render");
            }

            Debug.Log($"[LevelLoader] Tray container: screenRect={screenRect} size={containerWidth}x{containerHeight} center={containerCenter}");

            // Every piece's native (unscaled) world size, used to compute one
            // uniform scale factor that makes the whole row - plus padding -
            // fit inside the tray container, both horizontally and vertically.
            var nativeSizes = new Vector2[data.pieces.Length];
            for (int i = 0; i < data.pieces.Length; i++)
            {
                var def = data.pieces[i];
                // A piece with no pieceSprite renders on a 1x1 unit placeholder
                // square, so its effective native size IS the configured scale.
                Vector2 fallbackSize = data.GetSlotScale(i);
                nativeSizes[i] = def.pieceSprite != null
                    ? new Vector2(def.pieceSprite.rect.width, def.pieceSprite.rect.height) / def.pieceSprite.pixelsPerUnit
                    : fallbackSize;
            }

            float sumNativeWidths = 0f;
            float maxNativeHeight = 0f;
            foreach (var s in nativeSizes)
            {
                sumNativeWidths += s.x;
                if (s.y > maxNativeHeight) maxNativeHeight = s.y;
            }

            float availableWidth = containerWidth - trayInnerPadding * 2f - traySpacing * Mathf.Max(0, data.pieces.Length - 1);
            float availableHeight = containerHeight - trayInnerPadding * 2f;

            float scaleForWidth = sumNativeWidths > 0f ? availableWidth / sumNativeWidths : 1f;
            float scaleForHeight = maxNativeHeight > 0f ? availableHeight / maxNativeHeight : 1f;
            float pieceScale = Mathf.Clamp(Mathf.Min(scaleForWidth, scaleForHeight), 0.05f, maxPieceScale);

            Debug.Log($"[LevelLoader] Piece scale: scaleForWidth={scaleForWidth} scaleForHeight={scaleForHeight} -> using {pieceScale}");

            float totalRowWidth = sumNativeWidths * pieceScale + traySpacing * Mathf.Max(0, data.pieces.Length - 1);
            float cursorX = containerCenter.x - totalRowWidth / 2f;
            float pieceY = containerCenter.y;

            for (int i = 0; i < data.pieces.Length; i++)
            {
                var def = data.pieces[i];

                // Transform-style: position/scale are applied directly as the
                // spawned slot's own localPosition/localScale, relative to
                // slotAnchor - exactly like typing numbers into the Transform
                // component yourself, no world-space conversion involved.
                Transform slotParent = slotAnchor != null ? slotAnchor : boardRoot;
                var slotGO = CreateSlotObject($"Slot_{def.shapeId}", def.sprite, def.placeholderColor, def.position, data.GetSlotScale(i), slotSortingLayer, -1, slotParent);
                slotGO.GetComponent<Collider2D>().isTrigger = true; // slots aren't solid obstacles, just drop-detection zones
                var slot = slotGO.AddComponent<ShapeDropSlot>();
                slot.Initialize(def.shapeId, slotGO.transform.position);
                _spawned.Add(slotGO);
                slots[i] = slot;

                float pieceWidth = nativeSizes[i].x * pieceScale;
                float pieceHeight = nativeSizes[i].y * pieceScale;
                float pieceCenterX = cursorX + pieceWidth / 2f;
                Vector3 pieceWorldPos = new Vector3(pieceCenterX, pieceY, 0f);
                cursorX += pieceWidth + traySpacing;

                var pieceGO = CreateSpriteObject($"Piece_{def.shapeId}", def.pieceSprite, def.placeholderColor, new Vector2(pieceWidth, pieceHeight), pieceWorldPos, pieceSortingLayer, 0, trayParent);
                var draggable = pieceGO.AddComponent<DraggableShape>();
                draggable.Initialize(def.shapeId, pieceWorldPos);
                _spawned.Add(pieceGO);
                pieces[i] = draggable;

                Debug.Log($"[LevelLoader] {def.shapeId}: slotLocalPos={def.position} slotLocalScale={data.GetSlotScale(i)} slotWorldPos={slotGO.transform.position} piecePos={pieceWorldPos} pieceSize={pieceWidth}x{pieceHeight}");
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
        /// Returns the main camera's actual visible world-space rectangle
        /// (derived from its live orthographicSize/aspect/position, the same
        /// numbers CameraFit2D maintains), so tray sizing/placement adapts to
        /// whatever device/resolution is currently active instead of assuming
        /// fixed world coordinates.
        /// </summary>
        private Rect GetCameraVisibleWorldRect()
        {
            var cam = Camera.main;
            if (cam == null || !cam.orthographic)
            {
                Debug.Log("[LevelLoader] No orthographic main camera found for tray sizing - using a fallback screen rect");
                return new Rect(-8f, -8f, 16f, 16f);
            }

            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            Vector3 center = cam.transform.position;
            return new Rect(center.x - halfWidth, center.y - halfHeight, halfWidth * 2f, halfHeight * 2f);
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
            // OutsideEnvironment, or the tray container's own stretched scale) is
            // scaled.
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

        /// <summary>
        /// Builds a slot's SpriteRenderer + trigger BoxCollider2D GameObject,
        /// applying localPosition/localScale directly and verbatim - no
        /// world-size math, no parent-scale compensation. This is the literal
        /// Transform-style behavior LevelData's position/scale fields are
        /// meant to give: what you author is exactly what ends up on the
        /// spawned object's own Transform component.
        /// </summary>
        private GameObject CreateSlotObject(string name, Sprite sprite, Color placeholderColor, Vector3 localPosition, Vector3 localScale, string sortingLayer, int order, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;

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
