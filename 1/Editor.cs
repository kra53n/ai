using Raylib_cs;
using System.Collections;
using System.Data;
using System.Diagnostics;
using System.Numerics;
using System.Text.RegularExpressions;
using TinyDialogsNet;
using static Sokoban;

public class Editor
{
    private static string savePath = Sokoban.openPath;

    private static List<Block> blocks = new List<Block> {
        new Block(Sokoban.BLOCK_SIZE * 0, 0, Block.Type.Floor),
        new Block(Sokoban.BLOCK_SIZE * 1, 0, Block.Type.Wall),
        new Block(Sokoban.BLOCK_SIZE * 2, 0, Block.Type.Box),
        new Block(Sokoban.BLOCK_SIZE * 3, 0, Block.Type.Mark),
        new Block(Sokoban.BLOCK_SIZE * 4, 0, Block.Type.BoxOnMark),
        new Block(Sokoban.BLOCK_SIZE * 5, 0, Block.Type.Worker),
    };
    private static int defaultBlocksCount = blocks.Count;

    private static Block.Type? currBlock;
    private static KeyboardKey[] blockShortcuts = {
        KeyboardKey.Q,
        KeyboardKey.W,
        KeyboardKey.E,
        KeyboardKey.R,
        KeyboardKey.D,
        KeyboardKey.F,
        KeyboardKey.A
    };
    private static bool isSaved = false;

    private static Func<bool> IsLeftMouseButtonDown = () => Raylib.IsMouseButtonDown(MouseButton.Left) || Raylib.IsKeyDown(KeyboardKey.One);
    private static Func<bool> IsRightMouseButtonDown = () => Raylib.IsMouseButtonDown(MouseButton.Right) || Raylib.IsKeyDown(KeyboardKey.Two);

    public static void Update()
    {
        Vector2 mouse = GetMousePosition();

        if (Raylib.IsFileDropped())
        {
            var files = Raylib.GetDroppedFiles();
            if (files.Length == 1)
            {
                LoadLevel(Sokoban.LoadMapContentFromFile(files[0]));
            }
            else
            {
                Raylib.SetWindowTitle("Только один файл можно загрузить за раз");
            }
        }

        if (IsLeftMouseButtonDown() && mouse.Y != 0)
        {
            InsertBlock(mouse);
        }
        else if (IsRightMouseButtonDown() || IsLeftMouseButtonDown())
        {
            currBlock = GetBlock(mouse);
            changeTitleByCurrBlockChanging();
        }

        UpdateCurrBlockByKeyboard();


        if (Raylib.IsKeyPressed(KeyboardKey.M))
        {
            LoadLevel(Map.GetEmptySquareMap(16, 16).map);
        }

        if (Raylib.IsKeyDown(KeyboardKey.LeftControl))
        {
            if (Raylib.IsKeyPressed(KeyboardKey.F))
            {
                var filter = new FileFilter(".txt files", ["*.txt"]);
                var (canceled, openPaths) = TinyDialogs.OpenFileDialog("Choose level", openPath, false, filter);
                if (!canceled)
                {
                    openPath = openPaths.First();
                    savePath = openPath;
                    LoadLevel(Sokoban.LoadMapContentFromFile(openPath));
                }
            }
            if (Raylib.IsKeyPressed(KeyboardKey.G) || Raylib.IsKeyPressed(KeyboardKey.E))
            {
                Sokoban.mode = Sokoban.Mode.Game;
                var level = GetLevel();
                if (level != null)
                {
                    Sokoban.LoadAndApplyMap(level);
                }
                Sokoban.Rescale();
            }

            if (Raylib.IsKeyPressed(KeyboardKey.S))
            {
                var level = GetLevel();
                if (level != null)
                { 
                    var filter = new FileFilter(".txt files", ["*.txt"]);
                    if (!savePath.EndsWith(".txt"))
                    {
                        savePath += "level.txt";
                    }
                    (var canceled, savePath) = TinyDialogs.SaveFileDialog("Save level", savePath, filter);
                    if (!canceled)
                    {
                        File.WriteAllLines(savePath, byte2DArrayToStringArray(level));
                        Raylib.SetWindowTitle($"Saved as ({savePath})");
                        savePath = savePath.Substring(0, savePath.LastIndexOf("\\") + 1);
                    }
                    //int minIndex = 0;
                    //foreach (var file in Directory.GetFiles(Directory.GetCurrentDirectory(), "level*.txt"))
                    //{
                    //    var r = new Regex(".*level(.*?)\\.txt").Match(file);
                    //    if (r.Groups[1].Value != "")
                    //    {
                    //        minIndex = Math.Max(minIndex, int.Parse(r.Groups[1].Value));
                    //    }
                    //}
                    //minIndex++;
                    //File.WriteAllLines(Directory.GetCurrentDirectory() + $"/level{minIndex}.txt", byte2DArrayToStringArray(level));
                    //Raylib.SetWindowTitle($"Saved as ({Directory.GetCurrentDirectory() + $"/level{minIndex}.txt"})");

                }
            }
        }
        else
        {
            if (Raylib.IsKeyPressed(KeyboardKey.S))
            {
                var level = GetLevel();
                if (level != null)
                {
                    File.WriteAllLines(Directory.GetCurrentDirectory() + $"/level.txt", byte2DArrayToStringArray(level));
                    Raylib.SetWindowTitle($"Saved as ({Directory.GetCurrentDirectory() + $"/level.txt"})");
                }
            }
        }
    }

