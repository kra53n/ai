using Raylib_cs;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using static Worker;

class Sokoban
{
    public const string TEXTURE = "../../../assets.png";
    public const int WIDTH = 800;
    public const int HEIGHT = 800;
    public static float SCALE = 1;
    public static int BLOCK_SIZE = (int)(32 * SCALE);
    
    public const double ANIMATION_DELAY_BASE = 0.5;
    public static double ANIMATION_DELAY = 0.5;
    public static double playbackSpeed = 1;

    public static Mode mode = Mode.Game;
    public static Texture2D texture;
    public static Map? map;
    public static (byte x, byte y)[]? boxes = null;
    public static Worker worker = new Worker(0, 0);

    public static State? baseState = null;
    public static List<State>? states = null;
    public static int currStateIdx = 0;

    private static Action ControlsProcessor = GameControlsProcessor;
    public static string searchMethod = "";

    public enum Mode
    {
        Game,
        Edit,
        Replay
    }

    public static void Main(string[] args)
    {
        if (args.Length == 1)
        {
            Init(args[0]);
        }
        else
        {
            Init();
        }

        Raylib.SetTargetFPS(60);

        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(0x18, 0x18, 0x18, 0xff));

            switch (mode)
            {
                case Mode.Replay:
                case Mode.Game:
                    map.Draw(boxes);
                    Update();
                    break;
                case Mode.Edit:
                    Editor.Draw();
                    Editor.Update();
                    break;
            }

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }

    public static void Rescale()
    {
        SCALE = Math.Min((float)WIDTH / map.map.GetLength(1) / BLOCK_SIZE, (float)HEIGHT / map.map.GetLength(0) / BLOCK_SIZE);
    }

    public static void ReplayControlsProcessor()
    {
        if (Raylib.IsKeyDown(KeyboardKey.LeftControl))
        {
            if (Raylib.IsKeyPressed(KeyboardKey.A) || Raylib.IsKeyPressed(KeyboardKey.Left))
            {
                double increment = 0.25;
                if (playbackSpeed <= 2)
                {
                    increment = 0.25;
                }
                else if (playbackSpeed <= 6)
                {
                    increment = 1;
                }
                else
                {
                    increment = 2;
                }
                playbackSpeed = Math.Max(playbackSpeed - increment, 0.25);
                ANIMATION_DELAY = ANIMATION_DELAY_BASE / playbackSpeed;
                Raylib.SetWindowTitle($"{(Animator.Animating ? "⏵" : "⏸")} Режим воспроизведения (x{playbackSpeed}) - " + searchMethod);
            }
            if (Raylib.IsKeyPressed(KeyboardKey.D) || Raylib.IsKeyPressed(KeyboardKey.Right))
            {
                double increment = 0.25;
                if (playbackSpeed < 2)
                {
                    increment = 0.25;
                }
                else if (playbackSpeed < 6)
                {
                    increment = 1;
                }
                else
                {
                    increment = 2;
                }
                playbackSpeed = playbackSpeed + increment;
                ANIMATION_DELAY = ANIMATION_DELAY_BASE / playbackSpeed;
                Raylib.SetWindowTitle($"{(Animator.Animating ? "⏵" : "⏸")} Режим воспроизведения (x{playbackSpeed}) - " + searchMethod);
            }
        } 
        else 
        {
            if (Raylib.IsKeyPressed(KeyboardKey.A) || Raylib.IsKeyPressed(KeyboardKey.Left))
            {
                Animator.Pause();
                PrevState();
            }
            if (Raylib.IsKeyPressed(KeyboardKey.D) || Raylib.IsKeyPressed(KeyboardKey.Right))
            {
                Animator.Pause();
                NextState();
            }
            if (Raylib.IsKeyPressed(KeyboardKey.R))
            {
                Animator.Pause();
                SwitchToFirstState();
                //worker = (Worker)baseState.worker.Clone();
            }
            if (Raylib.IsKeyPressed(KeyboardKey.Space))
            {
                Animator.PlayOrPause();
            }
        }

        Animator.Animate();
    }

    public static void ProcessSearch(string searchMethod, ISearcher<List<State>> searcher)
    {
        Sokoban.searchMethod = searchMethod;
        Raylib.SetWindowTitle($"Осуществляется {searchMethod}");
        currStateIdx = 0;
        states = searcher.Search();
        Raylib.SetWindowTitle($"{char.ToUpper(searchMethod[0]) + searchMethod.Substring(1)} завершён");
    }

    public static void GameControlsProcessor()
    {
        if (Raylib.IsKeyDown(KeyboardKey.LeftControl))
        {
            return;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.W) || Raylib.IsKeyPressed(KeyboardKey.Up))
        {
            worker.Up(boxes);
        }
        if (Raylib.IsKeyPressed(KeyboardKey.A) || Raylib.IsKeyPressed(KeyboardKey.Left))
        {
            worker.Left(boxes);
        }
        if (Raylib.IsKeyPressed(KeyboardKey.S) || Raylib.IsKeyPressed(KeyboardKey.Down))
        {
            worker.Down(boxes);
        }
        if (Raylib.IsKeyPressed(KeyboardKey.D) || Raylib.IsKeyPressed(KeyboardKey.Right))
        {
            worker.Right(boxes);
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Zero + 1))
        {
            ProcessSearch("поиск в ширину", new Searcher(Searcher.Type.Breadth));
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Zero + 2))
        {
            ProcessSearch("поиск в глубину", new Searcher(Searcher.Type.Depth));
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Zero + 3))
        {
            ProcessSearch("поиск в глубину с итеративным углеблением", new DepthFirstSearch());
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Zero + 4))
        {
            ProcessSearch("двунаправленный поиск", new BidirectionalSearch());
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Zero + 5))
        {
            ProcessSearch("Эвристика Best", new InformedSearch(InformedSearch.BestHeuristic, "лучшего"));
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Zero + 6))
        {
            ProcessSearch("Эвристика Better", new InformedSearch(InformedSearch.BetterHeuristic, "получше"));
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Zero + 7))
        {
            ProcessSearch("Эвристика Mid", new InformedSearch(InformedSearch.MidHeuristic, "среднего"));
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Zero + 8))
        {
            ProcessSearch("Эвристика Worst", new InformedSearch(InformedSearch.WorstHeuristic, "худшего"));
        }

        if (Raylib.IsKeyPressed(KeyboardKey.R))
        {
            Animator.Pause();
            //SwitchToFirstState();
            baseState.boxes.CopyTo(boxes, 0);
            worker = (Worker)baseState.worker.Clone();
            return;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            ToggleReplayGameMode();
        }

        if (map.Complete(boxes))
        {
            Raylib.SetWindowTitle("Игра пройдена");
        }
    }

    public static void ToggleReplayGameMode()
    {
        if (mode != Mode.Replay)
        {
            if (!ShowCurrentState())
            {
                Raylib.SetWindowTitle("Не был проведён поиск, невозможно посмотреть путь");
                return;
            }
            ControlsProcessor = ReplayControlsProcessor;
            mode = Mode.Replay;
            Raylib.SetWindowTitle($"{(Animator.Animating ? "⏵" : "⏸")} Режим воспроизведения (x{playbackSpeed}) - " + searchMethod);
        }
        else
        {
            Animator.Pause();
            ControlsProcessor = GameControlsProcessor;
            map = (Map)map.Clone();
            worker = (Worker)worker.Clone();
            mode = Mode.Game;
            Raylib.SetWindowTitle("Режим игры");
        }
    }

    public static void LoadFromFolder(int index)
    {
        var files = Directory.GetFiles("../../../levels");
        if (files.Length - 1 < index)
        {
            return;
        }
        LoadAndApplyMap(files[index]);
    }

    public static void GlobalControlsProcessor()
    {
        if (Raylib.IsKeyDown(KeyboardKey.LeftControl))
        {
            if (Raylib.IsKeyPressed(KeyboardKey.R))
            {
                ToggleReplayGameMode();
            }
            for (int i = 0; i <= 9; i++)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.One + i))
                {
                    if (mode == Mode.Replay)
                    {
                        ToggleReplayGameMode();
                    }
                    LoadFromFolder(i);
                    break;
                }
            }
            return;
        }
        if (Raylib.IsKeyPressed(KeyboardKey.E))
        {
            mode = Mode.Edit;
            SCALE = 1;
        }
        if (Raylib.IsKeyPressed(KeyboardKey.F))
        {
            Process.Start("explorer.exe", Directory.GetCurrentDirectory());
        } 
    }

    public static void Update()
    {
        if (Raylib.IsFileDropped())
        {
            var files = Raylib.GetDroppedFiles();
            if (files.Length == 1)
            {
                LoadAndApplyMap(files[0]);
                if (mode == Mode.Replay)
                {
                    ToggleReplayGameMode();
                }
            }
            else
            {
                Raylib.SetWindowTitle("Только один файл можно загрузить за раз");
            }
        }

        ControlsProcessor();
        GlobalControlsProcessor();
    }

    public static void Init(string? file = null)
    {
        Raylib.InitWindow(WIDTH, HEIGHT, "СИИ 1 лаба - игра Сокобан");

        Image assetImage = Raylib.LoadImage(TEXTURE);
        unsafe
        {
            Raylib.ImageResizeNN(&assetImage, (int)(assetImage.Width * SCALE), (int)(assetImage.Height * SCALE));
        }
        texture = Raylib.LoadTextureFromImage(assetImage);
        Raylib.UnloadImage(assetImage);

        map = new Map(0, 0);
        try
        {
            LoadAndApplyMap(file);
        } 
        catch (Exception)
        {
            map.Load(new byte[,] {
                { 1, 1, 1, 1, 9, 9, 9, 9, 9, 9 },
                { 1, 0, 3, 1, 9, 9, 9, 9, 9, 9 },
                { 1, 0, 0, 1, 1, 1, 9, 9, 9, 9 },
                { 1, 4, 0, 0, 0, 1, 9, 9, 9, 9 },
                { 1, 0, 0, 2, 5, 1, 9, 9, 9, 9 },
                { 1, 0, 0, 1, 1, 1, 9, 9, 9, 9 },
                { 1, 1, 1, 1, 9, 9, 9, 9, 9, 9 },
                { 9, 9, 9, 9, 9, 9, 9, 9, 9, 9 },
                { 9, 9, 9, 9, 9, 9, 9, 9, 9, 9 },
                { 9, 9, 9, 9, 9, 9, 9, 9, 9, 9 }
            });
            Rescale();
        }
    }

    public static void LoadAndApplyMap(string file)
    {
        map.Load(LoadMapContentFromFile(file));
        Rescale();
    }

    public static byte[,] LoadMapContentFromFile(string file)
    {
        string[] lines = File.ReadAllLines(file);
        byte[,] content = new byte[lines.Length, lines[0].Length];
        for (int i = 0; i < lines.Length; i++)
        {
            var cols = lines[i].ToCharArray();
            for (int j = 0; j < cols.Length; j++)
                content[i, j] = (byte)(cols[j] - '0');
        }
        states = null;
        return content;
    }

    public static void SwitchToFirstState()
    {
        currStateIdx = -1;
        NextState();
    }

    public static bool ShowCurrentState()
    {
        if (states == null)
        {
            return false;
        }
        boxes = states[currStateIdx].boxes;
        worker = states[currStateIdx].worker;
        return true;
    }

    public static void NextState()
    {
        if (states == null || states.Count == 0)
        {
            return;
        }
        currStateIdx = Math.Min(currStateIdx + 1, states.Count - 1);
        boxes = states[currStateIdx].boxes;
        worker = states[currStateIdx].worker;
    }

    public static void PrevState()
    {
        if (states == null || states.Count == 0)
        {
            return;
        }
        currStateIdx = Math.Max(currStateIdx - 1, 0);
        boxes = states[currStateIdx].boxes;
        worker = states[currStateIdx].worker;
    }

    public static bool LastState()
    {
        return states != null && states.Count > 0 && currStateIdx == states.Count - 1;
    }
}



