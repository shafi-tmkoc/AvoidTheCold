using System;
using UnityEngine;

namespace AvoidTheCold
{
    /// <summary>
    /// Defines one level's puzzle layout and time limit: how many pieces,
    /// where each piece starts (tray) and where it belongs (slot), and how
    /// long the player has. Create new levels via
    /// Assets > Create > AvoidTheCold > Level Data.
    /// </summary>
    [CreateAssetMenu(fileName = "Level_", menuName = "AvoidTheCold/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Serializable]
        public struct PieceDefinition
        {
            [Tooltip("Must match between a piece and the slot it belongs in")]
            public string shapeId;
            public Vector2 trayPosition;

            [Tooltip("Slot's LOCAL position relative to the LevelLoader's slotAnchor (normally OutsideEnvironment) - applied directly as the spawned slot's Transform.localPosition, exactly like typing into the Transform component yourself")]
            public Vector3 position;
            public Color placeholderColor;

            [Tooltip("Slot's LOCAL scale - applied directly as the spawned slot's Transform.localScale, exactly like typing into the Transform component yourself. Leave X/Y as 0 to use the level's Default Scale instead")]
            public Vector3 scale;

            [Tooltip("Artwork shown on the target slot. Leave empty to fall back to the placeholder color square")]
            public Sprite sprite;

            [Tooltip("Artwork shown on the draggable piece. Leave empty to fall back to the placeholder color square")]
            public Sprite pieceSprite;

            public Sprite outlineSprite;
        }

        [Min(1)] public int levelNumber = 1;
        [Min(1f)] public float timeLimitSeconds = 30f;

        [Tooltip("Fallback local scale used for any piece whose own scale is left at (0,0) - keeps older level assets working without edits")]
        [SerializeField] private Vector3 defaultScale = new Vector3(2.2f, 2.2f, 1f);

        public PieceDefinition[] pieces;

        /// <summary>Effective slot local scale for a piece: its own scale if set, otherwise the level's defaultScale.</summary>
        public Vector3 GetSlotScale(int index)
        {
            if (pieces == null || index < 0 || index >= pieces.Length) return defaultScale;

            Vector3 scale = pieces[index].scale;
            return (scale.x > 0f && scale.y > 0f) ? scale : defaultScale;
        }
    }
}