    private static void UpdateCurrBlockByKeyboard()
    {
        for (int i = 0; i < blockShortcuts.Length; i++)
        {
            if (Raylib.IsKeyPressed(blockShortcuts[i]))
            {
                if (i == 6)
                {
                    i = 9;
                }
                currBlock = (Block.Type)i;
                changeTitleByCurrBlockChanging();
                break;
            }
        }
    }

    private static void changeTitleByCurrBlockChanging()
    {
        Raylib.SetWindowTitle($"Редактор Sokoban, блок({currBlock})");
    }

    public static void Draw()
    {
        Vector2 mouse = GetMousePosition();
        DrawBlocks();

        Color color = Color.White;
        color.A = 0x18;
        if (MouseOnBlock(mouse))
        {
            color = Color.Black;
            color.A = 0xff - 0x80;
        }
        Raylib.DrawRectangle((int)mouse.X, (int)mouse.Y, Sokoban.BLOCK_SIZE, Sokoban.BLOCK_SIZE, color);
    }

    public static byte[,]? GetLevel()
    {
        int maxX = 0;
        int maxY = 0;
        int minX = int.MaxValue;
        int minY = int.MaxValue;
        foreach (Block block in blocks)
        {
            if (block.type != Block.Type.Empty && block.y != 0)
            {
                maxX = Math.Max(maxX, block.x);
                maxY = Math.Max(maxY, block.y);
                minX = Math.Min(minX, block.x);
                minY = Math.Min(minY, block.y);
            }
        }
        for (int i = blocks.Count - 1; i >= 0; i--)
        {
            var block = blocks[i];
            if (block.type == Block.Type.Empty && (block.x < minX || block.x > maxX || block.y < minY || block.y > maxY))
            {
                blocks.RemoveAt(i);
            }
        }
        if (blocks.Count == defaultBlocksCount)
        {
            return null;
        }

        maxX -= minX;
        maxY -= minY;
        byte[,] level = new byte[maxY / Sokoban.BLOCK_SIZE + 3, maxX / Sokoban.BLOCK_SIZE + 3];
        for (int i = 0; i < level.GetLength(0); i++)
        {
            for (int j = 0; j < level.GetLength(1); j++)
            {
                level[i, j] = 9;
            }
        }
        foreach (Block block in blocks)
        {
            int v;
            if (block.type == null)
            {
                v = 9;
            }
            else
            {
                v = (int)block.type;
            }
            if (block.y == 0)
            {
                continue;
            }
            level[(block.y - minY) / Sokoban.BLOCK_SIZE + 1, (block.x - minX) / Sokoban.BLOCK_SIZE + 1] = (byte)v;
        }
        return level;
    }

    private static string[] byte2DArrayToStringArray(byte[,] intArr)
    {
        string[] res = new string[intArr.GetLength(0)];
        for (int i = 0; i < intArr.GetLength(0); i++)
        {
            string s = "";
            for (int j = 0; j < intArr.GetLength(1); j++)
            {
                s += intArr[i, j];
            }
            res[i] = s;
        }
        return res;
    }

    private static void LoadLevel(byte[,] map)
    {
        for (int i = 0; i < blocks.Count; i++)
        {
            if (blocks[i].y == 0)
            {
                continue;
            }
            blocks.Remove(blocks[i]);
            i--;
        }
        for (int i = 0; i < map.GetLength(0); i++)
        {
            for (int j = 0; j < map.GetLength(1); j++)
            {
                blocks.Add(new Block(j * Sokoban.BLOCK_SIZE, i * Sokoban.BLOCK_SIZE + Sokoban.BLOCK_SIZE, (Block.Type)map[i, j]));
            }
        }
    }

    private static void DrawBlocks()
    {
        foreach (Block block in blocks)
        {
            switch (block.type)
            {
                case Block.Type.Floor:
                    Floor.Draw(block.x, block.y);
                    break;
                case Block.Type.Wall:
                    Wall.Draw(block.x, block.y);
                    break;
                case Block.Type.Box:
                    Box.Draw(block.x, block.y);
                    break;
                case Block.Type.Mark:
                    Mark.Draw(block.x, block.y);
                    break;
                case Block.Type.BoxOnMark:
                    BoxOnMark.Draw(block.x, block.y);
                    break;
                case Block.Type.Worker:
                    Worker.DrawStatic(block.x, block.y);
                    break;
            }
        }
    }

    private static bool MouseOnBlock(Vector2 mouse)
    {
        foreach (Block block in blocks)
        {
            if (CalculateNearest((int)mouse.X, Sokoban.BLOCK_SIZE) == block.x && CalculateNearest((int)mouse.Y, Sokoban.BLOCK_SIZE) == block.y)
            {
                return block.type != null;
            }
        }
        return false;
    }

    private static int CalculateNearest(int v, int size)
    {
        return (v / size) * size;
    }

    private static Vector2 GetMousePosition()
    {
        Vector2 mouse = Raylib.GetMousePosition();
        mouse.X = CalculateNearest((int)mouse.X, Sokoban.BLOCK_SIZE);
        mouse.Y = CalculateNearest((int)mouse.Y, Sokoban.BLOCK_SIZE);
        return mouse;
    }

    private static Block.Type? GetBlock(Vector2 p)
    {
        foreach (Block block in blocks)
        {
            if (block.x == p.X && block.y == p.Y)
            {
                return block.type;
            }
        }
        return null;
    }

    private static void InsertBlock(Vector2 p)
    {
        foreach (Block block in blocks)
        {
            if (block.x == p.X && block.y == p.Y)
            {
                block.type = currBlock == null ? Block.Type.Empty : (Block.Type)currBlock;
                return;
            }
        }
        if (currBlock != null)
        {
            blocks.Add(new Block((int)p.X, (int)p.Y, Block.Type.Empty));
        }
    }
}
