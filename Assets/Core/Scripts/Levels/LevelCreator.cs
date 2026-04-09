using TMQFEL.Core;
using UnityEngine;

namespace TMQFEL.Levels
{
    [DisallowMultipleComponent]
    public sealed class LevelCreator : MonoBehaviour
    {
        private const string GeneratedRootName = "GeneratedLevel";
        private const string BackgroundLayerName = "BackgroundLayer";
        private const string CollidersLayerName = "CollidersLayer";
        private const string MarkersLayerName = "MarkersLayer";
        private const string FrontLayerName = "FrontLayer";

        [SerializeField] private LevelMapConfig levelMapConfig;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private float leftPadding = 0.35f;
        [SerializeField] private float rightPadding = 0.35f;
        [SerializeField] private float topPadding = 0.35f;
        [SerializeField] private float bottomPadding = 0.35f;
        [SerializeField] private int generatedPixelsPerCell = 32;
        [SerializeField] private float markerScale = 0.36f;
        [SerializeField] private Color backgroundColor = new(0.11f, 0.15f, 0.22f, 1f);
        [SerializeField] private Color frontColor = new(0.72f, 0.76f, 0.82f, 1f);
        [SerializeField] private Color spawnColor = new(0.27f, 0.69f, 0.98f, 1f);
        [SerializeField] private Color monsterColor = new(0.93f, 0.32f, 0.28f, 1f);
        [SerializeField] private Color treasureColor = new(0.99f, 0.78f, 0.17f, 1f);
        [SerializeField] private int backgroundSortingOrder;
        [SerializeField] private int frontSortingOrder = 10;
        [SerializeField] private int markerSortingOrder = 20;

        private Sprite _generatedFrontSprite;
        private Texture2D _generatedFrontTexture;

        public LevelMapConfig LevelMapConfig => levelMapConfig;

        public float CellSize => cellSize;

        private void Awake()
        {
            SystemsService.Instance.Register(new LevelSystem(this));
        }

        private void Start()
        {
            BuildLevel();
        }

        [ContextMenu("Build Level")]
        public void BuildLevel()
        {
            ClearGenerated();
            DestroyGeneratedAssets();
            FitCameraToLevel();

            var generatedRoot = CreateRoot(GeneratedRootName, transform);
            var backgroundRoot = CreateRoot(BackgroundLayerName, generatedRoot);
            var collidersRoot = CreateRoot(CollidersLayerName, generatedRoot);
            var markersRoot = CreateRoot(MarkersLayerName, generatedRoot);
            var frontRoot = CreateRoot(FrontLayerName, generatedRoot);

            CreateBackground(backgroundRoot);
            CreateFront(frontRoot);
            CreateWallColliders(collidersRoot);

            for (var y = 0; y < levelMapConfig.Height; y++)
            {
                for (var x = 0; x < levelMapConfig.Width; x++)
                {
                    var cellType = levelMapConfig.GetCell(x, y);
                    var position = GetCellPosition(x, y);

                    if (IsMarkerCell(cellType))
                    {
                        CreateMarker(markersRoot, x, y, position, cellType);
                    }

                    if (IsSlopeCell(cellType))
                    {
                        CreateCollider(collidersRoot, x, y, position, cellType);
                    }
                }
            }
        }

        [ContextMenu("Clear Generated")]
        public void ClearGenerated()
        {
            var generatedRoot = transform.Find(GeneratedRootName);
            if (generatedRoot == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(generatedRoot.gameObject);
                DestroyGeneratedAssets();
                return;
            }

            DestroyImmediate(generatedRoot.gameObject);
            DestroyGeneratedAssets();
        }

        private void CreateBackground(Transform parent)
        {
            var sprite = levelMapConfig.BackgroundSprite == null ? LevelSpriteCache.Square : levelMapConfig.BackgroundSprite;
            var color = levelMapConfig.BackgroundSprite == null ? backgroundColor : Color.white;
            var background = CreateSpriteObject(parent, "Background", Vector3.zero, sprite, color, backgroundSortingOrder);
            ScaleToMap(background.transform, sprite);
        }

        private void CreateFront(Transform parent)
        {
            var sprite = CreateMaskedFrontSprite();
            var front = CreateSpriteObject(parent, "Front", Vector3.zero, sprite, Color.white, frontSortingOrder);
            ScaleToMap(front.transform, sprite);
        }

        private void CreateMarker(Transform parent, int x, int y, Vector3 position, LevelCellType cellType)
        {
            var marker = CreateSpriteObject(parent, $"{cellType}_{x}_{y}", position, LevelSpriteCache.Square, GetMarkerColor(cellType), markerSortingOrder);
            var markerSize = cellSize * markerScale;
            marker.transform.localScale = Vector3.one * markerSize;

            if (cellType == LevelCellType.Treasure)
            {
                marker.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }
        }

