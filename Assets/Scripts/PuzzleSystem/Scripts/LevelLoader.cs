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
        [Tooltip("Prefab (SpriteRenderer + BoxCollider2D + DraggableShape) instantiated for every tray piece - Prefabs/Piece")]
        [SerializeField] private DraggableShape piecePrefab;
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
        [Tooltip("Max pieces shown in the tray at once. The rest stay queued and are revealed one at a time, each replacing a piece as soon as it's placed in its correct slot")]
        [SerializeField] private int trayVisibleCount = 4;

        [Header("Sorting")]
        [SerializeField] private string slotSortingLayer = "Slots";
        [SerializeField] private string pieceSortingLayer = "Pieces";

        [Header("Piece Appearance")]
        [Tooltip("Piece art (Shapes/Level N/X.png) is a flat white silhouette by design - this tints every draggable piece via SpriteRenderer multiply, so no source PNGs need editing to change piece color. Alpha < 1 gives pieces a glass-pane look, matching the window they're filling")]
        [SerializeField] private Color pieceTintColor = new Color(0.66f, 0.85f, 0.95f, 0.75f);

        [Header("Editor Preview")]
        [Tooltip("Used only by the 'Visualize' right-click command below - lets tray/cell values be tuned in the Inspector and checked instantly without entering Play Mode")]
        [SerializeField] private LevelData visualizeLevelData;

        [Header("Systems")]
        [SerializeField] private PuzzleProgressTracker progressTracker;
        [SerializeField] private FreezeMeterValue freezeMeterValue;
        [SerializeField] private CountdownTimer countdownTimer;
        [SerializeField] private MissionResolver missionResolver;
        [SerializeField] private ResultScreenUI resultScreenUI;
        [SerializeField] private GameplayVoiceOverTrigger gameplayVoiceOverTrigger;
        [SerializeField] private LevelTutorialSequencer tutorialSequencer;
        [SerializeField] private IdleHandTutorial idleHandTutorial;
        [SerializeField] private OutsideEnvironmentSeasonSwitcher seasonSwitcher;
        [Tooltip("Curtain/character Animators that switch state on puzzle-complete and back on a fresh attempt (e.g. Curtain, DayaShivering, TappuShivering)")]
        [SerializeField] private AnimatorStateOnPuzzleComplete[] puzzleCompleteAnimators;
        [Tooltip("Objects that disable themselves on puzzle-complete and re-enable on a fresh attempt (e.g. WindlinesStormy, the piece tray)")]
        [SerializeField] private HideOnPuzzleComplete[] hideOnPuzzleComplete;
        [Tooltip("Objects that enable themselves on puzzle-complete and disable again on a fresh attempt (e.g. ConfettiFullscreen)")]
        [SerializeField] private ShowOnPuzzleComplete[] showOnPuzzleComplete;

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

            BuildBoard(data, out var slots, out var pieces);

            if (resultScreenUI != null) resultScreenUI.HideAll();
            if (progressTracker != null) progressTracker.SetSlots(slots);
            if (freezeMeterValue != null) freezeMeterValue.ResetValue();
            if (missionResolver != null) missionResolver.ResetForNewAttempt();
            if (gameplayVoiceOverTrigger != null) gameplayVoiceOverTrigger.ResetForNewAttempt(pieces);
            if (tutorialSequencer != null) tutorialSequencer.BeginForFirstLevel(data.levelNumber, pieces, slots);
            if (idleHandTutorial != null) idleHandTutorial.SetPieces(pieces, slots);
            if (seasonSwitcher != null) seasonSwitcher.ResetForNewAttempt();
            if (puzzleCompleteAnimators != null)
            {
                foreach (var a in puzzleCompleteAnimators)
                {
                    if (a != null) a.ResetForNewAttempt();
                }
            }
            if (hideOnPuzzleComplete != null)
            {
                foreach (var h in hideOnPuzzleComplete)
                {
                    if (h != null) h.ResetForNewAttempt();
                }
            }
            if (showOnPuzzleComplete != null)
            {
                foreach (var s in showOnPuzzleComplete)
                {
                    if (s != null) s.ResetForNewAttempt();
                }
            }

            if (countdownTimer != null)
            {
                countdownTimer.SetDuration(data.timeLimitSeconds);
                countdownTimer.StartTimer();
            }
        }

        /// <summary>
        /// Editor-only preview: rebuilds the tray/slot/piece layout for
        /// visualizeLevelData using whatever tray values are currently set
        /// on this component, without touching any runtime systems (timer,
        /// progress tracker, VO, tutorials). Right-click the component
        /// header (or the gear icon) and choose "Visualize" to check tray
        /// sizing/spacing/scale changes instantly, without entering Play Mode.
        /// </summary>
        [ContextMenu("Visualize")]
        private void Visualize()
        {
            if (visualizeLevelData == null)
            {
                Debug.Log("[LevelLoader] Visualize: assign visualizeLevelData first");
                return;
            }

            Debug.Log($"[LevelLoader] Visualize: previewing '{visualizeLevelData.name}' tray/piece layout in the Editor");
            BuildBoard(visualizeLevelData, out _, out _);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.SceneView.RepaintAll();
            }
