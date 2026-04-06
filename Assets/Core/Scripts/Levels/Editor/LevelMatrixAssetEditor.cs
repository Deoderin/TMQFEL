using System.IO;
using UnityEditor;
using UnityEngine;

namespace TMQFEL.Levels.Editor
{
    [CustomEditor(typeof(LevelMatrixAsset))]
    public sealed class LevelMatrixAssetEditor : UnityEditor.Editor
    {
        private const float CellSize = 44f;
        private const float HeaderSize = 22f;
        private const float RowLabelWidth = 24f;
        private const float PaletteButtonMinWidth = 96f;

        private static readonly BrushDefinition[] Brushes =
        {
            new(LevelCellType.Empty, "0 Empty", ".", new Color(0.16f, 0.16f, 0.16f)),
            new(LevelCellType.Wall, "1 Wall", "W", new Color(0.35f, 0.35f, 0.35f)),
            new(LevelCellType.Monster, "2 Monster", "M", new Color(0.75f, 0.28f, 0.28f)),
            new(LevelCellType.Spawn, "3 Spawn", "S", new Color(0.22f, 0.55f, 0.87f)),
            new(LevelCellType.Treasure, "4 Treasure", "T", new Color(0.95f, 0.74f, 0.18f)),
            new(LevelCellType.Slope45UpRight, "5 Slope /", "/", new Color(0.37f, 0.63f, 0.34f)),
            new(LevelCellType.Slope45UpLeft, "6 Slope \\", "\\", new Color(0.27f, 0.53f, 0.24f))
        };

        private static int _selectedBrushIndex = 1;

        private GUIStyle _cellLabelStyle;
        private int _pendingWidth;
        private int _pendingHeight;

        private void OnEnable()
        {
            var matrix = (LevelMatrixAsset)target;
            _pendingWidth = matrix.Width;
            _pendingHeight = matrix.Height;
        }

        public override void OnInspectorGUI()
        {
            var matrix = (LevelMatrixAsset)target;
            EnsureStyles();

            HandleBrushHotkeys();
            DrawSizeControls(matrix);
            EditorGUILayout.Space(8f);
            DrawPalette();
            EditorGUILayout.Space(8f);
            DrawSaveControls(matrix);
            EditorGUILayout.Space(8f);
            DrawGrid(matrix);
        }

        private void DrawSizeControls(LevelMatrixAsset matrix)
        {
            EditorGUILayout.LabelField("Matrix Size", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                _pendingWidth = EditorGUILayout.IntField("Width", _pendingWidth);
                _pendingHeight = EditorGUILayout.IntField("Height", _pendingHeight);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply Size"))
                {
                    ResizeMatrix(matrix, _pendingWidth, _pendingHeight);
                }

                if (GUILayout.Button("Reset 8x5"))
                {
                    _pendingWidth = LevelMatrixAsset.DefaultWidth;
                    _pendingHeight = LevelMatrixAsset.DefaultHeight;
                    ResizeMatrix(matrix, _pendingWidth, _pendingHeight);
                }
            }

            EditorGUILayout.HelpBox("Hotkeys: 0-6 select brush. Left mouse paints. Right mouse clears to 0.", MessageType.None);
        }

