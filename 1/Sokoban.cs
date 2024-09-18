using Raylib_cs;
using System.Net;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text;

class Sokoban
{
    public const string TEXTURE = "../../../assets.png";
    public const int WIDTH = 800;
    public const int HEIGHT = 800;
    public const int SCALE = 2;
	public const int BLOCK_SIZE = 32 * SCALE;
	public const double ANIMATION_DELAY = 0.5;

    public static Texture2D texture;
	public static Map map;
	public static Worker worker = new Worker(0, 0);
	public static State? baseState = null;
	public static List<State>? states = null;
	public static int currStateIdx = 0;

	private static Searcher breadthSearcher = new Searcher(Searcher.Type.Breadth);
	private static Searcher depthSearcher = new Searcher(Searcher.Type.Depth);

	public enum Block: int
	{
		Floor = 0,
		Wall,
		Box,
		Mark,
		BoxOnMark,
		Player,
		Empty = 9,
	};

    static void Main()
    {
		Init();

        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(0x18, 0x18, 0x18, 0xff));

            map.Draw();

			if (Raylib.IsFileDropped())
			{
				var files = Raylib.GetDroppedFiles();
				if (files.Length == 1)
				{
					LoadMap(files[0]);
				}
				else
				{
					Raylib.SetWindowTitle("Только один файл можно загрузить за раз");
				}
			}
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
			if (Raylib.IsKeyPressed(KeyboardKey.One) && !Animator.Animating())
			{
				Raylib.SetWindowTitle("Осуществляется поиск в ширину");
				currStateIdx = 0;
                map = baseState.map;
                worker = baseState.worker;
                states = breadthSearcher.Search();
				Raylib.SetWindowTitle("Поиск в ширину завершён");
				            }
            if (Raylib.IsKeyPressed(KeyboardKey.Two) && !Animator.Animating())
			{
				Raylib.SetWindowTitle("Осуществляется поиск в глубину");
                currStateIdx = 0;
                map = baseState.map;
				worker = baseState.worker;
                states = depthSearcher.Search();
                Raylib.SetWindowTitle("Поиск в глубину завершён");
            }
            if (Raylib.IsKeyPressed(KeyboardKey.Space))
			{
				Animator.PlayOrPause();
                Raylib.SetWindowTitle("Воспроизведение пути");
            }
			if (Raylib.IsKeyPressed(KeyboardKey.R))
			{
				InitMap();
			}

            if (map.Complete())
			{
				Raylib.SetWindowTitle("Игра пройдена");
			}

			Animator.Animate();

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }

    static void Init()
	{
        Raylib.InitWindow(WIDTH, HEIGHT, "СИИ 1 лаба - игра Сокобан");

        Image assetImage = Raylib.LoadImage(TEXTURE);
		unsafe
		{
			Raylib.ImageResizeNN(&assetImage, assetImage.Width*SCALE, assetImage.Height*SCALE);
		}
		texture = Raylib.LoadTextureFromImage(assetImage);
		Raylib.UnloadImage(assetImage);

		LoadMap(@"../../../levels/level2.txt");
	}

	public static void LoadMap(string file)
	{
        map = new Map();
        map.x = BLOCK_SIZE;
		map.y = BLOCK_SIZE;
		map.filepath = file;

        string[] lines = File.ReadAllLines(file);
        int[,] result = new int[lines.Length, lines[0].Length];
        for (int i = 0; i < lines.Length; i++)
        {
            var cols = lines[i].ToCharArray();
            for (int j = 0; j < cols.Length; j++)
                result[i, j] = cols[j] - '0';
        }
        map.Load(result);
    }

    public static void InitMap()
    {
		if (map.filepath is not null)
		{
			LoadMap(map.filepath);
		}
    }

    public static void SwitchToFirstState()
	{
		currStateIdx = -1;
		NextState();
	}

	public static void NextState()
	{
		if (states == null || states.Count == 0)
		{
			return;
		}
		currStateIdx += 1;
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
	public string? filepath;

	public void Load(int[,] _map)
	{
		map = _map;
		for (int row = 0; row < GetRowsNum(); row++)
		{
			for (int col = 0; col < GetColsNum(); col++)
			{
				if (GetCell(row, col) == (int)Sokoban.Block.Player)
				{
					Sokoban.worker = new Worker(col, row);
					SetCell(row, col, (int)Sokoban.Block.Floor);
					break;
				}
			}
		}
		Sokoban.baseState = new State((Map)this.Clone(), (Worker)Sokoban.worker.Clone());
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
				_x += Sokoban.BLOCK_SIZE;
			}
			_y += Sokoban.BLOCK_SIZE;
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
		map[row, col] = val;
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
			worker.x == map.GetLength(0)-1 || worker.y == map.GetLength(1)-1 ||
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
		Map m = new Map();
		m.map = (int[,])map.Clone();
		m.x = x;
		m.y = y;
        return m;
    }
}