partial class Block
{
    public enum Type : byte
    {
        Floor = 0,
        Wall,
        Box,
        Mark,
        BoxOnMark,
        Worker,
        Empty = 9,
    };

    public int x;
    public int y;
    public Block.Type type;
    
    public Block(int _x, int _y, Block.Type _type)
    {
        x = _x;
        y = _y;
        type = _type;
    }

    //public override bool Equals(object? obj)
    //{
    //    if (obj == null) return false;
    //    var o = obj as Block;
    //    return o.x == x && o.y == y;
    //}

    public static (byte x, byte y)[] CloneBlocks((byte x, byte y)[] old)
    {
        (byte x, byte y)[] blocks = new (byte x, byte y)[Sokoban.baseState.boxes.Length];
        for (int i = 0; i < Sokoban.baseState.boxes.Length; i++)
        {
            blocks[i] = ((byte x, byte y))old[i];
        }
        return blocks;
    }
}

partial class Map : ICloneable
{
    // TODO(kra53n): look at memory usage if we will use Block.Type instead of byte
    public byte[,]? map;
    public int x;
    public int y;
    public (byte x, byte y)[]? marks;
    
    public Map(int _x, int _y)
    {
        x = _x;
        y = _y;
    }
    public void Load(byte[,] _map)
    {
        map = _map;
        List<(byte x, byte y)> _marks = new();
        List<(byte x, byte y)> boxes = new();
        for (int row = 0; row < GetRowsNum(); row++)
        {
            for (int col = 0; col < GetColsNum(); col++)
            {
                byte cell = GetCell(row, col);
                switch (cell)
                {
                case (byte)Block.Type.Worker:
                    Sokoban.worker = new Worker(col, row);
                    SetCell(row, col, (int)Block.Type.Floor);
                    break;
                case (byte)Block.Type.Box:
                case (byte)Block.Type.BoxOnMark:
                    boxes.Add(((byte x, byte y))(col, row));
                    if (cell == (byte)Block.Type.BoxOnMark)
                    {
                        map[row, col] = (byte)Block.Type.Mark;
                        _marks.Add(((byte)col, (byte)row));
                    }
                    else if (cell == (byte)Block.Type.Box)
                    {
                        map[row, col] = (byte)Block.Type.Floor;
                    }
                    break;
                    case (byte)Block.Type.Mark:
                        _marks.Add(((byte)col, (byte)row));
                        break;
                }
            }
        }
        marks = _marks.ToArray();
        Sokoban.boxes = boxes.ToArray(); // TODO(kra53n): maybe do as marks
        Sokoban.baseState = new State(((byte x, byte y)[])Sokoban.boxes.Clone(), (Worker)Sokoban.worker.Clone());
    }

