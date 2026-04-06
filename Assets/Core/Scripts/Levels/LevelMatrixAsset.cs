using UnityEngine;

namespace TMQFEL.Levels
{
    [CreateAssetMenu(fileName = "LevelMatrix", menuName = "TMQFEL/Levels/Level Matrix")]
    public sealed class LevelMatrixAsset : ScriptableObject
    {
        public const int DefaultWidth = 8;
        public const int DefaultHeight = 5;

        [SerializeField] private int width = DefaultWidth;
        [SerializeField] private int height = DefaultHeight;
        [SerializeField] private LevelCellType[] cells = new LevelCellType[DefaultWidth * DefaultHeight];

        public int Width => width;

        public int Height => height;

        public LevelCellType GetCell(int x, int y)
        {
            return cells[GetIndex(x, y)];
        }

        public void SetCell(int x, int y, LevelCellType cellType)
        {
            cells[GetIndex(x, y)] = cellType;
        }

        public LevelCellType[] CopyCells()
        {
            var copiedCells = new LevelCellType[cells.Length];
            cells.CopyTo(copiedCells, 0);
            return copiedCells;
        }

        public void Resize(int newWidth, int newHeight)
        {
            var resizedCells = new LevelCellType[newWidth * newHeight];
            var copyWidth = Mathf.Min(width, newWidth);
            var copyHeight = Mathf.Min(height, newHeight);

            for (var y = 0; y < copyHeight; y++)
            {
                for (var x = 0; x < copyWidth; x++)
                {
                    resizedCells[(y * newWidth) + x] = cells[GetIndex(x, y)];
                }
            }

            width = newWidth;
            height = newHeight;
            cells = resizedCells;
        }

        private int GetIndex(int x, int y)
        {
            return (y * width) + x;
        }
    }
}