        private static Transform CreateRoot(string rootName, Transform parent)
        {
            var root = new GameObject(rootName).transform;
            root.SetParent(parent, false);
            root.localPosition = Vector3.zero;
            return root;
        }

        private static GameObject CreateSpriteObject(
            Transform parent,
            string objectName,
            Vector3 localPosition,
            Sprite sprite,
            Color color,
            int sortingOrder)
        {
            var gameObject = new GameObject(objectName);
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;

            var spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = sortingOrder;

            return gameObject;
        }

        private void CreateCollider(Transform parent, int x, int y, Vector3 position, LevelCellType cellType)
        {
            var colliderObject = new GameObject($"Collider_{x}_{y}");
            colliderObject.transform.SetParent(parent, false);
            colliderObject.transform.localPosition = position;

            colliderObject.AddComponent<PolygonCollider2D>().points = GetScaledSlopePoints(cellType);
        }

        private void CreateWallColliders(Transform parent)
        {
            for (var y = 0; y < levelMapConfig.Height; y++)
            {
                var x = 0;
                while (x < levelMapConfig.Width)
                {
                    if (levelMapConfig.GetCell(x, y) != LevelCellType.Wall)
                    {
                        x++;
                        continue;
                    }

                    var startX = x;
                    while (x + 1 < levelMapConfig.Width && levelMapConfig.GetCell(x + 1, y) == LevelCellType.Wall)
                    {
                        x++;
                    }

                    CreateWallCollider(parent, startX, x, y);
                    x++;
                }
            }
        }

        private void CreateWallCollider(Transform parent, int startX, int endX, int y)
        {
            var colliderObject = new GameObject($"Collider_{startX}_{y}_{endX}");
            colliderObject.transform.SetParent(parent, false);

            var widthInCells = (endX - startX) + 1;
            var centerX = startX + ((widthInCells - 1) * 0.5f);
            colliderObject.transform.localPosition = GetCellPosition(centerX, y);
            colliderObject.AddComponent<BoxCollider2D>().size = new Vector2(widthInCells * cellSize, cellSize);
        }

        private void ScaleToMap(Transform target, Sprite sprite)
        {
            var spriteSize = sprite.bounds.size;
            var mapWidth = GetMapWidth();
            var mapHeight = GetMapHeight();

            target.localScale = new Vector3(mapWidth / spriteSize.x, mapHeight / spriteSize.y, 1f);
        }

        private Vector3 GetCellPosition(int x, int y)
        {
            return GetCellPosition((float)x, (float)y);
        }

        private Vector3 GetCellPosition(float x, float y)
        {
            var widthOffset = (levelMapConfig.Width - 1) * cellSize * 0.5f;
            var heightOffset = (levelMapConfig.Height - 1) * cellSize * 0.5f;

            return new Vector3((x * cellSize) - widthOffset, (y * cellSize) - heightOffset, 0f);
        }

        public Vector3 GetCellWorldPosition(int x, int y)
        {
            return transform.TransformPoint(GetCellPosition(x, y));
        }

        private static bool IsFrontCell(LevelCellType cellType)
        {
            return cellType == LevelCellType.Wall
                || cellType == LevelCellType.Slope45UpRight
                || cellType == LevelCellType.Slope45UpLeft;
        }

        private static bool IsSlopeCell(LevelCellType cellType)
        {
            return cellType == LevelCellType.Slope45UpRight
                || cellType == LevelCellType.Slope45UpLeft;
        }

        private static bool IsMarkerCell(LevelCellType cellType)
        {
            return cellType == LevelCellType.Monster
                || cellType == LevelCellType.Spawn
                || cellType == LevelCellType.Treasure;
        }

        private Color GetMarkerColor(LevelCellType cellType)
        {
            return cellType switch
            {
                LevelCellType.Monster => monsterColor,
                LevelCellType.Spawn => spawnColor,
                LevelCellType.Treasure => treasureColor,
                _ => Color.white
            };
        }

        private Vector2[] GetScaledSlopePoints(LevelCellType cellType)
        {
            var points = GetSlopePoints(cellType);
            for (var i = 0; i < points.Length; i++)
            {
                points[i] *= cellSize;
            }

            return points;
        }

        private float GetMapWidth()
        {
            return cellSize * levelMapConfig.Width;
        }

        private float GetMapHeight()
        {
            return cellSize * levelMapConfig.Height;
        }

        private float GetCameraWidth()
        {
            return GetCameraHeight() * targetCamera.aspect;
        }

        private float GetCameraHeight()
        {
            return targetCamera.orthographicSize * 2f;
        }

        private void FitCameraToLevel()
        {
            var requiredHeight = GetMapHeight() + topPadding + bottomPadding;
            var requiredWidth = GetMapWidth() + leftPadding + rightPadding;
            var heightBasedSize = requiredHeight * 0.5f;
            var widthBasedSize = requiredWidth / targetCamera.aspect * 0.5f;

            targetCamera.orthographicSize = Mathf.Max(heightBasedSize, widthBasedSize);
        }

