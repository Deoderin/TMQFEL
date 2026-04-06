using UnityEngine;

namespace TMQFEL.Levels
{
    [CreateAssetMenu(fileName = "LevelMapConfig", menuName = "TMQFEL/Levels/Level Map Config")]
    public sealed class LevelMapConfig : ScriptableObject
    {
        [SerializeField] private int width;
        [SerializeField] private int height;
        [SerializeField] private LevelCellType[] cells;
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private Sprite frontSprite;

        public int Width => width;

        public int Height => height;

        public Sprite BackgroundSprite => backgroundSprite;

        public Sprite FrontSprite => frontSprite;

        public LevelCellType GetCell(int x, int y)
        {
            return cells[(y * width) + x];
        }

        public void SetData(int mapWidth, int mapHeight, LevelCellType[] mapCells)
        {
            width = mapWidth;
            height = mapHeight;
            cells = mapCells;
        }
    }
}