    public IEnumerable<(int col, int row)> FindBlocks(Block.Type block)
    {
        for (int row = 0; row < GetRowsNum(); row++)
        {
            for (int col = 0; col < GetColsNum(); col++)
            {
                if (map[row, col] == (int)block)
                {
                    yield return (col, row);
                }
            }
        }
    }

    public void Draw((byte x, byte y)[] boxes)
    {
        int _x = x;
        int _y = y;
        for (int row = 0; row < map.GetLength(0); row++)
        {
            for (int col = 0; col < map.GetLength(1); col++)
            {
                switch (map[row, col])
                {
                case (byte)Block.Type.Floor:
                    Floor.Draw(_x, _y);
                    break;
                case (byte)Block.Type.Wall:
                    Wall.Draw(_x, _y);
                    break;
                case (byte)Block.Type.Mark:
                    Mark.Draw(_x, _y);
                    break;
                }
                _x += (int)(Sokoban.BLOCK_SIZE * Sokoban.SCALE);
            }
            _y += (int)(Sokoban.BLOCK_SIZE * Sokoban.SCALE);
            _x = x;
        }
        foreach ((byte x, byte y) b in boxes)
        {
            _x = b.x * (int)(Sokoban.BLOCK_SIZE * Sokoban.SCALE);
            _y = b.y * (int)(Sokoban.BLOCK_SIZE * Sokoban.SCALE);
            byte cell = GetCell(b.y, b.x);
            if (cell == (byte)Block.Type.Mark)
            {
                BoxOnMark.Draw(_x, _y);
            }
            else if (cell == (byte)Block.Type.Floor)
            {
                Box.Draw(_x, _y);
            }
        }
        Sokoban.worker.Draw(x, y);
    }