class Box
{
	static Texture2D texture;
	static int TEXTURE_POS = 0;

	public static void Draw(int x, int y)
	{
		Raylib.DrawTextureRec(Sokoban.texture, new Rectangle(TEXTURE_POS * Sokoban.BLOCK_SIZE, 0, Sokoban.BLOCK_SIZE, Sokoban.BLOCK_SIZE), new(x, y), Color.White);
	}
}

class BoxOnMark
{
	static Texture2D texture;
	static int TEXTURE_POS = 1;

	public static void Draw(int x, int y)
	{
		Raylib.DrawTextureRec(Sokoban.texture, new Rectangle(TEXTURE_POS * Sokoban.BLOCK_SIZE, 0, Sokoban.BLOCK_SIZE, Sokoban.BLOCK_SIZE), new(x, y), Color.White);
	}
}

class Floor
{
	static int TEXTURE_POS = 3;

	public static void Draw(int x, int y)
	{
		Raylib.DrawTextureRec(Sokoban.texture, new Rectangle(TEXTURE_POS * Sokoban.BLOCK_SIZE, 0, Sokoban.BLOCK_SIZE, Sokoban.BLOCK_SIZE), new(x, y), Color.White);
	}
}

class Mark
{
	static int TEXTURE_POS = 2;

	public static void Draw(int x, int y)
	{
		Raylib.DrawTextureRec(Sokoban.texture, new Rectangle(TEXTURE_POS * Sokoban.BLOCK_SIZE, 0, Sokoban.BLOCK_SIZE, Sokoban.BLOCK_SIZE), new(x, y), Color.White);
	}
}

class Wall
{
	static int TEXTURE_POS = 4;

	public static void Draw(int x, int y)
	{
		Raylib.DrawTextureRec(Sokoban.texture, new Rectangle(TEXTURE_POS * Sokoban.BLOCK_SIZE, 0, Sokoban.BLOCK_SIZE, Sokoban.BLOCK_SIZE), new(x, y), Color.White);
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
		Raylib.DrawTextureRec(
			Sokoban.texture,
			new Rectangle(
				TEXTURE_POS * Sokoban.BLOCK_SIZE,
				0, 
				Sokoban.BLOCK_SIZE, 
				Sokoban.BLOCK_SIZE
			), 
			new(
				x * Sokoban.BLOCK_SIZE+mapX,
				y * Sokoban.BLOCK_SIZE+mapY
			),
			Color.White
		);
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

	public static void PlayOrPause()
    {
		if (animating)
		{
			animating = false;
		}
		else
		{
			animating = true;
			lastFrameTime = Raylib.GetTime();
			if (Sokoban.LastState())
			{
				Sokoban.SwitchToFirstState();
			}
		}
	}

	public static void Stop()
	{
		animating = false;
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

	public static bool Animating()
	{
		return animating;
	}
}
