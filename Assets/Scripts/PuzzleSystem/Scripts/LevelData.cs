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
            public Vector2 slotPosition;
            public Color placeholderColor;

            [Tooltip("Width/height of this piece's target slot. Leave as (0,0) to use the level's Default Slot Size instead")]
            public Vector2 slotSize;

            [Tooltip("Artwork shown on the target slot. Leave empty to fall back to the placeholder color square")]
            public Sprite sprite;

            [Tooltip("Artwork shown on the draggable piece. Leave empty to fall back to the placeholder color square")]
            public Sprite pieceSprite;
        }

        [Min(1)] public int levelNumber = 1;
        [Min(1f)] public float timeLimitSeconds = 30f;

        [Tooltip("Fallback slot size used for any piece whose own slotSize is left at (0,0) - keeps older level assets working without edits")]
        [SerializeField] private Vector2 defaultSlotSize = new Vector2(220f, 220f);

        public PieceDefinition[] pieces;

        /// <summary>Effective slot size for a piece: its own slotSize if set, otherwise the level's defaultSlotSize.</summary>
        public Vector2 GetSlotSize(int index)
        {
            if (pieces == null || index < 0 || index >= pieces.Length) return defaultSlotSize;

            Vector2 size = pieces[index].slotSize;
            return (size.x > 0f && size.y > 0f) ? size : defaultSlotSize;
        }
    }
}