    public int GetRowsNum()
    {
        return map.GetLength(0);
    }

    public int GetColsNum()
    {
        return map.GetLength(1);
    }

    public byte GetCell(int row, int col)
    {
        return map[row, col];
    }

    public void SetCell(int row, int col, byte val)
    {
        map[row, col] = val;
    }

    public bool Complete((byte x, byte y)[] boxes)
    {
        foreach ((byte x, byte y) b in boxes)
        {
            if (GetCell(b.y, b.x) != (byte)Block.Type.Mark)
            {
                return false;
            }
        }
        return true;
    }

    public bool Stepped(in Worker worker, (byte x, byte y)[] boxes)
    {
        foreach ((byte x, byte y) b in boxes)
        {
            if (worker.x == b.x && worker.y == b.y)
            {
                return true;
            }
        }
        return map[worker.y, worker.x] == (byte)Block.Type.Wall;
    }

    public bool CanMoveBox(in Worker worker, Worker.Direction dir, (byte x, byte y)[] boxes)
    {
        if (
            worker.x == 0 || worker.y == 0 ||
            worker.x == map.GetLength(1)-1 || worker.y == map.GetLength(0)-1 ||
            GetCell(worker.y, worker.x) == (byte)Block.Type.Wall
        )
        {
            return false;
        }
        int x = worker.x + dir.GetX();
        int y = worker.y + dir.GetY();
        foreach ((byte x, byte y) b1 in boxes)
        {
            foreach((byte x, byte y) b2 in boxes)
            {
                if (b1.x == worker.x && b1.y == worker.y && b2.x == x && b2.y == y)
                {
                    return false;
                }
            }
        }
        int cell = map[y, x];
        return cell != (byte)Block.Type.Wall;
    }

