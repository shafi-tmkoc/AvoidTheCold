using System;
using UnityEngine;

namespace Puzzle
{
    /// <summary>
    /// Defines one level's puzzle layout and time limit: how many pieces,
    /// where each piece starts (tray) and where it belongs (slot), and how
    /// long the player has. Create new levels via
    /// Assets > Create > Puzzle > Level Data.
    /// </summary>
    [CreateAssetMenu(fileName = "Level_", menuName = "Puzzle/Level Data")]
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
        }

        [Min(1)] public int levelNumber = 1;
        [Min(1f)] public float timeLimitSeconds = 30f;
        public PieceDefinition[] pieces;
    }
}
