using UnityEngine;

namespace TMQFEL.Levels
{
    public sealed class LevelSystem
    {
        private readonly LevelCreator _levelCreator;

        public LevelSystem(LevelCreator levelCreator)
        {
            _levelCreator = levelCreator;
        }

        public LevelMapConfig LevelMapConfig => _levelCreator.LevelMapConfig;

        public float CellSize => _levelCreator.CellSize;

        public Vector3 GetCellWorldPosition(int x, int y)
        {
            return _levelCreator.GetCellWorldPosition(x, y);
        }

        public Vector3 GetSpawnWorldPosition()
        {
            var levelMapConfig = LevelMapConfig;

            for (var y = 0; y < levelMapConfig.Height; y++)
            {
                for (var x = 0; x < levelMapConfig.Width; x++)
                {
                    if (levelMapConfig.GetCell(x, y) != LevelCellType.Spawn)
                    {
                        continue;
                    }

                    return GetCellWorldPosition(x, y);
                }
            }

            return GetWorldPosition();
        }

        public Vector3 GetWorldPosition()
        {
            return _levelCreator.transform.position;
        }
    }
}