    public void MoveBox(in Worker worker, Worker.Direction dir, (byte x, byte y)[] boxes)
    {
        int x = worker.x;
        int y = worker.y;
        switch (dir)
        {
        case Worker.Direction.Up:
            y = worker.y-1;
            break;
        case Worker.Direction.Left:
            x = worker.x-1;
            break; 
        case Worker.Direction.Down:
            y = worker.y+1;
            break;
        case Worker.Direction.Right:
            x = worker.x+1;
            break;
        default:
            return;
        }

        // TODO(kra53n): delete codes below
        //for (int i = 0; i < boxes.Count; i++)
        //{
        //    if (worker.x == boxes[i].x && worker.y == boxes[i].y)
        //    {
        //        Block b = boxes[i];
        //        b.x = x;
        //        b.y = y;
        //        boxes[i] = b;
        //        break;
        //    }
        //}
        for (int i = 0; i < boxes.Length; i++)
        {
            if (worker.x == boxes[i].x && worker.y == boxes[i].y)
            {
                boxes[i].x = (byte)x;
                boxes[i].y = (byte)y;
            }
        }
    }

    public object Clone()
    {
        if (map is null)
        {
            throw new NoNullAllowedException("map is null");
        }
        Map m = new Map(x, y);
        m.map = (byte[,])map.Clone();
        m.marks = ((byte, byte)[])marks.Clone();
        return m;
    }
}

class Box
{
    static int TEXTURE_POS = 0;

