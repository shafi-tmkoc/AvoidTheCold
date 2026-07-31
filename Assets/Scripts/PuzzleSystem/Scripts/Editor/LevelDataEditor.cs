using UnityEditor;
using UnityEngine;

namespace AvoidTheCold
{
    /// <summary>
    /// Draws draggable Scene-view handles for every piece's slot
    /// position/scale directly on top of the real WindowFrame art, so a
    /// designer can visually align each slot to the window's cutout
    /// silhouette (per the GDD) instead of typing numbers blindly and
    /// retesting in Play Mode. LevelData's position/scale are LOCAL values
    /// (relative to the open scene's LevelLoader.slotAnchor, applied directly
    /// as the spawned slot's own Transform), so this editor converts through
    /// slotAnchor.TransformPoint/InverseTransformPoint to draw and edit the
    /// correct world-space rectangle - open GamePlay_AvoidTheCold and select
    /// a LevelData asset to use it.
    /// </summary>
    [CustomEditor(typeof(LevelData))]
    public class LevelDataEditor : Editor
    {
        private static readonly Color[] HandleColors =
        {
            Color.red, new Color(1f, 0.55f, 0f), Color.yellow, Color.green, Color.cyan, Color.magenta
        };

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var loader = FindObjectOfType<LevelLoader>();
            EditorGUILayout.Space();
            if (loader == null)
            {
                EditorGUILayout.HelpBox("No LevelLoader found in the open scene - open GamePlay_AvoidTheCold to drag-edit slot position/scale directly in the Scene view.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Scene view: drag the center dot to move a slot, drag either corner square to resize it. Both write straight back into this asset's local position/scale.", MessageType.Info);
            }
        }

        private void OnSceneGUI()
        {
            var data = (LevelData)target;
            if (data.pieces == null) return;

            var loader = FindObjectOfType<LevelLoader>();
            if (loader == null) return;

            Transform anchor = loader.SlotAnchor;
            for (int i = 0; i < data.pieces.Length; i++)
            {
                DrawSlotHandle(data, i, anchor);
            }
        }

        /// <summary>The slot's native (unscaled) size: its assigned sprite's own size, or a 1x1 unit placeholder square - matches LevelLoader's runtime logic exactly.</summary>
        private static Vector2 GetNativeSize(LevelData.PieceDefinition piece)
        {
            return piece.sprite != null
                ? new Vector2(piece.sprite.rect.width, piece.sprite.rect.height) / piece.sprite.pixelsPerUnit
                : Vector2.one;
        }

        private static Vector3 LocalToWorld(Transform anchor, Vector3 local)
        {
            return anchor != null ? anchor.TransformPoint(local) : local;
        }

        private static Vector3 WorldToLocal(Transform anchor, Vector3 world)
        {
            return anchor != null ? anchor.InverseTransformPoint(world) : world;
        }

        private void DrawSlotHandle(LevelData data, int index, Transform anchor)
        {
            var piece = data.pieces[index];
            Vector2 nativeSize = GetNativeSize(piece);
            Vector3 localScale = data.GetSlotScale(index);

            Vector3 center = LocalToWorld(anchor, piece.position);
            // World-space half-size accounts for the anchor's own lossy scale,
            // since localScale compounds with it - matches what will actually render.
            Vector3 anchorLossyScale = anchor != null ? anchor.lossyScale : Vector3.one;
            Vector3 halfSize = new Vector3(
                nativeSize.x * localScale.x * anchorLossyScale.x,
                nativeSize.y * localScale.y * anchorLossyScale.y,
                0f) * 0.5f;

            Vector3 min = center - halfSize;
            Vector3 max = center + halfSize;

            Color c = HandleColors[index % HandleColors.Length];

            Handles.color = c;
            Handles.DrawSolidRectangleWithOutline(
                new[]
                {
                    new Vector3(min.x, min.y, 0f),
                    new Vector3(max.x, min.y, 0f),
                    new Vector3(max.x, max.y, 0f),
                    new Vector3(min.x, max.y, 0f)
                },
                new Color(c.r, c.g, c.b, 0.15f),
                c);

            Handles.Label(new Vector3(center.x, max.y + 0.15f, center.z), string.IsNullOrEmpty(piece.shapeId) ? $"piece {index}" : piece.shapeId);

            float handleSize = HandleUtility.GetHandleSize(center) * 0.06f;
            Handles.color = c;

            // Move handle - drag the center to translate the whole slot.
            EditorGUI.BeginChangeCheck();
            Vector3 newCenter = Handles.FreeMoveHandle(center, handleSize, Vector3.zero, Handles.SphereHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                newCenter.z = center.z;
                Vector3 newLocalPos = WorldToLocal(anchor, newCenter);

                Undo.RecordObject(data, "Move Slot");
                var p = data.pieces[index];
                p.position = new Vector3(newLocalPos.x, newLocalPos.y, p.position.z);
                data.pieces[index] = p;
                EditorUtility.SetDirty(data);
            }

            // Resize handles - drag either corner, the opposite corner stays put.
            EditorGUI.BeginChangeCheck();
            Vector3 newMin = Handles.FreeMoveHandle(min, handleSize, Vector3.zero, Handles.CubeHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                newMin.z = min.z;
                ApplyCornerResize(data, index, anchor, nativeSize, anchorLossyScale, newMin, max);
            }

            EditorGUI.BeginChangeCheck();
            Vector3 newMax = Handles.FreeMoveHandle(max, handleSize, Vector3.zero, Handles.CubeHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                newMax.z = max.z;
                ApplyCornerResize(data, index, anchor, nativeSize, anchorLossyScale, min, newMax);
            }
        }

        private void ApplyCornerResize(LevelData data, int index, Transform anchor, Vector2 nativeSize, Vector3 anchorLossyScale, Vector3 newMin, Vector3 newMax)
        {
            Vector3 newCenterWorld = (newMin + newMax) / 2f;
            Vector3 newSizeWorld = newMax - newMin;
            newSizeWorld.x = Mathf.Abs(newSizeWorld.x);
            newSizeWorld.y = Mathf.Abs(newSizeWorld.y);

            float safeNativeX = Mathf.Max(0.0001f, nativeSize.x * Mathf.Max(0.0001f, anchorLossyScale.x));
            float safeNativeY = Mathf.Max(0.0001f, nativeSize.y * Mathf.Max(0.0001f, anchorLossyScale.y));

            Vector3 newLocalPos = WorldToLocal(anchor, newCenterWorld);

            Undo.RecordObject(data, "Resize Slot");
            var p = data.pieces[index];
            p.position = new Vector3(newLocalPos.x, newLocalPos.y, p.position.z);
            p.scale = new Vector3(
                Mathf.Max(0.01f, newSizeWorld.x / safeNativeX),
                Mathf.Max(0.01f, newSizeWorld.y / safeNativeY),
                p.scale.z);
            data.pieces[index] = p;
            EditorUtility.SetDirty(data);
        }
    }
}