        private void DrawPalette()
        {
            EditorGUILayout.LabelField("Brushes", EditorStyles.boldLabel);

            var availableWidth = EditorGUIUtility.currentViewWidth - 40f;
            var buttonsPerRow = Mathf.Max(1, Mathf.FloorToInt(availableWidth / PaletteButtonMinWidth));

            for (var rowStart = 0; rowStart < Brushes.Length; rowStart += buttonsPerRow)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var rowEnd = Mathf.Min(rowStart + buttonsPerRow, Brushes.Length);

                    for (var i = rowStart; i < rowEnd; i++)
                    {
                        var brush = Brushes[i];
                        var previousColor = GUI.backgroundColor;
                        GUI.backgroundColor = brush.Color;

                        if (GUILayout.Toggle(
                                _selectedBrushIndex == i,
                                brush.Label,
                                "Button",
                                GUILayout.MinWidth(PaletteButtonMinWidth),
                                GUILayout.ExpandWidth(true)))
                        {
                            _selectedBrushIndex = i;
                        }

                        GUI.backgroundColor = previousColor;
                    }
                }
            }
        }

        private void DrawGrid(LevelMatrixAsset matrix)
        {
            EditorGUILayout.LabelField("Level Matrix", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(RowLabelWidth);

                for (var x = 0; x < matrix.Width; x++)
                {
                    GUILayout.Label(x.ToString(), EditorStyles.centeredGreyMiniLabel, GUILayout.Width(CellSize), GUILayout.Height(HeaderSize));
                }
            }

            for (var y = matrix.Height - 1; y >= 0; y--)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(y.ToString(), EditorStyles.centeredGreyMiniLabel, GUILayout.Width(RowLabelWidth), GUILayout.Height(CellSize));

                    for (var x = 0; x < matrix.Width; x++)
                    {
                        DrawCell(matrix, x, y);
                    }
                }
            }
        }

        private void DrawSaveControls(LevelMatrixAsset matrix)
        {
            EditorGUILayout.LabelField("Runtime Config", EditorStyles.boldLabel);

            if (GUILayout.Button("Save Config", GUILayout.Height(28f)))
            {
                SaveConfig(matrix);
            }
        }

        private void DrawCell(LevelMatrixAsset matrix, int x, int y)
        {
            var cellType = matrix.GetCell(x, y);
            var brush = Brushes[(int)cellType];
            var rect = GUILayoutUtility.GetRect(CellSize, CellSize, GUILayout.Width(CellSize), GUILayout.Height(CellSize));

            EditorGUI.DrawRect(rect, brush.Color);
            GUI.Box(rect, GUIContent.none);
            GUI.Label(rect, new GUIContent(brush.Symbol, $"{x},{y} {cellType}"), _cellLabelStyle);

            var currentEvent = Event.current;
            if (!rect.Contains(currentEvent.mousePosition))
            {
                return;
            }

            if (currentEvent.type != EventType.MouseDown && currentEvent.type != EventType.MouseDrag)
            {
                return;
            }

            if (currentEvent.button == 0)
            {
                PaintCell(matrix, x, y, Brushes[_selectedBrushIndex].CellType);
                currentEvent.Use();
            }
            else if (currentEvent.button == 1)
            {
                PaintCell(matrix, x, y, LevelCellType.Empty);
                currentEvent.Use();
            }
        }

        private void PaintCell(LevelMatrixAsset matrix, int x, int y, LevelCellType cellType)
        {
            Undo.RecordObject(matrix, "Paint Level Cell");
            matrix.SetCell(x, y, cellType);
            EditorUtility.SetDirty(matrix);
        }

        private void ResizeMatrix(LevelMatrixAsset matrix, int width, int height)
        {
            Undo.RecordObject(matrix, "Resize Level Matrix");
            matrix.Resize(width, height);
            EditorUtility.SetDirty(matrix);
        }

        private void SaveConfig(LevelMatrixAsset matrix)
        {
            var matrixPath = AssetDatabase.GetAssetPath(matrix);
            var matrixDirectory = Path.GetDirectoryName(matrixPath);
            var matrixName = Path.GetFileNameWithoutExtension(matrixPath);
            var configPath = Path.Combine(matrixDirectory!, $"{matrixName}_Config.asset").Replace("\\", "/");

            var config = AssetDatabase.LoadAssetAtPath<LevelMapConfig>(configPath);
            if (config == null)
            {
                config = CreateInstance<LevelMapConfig>();
                AssetDatabase.CreateAsset(config, configPath);
            }

            Undo.RecordObject(config, "Save Level Config");
            config.SetData(matrix.Width, matrix.Height, matrix.CopyCells());
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(config);
        }

        private void HandleBrushHotkeys()
        {
            var currentEvent = Event.current;
            if (currentEvent.type != EventType.KeyDown)
            {
                return;
            }

            var brushIndex = currentEvent.keyCode switch
            {
                KeyCode.Alpha0 => 0,
                KeyCode.Alpha1 => 1,
                KeyCode.Alpha2 => 2,
                KeyCode.Alpha3 => 3,
                KeyCode.Alpha4 => 4,
                KeyCode.Alpha5 => 5,
                KeyCode.Alpha6 => 6,
                KeyCode.Keypad0 => 0,
                KeyCode.Keypad1 => 1,
                KeyCode.Keypad2 => 2,
                KeyCode.Keypad3 => 3,
                KeyCode.Keypad4 => 4,
                KeyCode.Keypad5 => 5,
                KeyCode.Keypad6 => 6,
                _ => -1
            };

            if (brushIndex < 0)
            {
                return;
            }

            _selectedBrushIndex = brushIndex;
            currentEvent.Use();
            Repaint();
        }

        private void EnsureStyles()
        {
            if (_cellLabelStyle != null)
            {
                return;
            }

            _cellLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };
            _cellLabelStyle.normal.textColor = Color.white;
        }

        private readonly struct BrushDefinition
        {
            public BrushDefinition(LevelCellType cellType, string label, string symbol, Color color)
            {
                CellType = cellType;
                Label = label;
                Symbol = symbol;
                Color = color;
            }

            public LevelCellType CellType { get; }

            public string Label { get; }

            public string Symbol { get; }

            public Color Color { get; }
        }
    }
}