    public static void Draw(int x, int y)
    {
        Raylib.DrawTexturePro(Sokoban.texture, new Rectangle(TEXTURE_POS * Sokoban.BLOCK_SIZE, 0, Sokoban.BLOCK_SIZE, Sokoban.BLOCK_SIZE), new(x, y, Sokoban.BLOCK_SIZE * Sokoban.SCALE, Sokoban.BLOCK_SIZE * Sokoban.SCALE), new(0, 0), 0, Color.White);
    }
}

class BoxOnMark
{
    static int TEXTURE_POS = 1;

    public static void Draw(int x, int y)
    {
        Raylib.DrawTexturePro(Sokoban.texture, new Rectangle(TEXTURE_POS * Sokoban.BLOCK_SIZE, 0, Sokoban.BLOCK_SIZE, Sokoban.BLOCK_SIZE), new(x, y, Sokoban.BLOCK_SIZE * Sokoban.SCALE, Sokoban.BLOCK_SIZE * Sokoban.SCALE), new(0, 0), 0, Color.White);
    }
}

class Floor
{
    static int TEXTURE_POS = 3;

    public static void Draw(int x, int y)
    {
        Raylib.DrawTexturePro(Sokoban.texture, new Rectangle(TEXTURE_POS * Sokoban.BLOCK_SIZE, 0, Sokoban.BLOCK_SIZE, Sokoban.BLOCK_SIZE), new(x, y, Sokoban.BLOCK_SIZE * Sokoban.SCALE, Sokoban.BLOCK_SIZE * Sokoban.SCALE), new(0, 0), 0, Color.White);
    }
}

class Mark
{
    static int TEXTURE_POS = 2;

    public static void Draw(int x, int y)
    {
        Raylib.DrawTexturePro(Sokoban.texture, new Rectangle(TEXTURE_POS * Sokoban.BLOCK_SIZE, 0, Sokoban.BLOCK_SIZE, Sokoban.BLOCK_SIZE), new(x, y, Sokoban.BLOCK_SIZE * Sokoban.SCALE, Sokoban.BLOCK_SIZE * Sokoban.SCALE), new(0, 0), 0, Color.White);
    }
}

class Wall
{
    static int TEXTURE_POS = 4;

    public static void Draw(int x, int y)
    {
        Raylib.DrawTexturePro(Sokoban.texture, new Rectangle(TEXTURE_POS * Sokoban.BLOCK_SIZE, 0, Sokoban.BLOCK_SIZE, Sokoban.BLOCK_SIZE), new(x, y, Sokoban.BLOCK_SIZE * Sokoban.SCALE, Sokoban.BLOCK_SIZE * Sokoban.SCALE), new(0, 0), 0, Color.White);
    }
}


static class DirectionMethods
{
    public static int GetX(this Direction dir)
    {
        if (dir == Direction.Left) return -1;
        if (dir == Direction.Right) return 1;
        return 0;
    }    

    public static int GetY(this Direction dir)
    {
        if (dir == Direction.Down) return 1;
        if (dir == Direction.Up) return -1;
        return 0;
    }
}

public class Worker : ICloneable
{
    static int TEXTURE_POS = 5;
    public int x;
    public int y;

    public enum Direction
    {
        Up,
        Left,
        Down,
        Right,
    }
   
    public static readonly Direction[] directions = { Direction.Up, Direction.Left, Direction.Down, Direction.Right };

    public Worker(int _x, int _y)
    {
        x = _x;
        y = _y;
    }

    public void Draw(int mapX, int mapY)
    {
        Raylib.DrawTexturePro(
            Sokoban.texture,
            new Rectangle(
                TEXTURE_POS * Sokoban.BLOCK_SIZE,
                0,
                Sokoban.BLOCK_SIZE,
                Sokoban.BLOCK_SIZE
            ),
            new Rectangle(
                (x * Sokoban.BLOCK_SIZE + mapX) * Sokoban.SCALE,
                (y * Sokoban.BLOCK_SIZE + mapY) * Sokoban.SCALE,
                Sokoban.BLOCK_SIZE * Sokoban.SCALE,
                Sokoban.BLOCK_SIZE * Sokoban.SCALE
            ),
            new(0, 0),
            0,
            Color.White
        );
    }