        private static Vector2[] GetSlopePoints(LevelCellType cellType)
        {
            return cellType switch
            {
                LevelCellType.Slope45UpRight => new[]
                {
                    new Vector2(-0.5f, -0.5f),
                    new Vector2(0.5f, -0.5f),
                    new Vector2(0.5f, 0.5f)
                },
                LevelCellType.Slope45UpLeft => new[]
                {
                    new Vector2(-0.5f, 0.5f),
                    new Vector2(-0.5f, -0.5f),
                    new Vector2(0.5f, -0.5f)
                },
                _ => new[]
                {
                    new Vector2(-0.5f, -0.5f),
                    new Vector2(0.5f, -0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(-0.5f, 0.5f)
                }
            };
        }

        private Sprite CreateMaskedFrontSprite()
        {
            var frontSprite = levelMapConfig.FrontSprite;

            var sourceWidth = frontSprite == null
                ? levelMapConfig.Width * generatedPixelsPerCell
                : Mathf.RoundToInt(frontSprite.rect.width);

            var sourceHeight = frontSprite == null
                ? levelMapConfig.Height * generatedPixelsPerCell
                : Mathf.RoundToInt(frontSprite.rect.height);

            var pixels = frontSprite == null
                ? CreateSolidPixels(sourceWidth, sourceHeight, frontColor)
                : frontSprite.texture.GetPixels(
                    Mathf.RoundToInt(frontSprite.rect.x),
                    Mathf.RoundToInt(frontSprite.rect.y),
                    sourceWidth,
                    sourceHeight);

            for (var y = 0; y < sourceHeight; y++)
            {
                for (var x = 0; x < sourceWidth; x++)
                {
                    if (IsFrontPixelVisible(x, y, sourceWidth, sourceHeight))
                    {
                        continue;
                    }

                    pixels[(y * sourceWidth) + x] = Color.clear;
                }
            }

            _generatedFrontTexture = new Texture2D(sourceWidth, sourceHeight, TextureFormat.ARGB32, false)
            {
                filterMode = frontSprite == null ? FilterMode.Point : frontSprite.texture.filterMode,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            _generatedFrontTexture.SetPixels(pixels);
            _generatedFrontTexture.Apply();

            var pixelsPerUnit = frontSprite == null ? generatedPixelsPerCell : frontSprite.pixelsPerUnit;
            _generatedFrontSprite = Sprite.Create(
                _generatedFrontTexture,
                new Rect(0f, 0f, sourceWidth, sourceHeight),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit);

            _generatedFrontSprite.hideFlags = HideFlags.HideAndDontSave;
            return _generatedFrontSprite;
        }

        private bool IsFrontPixelVisible(int pixelX, int pixelY, int sourceWidth, int sourceHeight)
        {
            var normalizedX = (pixelX + 0.5f) / sourceWidth;
            var normalizedY = (pixelY + 0.5f) / sourceHeight;

            var mappedX = normalizedX * levelMapConfig.Width;
            var mappedY = normalizedY * levelMapConfig.Height;

            var cellX = Mathf.Min(levelMapConfig.Width - 1, Mathf.FloorToInt(mappedX));
            var cellY = Mathf.Min(levelMapConfig.Height - 1, Mathf.FloorToInt(mappedY));

            var localX = mappedX - cellX;
            var localY = mappedY - cellY;

            return IsFrontPixelVisible(levelMapConfig.GetCell(cellX, cellY), localX, localY);
        }

        private static bool IsFrontPixelVisible(LevelCellType cellType, float localX, float localY)
        {
            return cellType switch
            {
                LevelCellType.Wall => true,
                LevelCellType.Slope45UpRight => localY <= localX,
                LevelCellType.Slope45UpLeft => localY <= 1f - localX,
                _ => false
            };
        }

        private static Color[] CreateSolidPixels(int width, int height, Color color)
        {
            var pixels = new Color[width * height];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            return pixels;
        }

        private void DestroyGeneratedAssets()
        {
            if (_generatedFrontSprite != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_generatedFrontSprite);
                }
                else
                {
                    DestroyImmediate(_generatedFrontSprite);
                }

                _generatedFrontSprite = null;
            }

            if (_generatedFrontTexture == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_generatedFrontTexture);
            }
            else
            {
                DestroyImmediate(_generatedFrontTexture);
            }

            _generatedFrontTexture = null;
        }

        private static class LevelSpriteCache
        {
            private static Sprite _square;

            public static Sprite Square => _square ??= CreateSquareSprite();

            private static Sprite CreateSquareSprite()
            {
                var texture = Texture2D.whiteTexture;
                var sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    texture.width);

                sprite.name = "SquareCell";
                return sprite;
            }
        }
    }
}
