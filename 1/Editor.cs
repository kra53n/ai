using Raylib_cs;
using System.Collections;
using System.Data;
using System.Diagnostics;
using System.Numerics;

class Editor
{
    private class EditorBlock
    {
        public int x;
        public int y;
        public Sokoban.Block? type;

        public EditorBlock(int _x, int _y, Sokoban.Block _type)
        {
            x = _x;
            y = _y;
            type = _type;
        }
    }

    private static List<EditorBlock> blocks = new List<EditorBlock> {
        new EditorBlock(Sokoban.BLOCK_SIZE * 0, 0, Sokoban.Block.Floor),
        new EditorBlock(Sokoban.BLOCK_SIZE * 1, 0, Sokoban.Block.Wall),
        new EditorBlock(Sokoban.BLOCK_SIZE * 2, 0, Sokoban.Block.Box),
        new EditorBlock(Sokoban.BLOCK_SIZE * 3, 0, Sokoban.Block.Mark),
        new EditorBlock(Sokoban.BLOCK_SIZE * 4, 0, Sokoban.Block.BoxOnMark),
        new EditorBlock(Sokoban.BLOCK_SIZE * 5, 0, Sokoban.Block.Worker),
    };

    private static Sokoban.Block? currBlock;

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
        if (Raylib.IsMouseButtonDown(MouseButton.Right))
        {
            currBlock = GetBlock(mouse);
        }
        if (Raylib.IsMouseButtonDown(MouseButton.Left) && mouse.Y != 0)
        {
            InsertBlock(mouse);
        }
        if (Raylib.IsKeyPressed(KeyboardKey.G))
        {
            Sokoban.mode = Sokoban.Mode.Game;
        }
        if (Raylib.IsKeyPressed(KeyboardKey.S))
        {
            File.WriteAllLines(Directory.GetCurrentDirectory() + "/level.txt", int2DArrayToStringArray(GetLevel()));
        }

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

    public static int[,] GetLevel()
    {
        int maxX = 0;
        int maxY = 0;
        foreach (EditorBlock block in blocks)
        {
            maxX = Math.Max(maxX, block.x);
            maxY = Math.Max(maxY, block.y);
        }
        int[,] level = new int[maxY / Sokoban.BLOCK_SIZE, maxX / Sokoban.BLOCK_SIZE + 1];
        for (int i = 0; i < level.GetLength(0); i++)
        {
            for (int j = 0; j < level.GetLength(1); j++)
            {
                level[i, j] = 9;
            }
        }
        foreach (EditorBlock block in blocks)
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
            level[block.y / Sokoban.BLOCK_SIZE - 1, block.x / Sokoban.BLOCK_SIZE] = v;
        }
        return level;
    }
    
    private static string[] int2DArrayToStringArray(int[,] intArr)
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

    private static void LoadLevel(int[,] map)
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
                blocks.Add(new EditorBlock(j * Sokoban.BLOCK_SIZE, i * Sokoban.BLOCK_SIZE + Sokoban.BLOCK_SIZE, (Sokoban.Block)map[i, j]));
            }
        }
    }

    private static void DrawBlocks()
    {
        foreach (EditorBlock block in blocks)
        {
            switch (block.type)
            {
            case Sokoban.Block.Floor:
                Floor.Draw(block.x, block.y);
                break;
            case Sokoban.Block.Wall:
                Wall.Draw(block.x, block.y);
                break;
            case Sokoban.Block.Box:
                Box.Draw(block.x, block.y);
                break;
            case Sokoban.Block.Mark:
                Mark.Draw(block.x, block.y);
                break;
            case Sokoban.Block.BoxOnMark:
                BoxOnMark.Draw(block.x, block.y);
                break;
            case Sokoban.Block.Worker:
                Worker.DrawStatic(block.x, block.y);
                break;
            }
        }
    }

   private static bool MouseOnBlock(Vector2 mouse)
    {
        foreach (EditorBlock block in blocks)
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

    private static Sokoban.Block? GetBlock(Vector2 p)
    {
        foreach (EditorBlock block in blocks)
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
        foreach (EditorBlock block in blocks)
        {
            if (block.x == p.X && block.y == p.Y)
            {
                block.type = currBlock;
                return;
            }
        }
        if (currBlock != null)
        {
            blocks.Add(new EditorBlock((int)p.X, (int)p.Y, (Sokoban.Block)currBlock));
        }
    }
}