#endif
        }

        /// <summary>
        /// Clears any previously spawned board, then builds the tray
        /// container plus every slot and piece for the given level data.
        /// Pure layout/spawn - no runtime system wiring - so both LoadLevel
        /// (real attempts) and Visualize (Editor preview) can share it.
        /// </summary>
        private void BuildBoard(LevelData data, out ShapeDropSlot[] resultSlots, out DraggableShape[] resultPieces)
        {
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

            // Every piece's native (unscaled) world size. Each tray CELL scales
            // its own piece independently to fit (see SpawnPieceIntoCell below),
            // so pieces of very different sizes each look right in their cell
            // regardless of spawn order.
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

            // Only trayVisibleCount pieces ever occupy the tray at once, in
            // fixed evenly-spaced cells. Remaining pieces (by level-data
            // order) stay queued and each is revealed into the cell its
            // predecessor just vacated once that piece is placed correctly.
            int visibleCount = Mathf.Clamp(trayVisibleCount, 1, data.pieces.Length);
            float availableWidth = containerWidth - trayInnerPadding * 2f - traySpacing * Mathf.Max(0, visibleCount - 1);
            float availableHeight = containerHeight - trayInnerPadding * 2f;
            float cellWidth = availableWidth / visibleCount;
            float cellHeight = availableHeight;
            float totalCellsWidth = cellWidth * visibleCount + traySpacing * Mathf.Max(0, visibleCount - 1);
            float cellsStartX = containerCenter.x - totalCellsWidth / 2f;
            float pieceY = containerCenter.y;

            var cellCenterX = new float[visibleCount];
            for (int c = 0; c < visibleCount; c++)
                cellCenterX[c] = cellsStartX + cellWidth / 2f + c * (cellWidth + traySpacing);

            Debug.Log($"[LevelLoader] Tray cells: visibleCount={visibleCount}/{data.pieces.Length} cellSize={cellWidth}x{cellHeight}");

            int nextQueuedIndex = visibleCount;

            // Spawns piece `pieceIndex` into tray cell `cellIndex`, scaled to
            // fit that cell on its own. Wires up a one-shot reveal: once this
            // piece is placed correctly, the next queued piece (if any) spawns
            // into the same cell.
            void SpawnPieceIntoCell(int pieceIndex, int cellIndex)
            {
                var def = data.pieces[pieceIndex];
                Vector2 native = nativeSizes[pieceIndex];
                float scaleForWidth = native.x > 0f ? cellWidth / native.x : 1f;
                float scaleForHeight = native.y > 0f ? cellHeight / native.y : 1f;
                float scale = Mathf.Clamp(Mathf.Min(scaleForWidth, scaleForHeight), 0.05f, maxPieceScale);
                float pieceWidth = native.x * scale;
                float pieceHeight = native.y * scale;
                Vector3 pieceWorldPos = new Vector3(cellCenterX[cellIndex], pieceY, 0f);

                var pieceGO = CreateSpriteObject($"Piece_{def.shapeId}", def.pieceSprite, def.placeholderColor, new Vector2(pieceWidth, pieceHeight), pieceWorldPos, pieceSortingLayer, 2, trayParent);
                var draggable = pieceGO.GetComponent<DraggableShape>();
                if (draggable == null) draggable = pieceGO.AddComponent<DraggableShape>();
                draggable.Initialize(def.shapeId, pieceWorldPos, def.outlineSprite);
                _spawned.Add(pieceGO);
                pieces[pieceIndex] = draggable;

                draggable.OnPlacedSuccessfully += () =>
                {
                    Debug.Log($"[LevelLoader] {def.shapeId} placed - freeing tray cell {cellIndex}");
                    if (nextQueuedIndex < data.pieces.Length)
                    {
                        int revealIndex = nextQueuedIndex;
                        nextQueuedIndex++;
                        SpawnPieceIntoCell(revealIndex, cellIndex);
                    }
                };

                Debug.Log($"[LevelLoader] {def.shapeId}: cell={cellIndex} piecePos={pieceWorldPos} pieceSize={pieceWidth}x{pieceHeight}");
            }

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

                Debug.Log($"[LevelLoader] {def.shapeId}: slotLocalPos={def.position} slotLocalScale={data.GetSlotScale(i)} slotWorldPos={slotGO.transform.position}");
            }

            // Only the first batch actually spawns into the tray now - the
            // rest wait in the queue and get revealed one-by-one above.
            for (int c = 0; c < visibleCount; c++)
                SpawnPieceIntoCell(c, c);

            resultSlots = slots;
            resultPieces = pieces;
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
        /// Builds a piece GameObject (SpriteRenderer + BoxCollider2D +
        /// DraggableShape) under the given parent, sized in world units, by
        /// instantiating piecePrefab (Prefabs/Piece) - never builds it from
        /// scratch in code when the prefab is assigned. Falls back to
        /// building a plain GameObject only if piecePrefab is left
        /// unassigned, so a missing reference doesn't silently break
        /// spawning. Falls back to a solid-color placeholder square when no
        /// sprite is assigned yet.
        /// </summary>
        private GameObject CreateSpriteObject(string name, Sprite sprite, Color placeholderColor, Vector2 size, Vector3 position, string sortingLayer, int order, Transform parent)
        {
            GameObject go;
            if (piecePrefab != null)
            {
                go = Instantiate(piecePrefab.gameObject, position, Quaternion.identity, parent);
                go.name = name;
            }
            else
            {
                Debug.Log("[LevelLoader] piecePrefab not assigned - building piece GameObject from scratch instead");
                go = new GameObject(name);
                go.transform.SetParent(parent, false);
                go.transform.position = position;
            }

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = order;

            Vector2 nativeSize;
            if (sprite != null)
            {
                sr.sprite = sprite;
                // Piece art (Shapes/Level N/X.png) is a flat white silhouette by
                // design, so it takes pieceTintColor directly via SpriteRenderer's
                // multiply-tint - no need to hand-edit every source PNG.
                sr.color = pieceTintColor;
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

            var collider = go.GetComponent<BoxCollider2D>();
            if (collider == null) collider = go.AddComponent<BoxCollider2D>();
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
                if (go == null) continue;
                // Destroy() only works in Play Mode - the Visualize command
                // (see above) runs in Edit Mode too, where DestroyImmediate is required.
                if (Application.isPlaying) Destroy(go);
                else DestroyImmediate(go);
            }
            _spawned.Clear();
        }
    }
}