    public static void DrawStatic(int x, int y)
    {
        Raylib.DrawTexturePro(Sokoban.texture, new Rectangle(TEXTURE_POS * Sokoban.BLOCK_SIZE, 0, Sokoban.BLOCK_SIZE, Sokoban.BLOCK_SIZE), new(x, y, Sokoban.BLOCK_SIZE * Sokoban.SCALE, Sokoban.BLOCK_SIZE * Sokoban.SCALE), new(0, 0), 0, Color.White);
    }

    public void Move(Direction direction, (byte x, byte y)[] boxes)
    {
        switch (direction)
        {
        case Direction.Up:
            Up(boxes);
            break;
        case Direction.Left:
            Left(boxes);
            break;
        case Direction.Down:
            Down(boxes);
            break;
        case Direction.Right:
            Right(boxes);
            break;
        }
    }

    public void Up((byte x, byte y)[] boxes)
    {
        y -= 1;
        if (Sokoban.map.Stepped(this, boxes))
        {
            if (Sokoban.map.CanMoveBox(this, Worker.Direction.Up, boxes))
            {
                Sokoban.map.MoveBox(this, Worker.Direction.Up, boxes);
            }
            else
            {
                y += 1;
            }
        }
    }

    public void Left((byte x, byte y)[] boxes)
    {
        x -= 1;
        if (Sokoban.map.Stepped(this, boxes))
        {
            if (Sokoban.map.CanMoveBox(this, Worker.Direction.Left, boxes))
            {
                Sokoban.map.MoveBox(this, Worker.Direction.Left, boxes);
            }
            else
            {
                x += 1;
            }
        }
    }

    public void Down((byte x, byte y)[] boxes)
    {
        y += 1;
        if (Sokoban.map.Stepped(this, boxes))
        {
            if (Sokoban.map.CanMoveBox(this, Worker.Direction.Down, boxes))
            {
                Sokoban.map.MoveBox(this, Worker.Direction.Down, boxes);
            }
            else
            {
                y -= 1;
            }
        }
    }

    public void Right((byte x, byte y)[] boxes)
    {
        x += 1;
        if (Sokoban.map.Stepped(this, boxes))
        {
            if (Sokoban.map.CanMoveBox(this, Worker.Direction.Right, boxes))
            {
                Sokoban.map.MoveBox(this, Worker.Direction.Right, boxes);
            }
            else
            {
                x -= 1;
            }
        }
    }

    public object Clone()
    {
        return this.MemberwiseClone();
    }
}

class Animator
{
    private static bool animating = false;
    private static double lastFrameTime;

    public static bool Animating
    {
        get {
            return animating;
        }
    }
    public static void PlayOrPause()
    {
        if (animating)
        {
            Pause();
        }
        else
        {
            Play();
        }
    }

    public static void Play()
    {
        animating = true;
        if (Sokoban.mode == Sokoban.Mode.Replay)
        {
            Raylib.SetWindowTitle($"⏵ Режим воспроизведения (x{Sokoban.playbackSpeed}) - " + Sokoban.searchMethod);
        }
        lastFrameTime = Raylib.GetTime();
        if (Sokoban.LastState())
        {
            Sokoban.SwitchToFirstState();
        }
    }

    public static void Pause()
    {
        animating = false;
        if (Sokoban.mode == Sokoban.Mode.Replay)
        {
            Raylib.SetWindowTitle($"⏸ Режим воспроизведения (x{Sokoban.playbackSpeed}) - " + Sokoban.searchMethod);
        }
    }

    public static void Animate()
    {
        if (Sokoban.LastState() || !animating)
        {
            animating = false;
            return;
        }
        if (Raylib.GetTime() - lastFrameTime >= Sokoban.ANIMATION_DELAY)
        {
            lastFrameTime = Raylib.GetTime();
            Sokoban.NextState();
        }

    }
}
