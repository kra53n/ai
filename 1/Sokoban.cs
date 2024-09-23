using Raylib_cs;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
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
    public static Worker worker = new Worker(0, 0);
    public static State? baseState = null;
    public static List<State>? states = null;
    public static int currStateIdx = 0;

    private static Action ControlsProcessor = GameControlsProcessor;
    public static string searchMethod = "";

    public enum Block : int
    {
        Floor = 0,
        Wall,
        Box,
        Mark,
        BoxOnMark,
        Worker,
        Empty = 9,
    };

    public enum Mode
    {
        Game,
        Edit,
        Replay
    }

    public static void Main()
    {
        Init();

        Raylib.SetTargetFPS(60);

        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(0x18, 0x18, 0x18, 0xff));

            switch (mode)
            {
                case Mode.Replay:
                case Mode.Game:
                    map.Draw();
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
        if (Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            Animator.PlayOrPause();
        }
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
        if (Raylib.IsKeyPressed(KeyboardKey.W) || Raylib.IsKeyPressed(KeyboardKey.Up))
        {
            worker.Up(map);
        }
        if (Raylib.IsKeyPressed(KeyboardKey.A) || Raylib.IsKeyPressed(KeyboardKey.Left))
        {
            worker.Left(map);
        }
        if (Raylib.IsKeyPressed(KeyboardKey.S) || Raylib.IsKeyPressed(KeyboardKey.Down))
        {
            worker.Down(map);
        }
        if (Raylib.IsKeyPressed(KeyboardKey.D) || Raylib.IsKeyPressed(KeyboardKey.Right))
        {
            worker.Right(map);
        }

        if (Raylib.IsKeyPressed(KeyboardKey.One))
        {
            ProcessSearch("поиск в ширину", new Searcher(Searcher.Type.Breadth));
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Two))
        {
            ProcessSearch("поиск в глубину", new Searcher(Searcher.Type.Depth));
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Three))
        {
            ProcessSearch("поиск в глубину с итеративным углеблением", new DepthFirstSearch());
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Four))
        {
            ProcessSearch("двунаправленный поиск", new BidirectionalSearch());
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            ToggleReplayGameMode();
        }

        if (map.Complete())
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

    public static void GlobalControlsProcessor()
    {
        if (Raylib.IsKeyDown(KeyboardKey.LeftControl))
        {
            if (Raylib.IsKeyPressed(KeyboardKey.R))
            {
                ToggleReplayGameMode();
            }
            return;
        }
        if (Raylib.IsKeyPressed(KeyboardKey.E))
        {
            mode = Mode.Edit;
            SCALE = 1;
        }
        if (Raylib.IsKeyPressed(KeyboardKey.R))
        {
            Animator.Pause();
            SwitchToFirstState();
            map = (Map)baseState.map.Clone();
            worker = (Worker)baseState.worker.Clone();
            return;
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
                map.Load(LoadMapContentFromFile(files[0]));
                Rescale();
            }
            else
            {
                Raylib.SetWindowTitle("Только один файл можно загрузить за раз");
            }
        }

        ControlsProcessor();
        GlobalControlsProcessor();
    }

    public static void Init()
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
        map.Load(new int[,] {
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

    public static int[,] LoadMapContentFromFile(string file)
    {
        string[] lines = File.ReadAllLines(file);
        int[,] content = new int[lines.Length, lines[0].Length];
        for (int i = 0; i < lines.Length; i++)
        {
            var cols = lines[i].ToCharArray();
            for (int j = 0; j < cols.Length; j++)
                content[i, j] = cols[j] - '0';
        }
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
        map = states[currStateIdx].map;
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
        map = states[currStateIdx].map;
        worker = states[currStateIdx].worker;
    }

    public static void PrevState()
    {
        if (states == null || states.Count == 0)
        {
            return;
        }
        currStateIdx = Math.Max(currStateIdx - 1, 0);
        map = states[currStateIdx].map;
        worker = states[currStateIdx].worker;
    }

    public static bool LastState()
    {
        return states != null && states.Count > 0 && currStateIdx == states.Count - 1;
    }
}

class Map : ICloneable
{
    public int[,]? map;
    public int x;
    public int y;
    
    public Map(int _x, int _y)
    {
        x = _x;
        y = _y;
    }
    public void Load(int[,] _map)
    {
        map = _map;
        for (int row = 0; row < GetRowsNum(); row++)
        {
            for (int col = 0; col < GetColsNum(); col++)
            {
                if (GetCell(row, col) == (int)Sokoban.Block.Worker)
                {
                    Sokoban.worker = new Worker(col, row);
                    SetCell(row, col, (int)Sokoban.Block.Floor);
                    break;
                }
            }
        }
        Sokoban.baseState = new State((Map)this.Clone(), (Worker)Sokoban.worker.Clone());
    }

    public IEnumerable<Tuple<int, int>> FindBlocks(Sokoban.Block block)
    {
        for (int row = 0; row < GetRowsNum(); row++)
        {
            for (int col = 0; col < GetColsNum(); col++)
            {
                if (map[row, col] == (int)block)
                {
                    yield return new(col, row);
                }
            }
        }
    }

    public void Draw()
    {
        int _x = x;
        int _y = y;
        for (int row = 0; row < map.GetLength(0); row++)
        {
            for (int col = 0; col < map.GetLength(1); col++)
            {
                switch (map[row, col])
                {
                case (int)Sokoban.Block.Floor:
                    Floor.Draw(_x, _y);
                    break;
                case (int)Sokoban.Block.Wall:
                    Wall.Draw(_x, _y);
                    break;
                case (int)Sokoban.Block.Box:
                    Box.Draw(_x, _y);
                    break;
                case (int)Sokoban.Block.Mark:
                    Mark.Draw(_x, _y);
                    break;
                case (int)Sokoban.Block.BoxOnMark:
                    BoxOnMark.Draw(_x, _y);
                    break;
                }
                _x += (int)(Sokoban.BLOCK_SIZE * Sokoban.SCALE);
            }
            _y += (int)(Sokoban.BLOCK_SIZE * Sokoban.SCALE);
            _x = x;
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

    public int GetCell(int row, int col)
    {
        return map[row, col];
    }

    public void SetCell(int row, int col, int val)
    {
        if (val == (int)Sokoban.Block.Box && map[row, col] == (int)Sokoban.Block.Mark)
        {
            map[row, col] = (int)Sokoban.Block.BoxOnMark;
        } 
        else
        {
            map[row, col] = val;
        }
    }
    public bool Complete()
    {
        for (int row = 0; row < GetRowsNum(); row++)
        {
            for (int col = 0; col < GetColsNum(); col++)
            {
                if (GetCell(row, col) == (int)Sokoban.Block.Mark)
                {
                    return false;
                }
            }
        }
        return true;
    }

    // Worker Stepped if he is on Wall, Box or BoxOnMark
    public bool Stepped(in Worker worker)
    {
        return !(map[worker.y, worker.x] == (int)Sokoban.Block.Floor || map[worker.y, worker.x] == (int)Sokoban.Block.Mark);
    }

    public bool CanMoveBox(in Worker worker, Worker.Direction dir)
    {
        if (
            worker.x == 0 || worker.y == 0 ||
            worker.x == map.GetLength(1)-1 || worker.y == map.GetLength(0)-1 ||
            GetCell(worker.y, worker.x) == (int)Sokoban.Block.Wall
        )
        {
            return false;
        }
        int val;
        switch (dir)
        {
        case Worker.Direction.Up:
            val = map[worker.y-1, worker.x];
            break;
        case Worker.Direction.Left:
            val = map[worker.y, worker.x-1];
            break; 
        case Worker.Direction.Down:
            val = map[worker.y+1, worker.x];
            break;
        case Worker.Direction.Right:
            val = map[worker.y, worker.x+1];
            break;
        default:
            return false;
        }
        return val == (int)Sokoban.Block.Floor || val == (int)Sokoban.Block.Mark;
    }

    public void MoveBox(in Worker worker, Worker.Direction dir)
    {
        int val = 0;
        val = (int)Sokoban.Block.Box;
        switch (map[worker.y, worker.x])
        {
            case (int)Sokoban.Block.Box:
                map[worker.y, worker.x] = (int)Sokoban.Block.Floor;
                val = (int)Sokoban.Block.Box;
                break;
            case (int)Sokoban.Block.BoxOnMark:
                map[worker.y, worker.x] = (int)Sokoban.Block.Mark;
                val = (int)Sokoban.Block.BoxOnMark;
                break;
        }
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

        switch (GetCell(y, x))
        {
        case (int)Sokoban.Block.Floor:
            SetCell(y, x, (int)Sokoban.Block.Box);
            break;
        case (int)Sokoban.Block.Mark:
            SetCell(y, x, (int)Sokoban.Block.BoxOnMark); 
            break;
        }
    }

    public object Clone()
    {
        if (map is null)
        {
            throw new NoNullAllowedException("map is null");
        }
        Map m = new Map(x, y);
        m.map = (int[,])map.Clone();
        return m;
    }
}

class Box
{
    static Texture2D texture;
    static int TEXTURE_POS = 0;

    public static void Draw(int x, int y)
    {
        Raylib.DrawTexturePro(Sokoban.texture, new Rectangle(TEXTURE_POS * Sokoban.BLOCK_SIZE, 0, Sokoban.BLOCK_SIZE, Sokoban.BLOCK_SIZE), new(x, y, Sokoban.BLOCK_SIZE * Sokoban.SCALE, Sokoban.BLOCK_SIZE * Sokoban.SCALE), new(0, 0), 0, Color.White);
    }
}

class BoxOnMark
{
    static Texture2D texture;
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

class Worker : ICloneable
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

    public void Move(Map map, Direction direction)
    {
        switch (direction)
        {
        case Direction.Up:
            Up(map);
            break;
        case Direction.Left:
            Left(map);
            break;
        case Direction.Down:
            Down(map);
            break;
        case Direction.Right:
            Right(map);
            break;
        }
    }

    public void Up(Map map)
    {
        y -= 1;
        if (map.Stepped(this))
        {
            if (map.CanMoveBox(this, Worker.Direction.Up))
            {
                map.MoveBox(this, Worker.Direction.Up);
            }
            else
            {
                y += 1;
            }
        }
    }

    public void Left(Map map)
    {
        x -= 1;
        if (map.Stepped(this))
        {
            if (map.CanMoveBox(this, Worker.Direction.Left))
            {
                map.MoveBox(this, Worker.Direction.Left);
            }
            else
            {
                x += 1;
            }
        }
    }

    public void Down(Map map)
    {
        y += 1;
        if (map.Stepped(this))
        {
            if (map.CanMoveBox(this, Worker.Direction.Down))
            {
                map.MoveBox(this, Worker.Direction.Down);
            }
            else
            {
                y -= 1;
            }
        }
    }

    public void Right(Map map)
    {
        x += 1;
        if (map.Stepped(this))
        {
            if (map.CanMoveBox(this, Worker.Direction.Right))
            {
                map.MoveBox(this, Worker.Direction.Right);
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
        Raylib.SetWindowTitle($"⏵ Режим воспроизведения (x{Sokoban.playbackSpeed}) - " + Sokoban.searchMethod);
        lastFrameTime = Raylib.GetTime();
        if (Sokoban.LastState())
        {
            Sokoban.SwitchToFirstState();
        }
    }

    public static void Pause()
    {
        animating = false;
        Raylib.SetWindowTitle($"⏸ Режим воспроизведения (x{Sokoban.playbackSpeed}) - " + Sokoban.searchMethod);
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
