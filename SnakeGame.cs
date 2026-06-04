namespace patrikpusztai_snakegame;

using Silk.NET.SDL;
using System.Diagnostics;


public sealed class SnakeGame : ISdlGame
{
    private const int windowWidth=600;
    private const int windowHeight=600;

    private const int cellSize=20;

    private const int gridW = windowWidth / cellSize;
    private const int gridH = windowHeight / cellSize;

    private readonly Random _random = new();

    private readonly Sdl _sdl;
    private readonly IntPtr _window;
    private readonly IntPtr _renderer;
    private readonly Stopwatch _timer = new();
    private readonly List<GridPosition> _snake = new();

    private Direction direction;
    private Direction nextMove;
    private GridPosition apple;

    private bool running = true;
    private bool game_over;
    private int score;

    private ulong frames;

    public SnakeGame()
    {
        _sdl = new Sdl(new SdlContext());

        if (_sdl.Init(Sdl.InitVideo |Sdl.InitEvents |Sdl.InitTimer) < 0)
        {
            throw new SnakeGameException("SDL initialization failed.");
        }

        unsafe
        {
            _window = (IntPtr)_sdl.CreateWindow(
                "Snake Game - Patrik Pusztai",
                Sdl.WindowposCentered,
                Sdl.WindowposCentered,
                windowWidth,
                windowHeight,
                (uint)WindowFlags.Shown);

            if (_window == IntPtr.Zero)
            {
                throw new SnakeGameException("Window creation failed.");
            }

            _renderer = (IntPtr)_sdl.CreateRenderer((Window*)_window, -1,(uint)RendererFlags.Accelerated);

            if (_renderer == IntPtr.Zero)
            {
                throw new SnakeGameException("Renderer creation failed.");
            }

            _sdl.RenderSetVSync((Renderer*)_renderer, 1);
        }

        ResetGame();
    }

    public void Run()
    {
        var eventData= new Event();
        const double tickRate=0.12;
        double accumulator=0;
        _timer.Start();

        while (running)
        {
            while (_sdl.PollEvent(ref eventData) != 0)
            {
                HandleEvent(eventData);
            }
         
            var elapsed = _timer.Elapsed.TotalSeconds;
            _timer.Restart();

            accumulator += elapsed;

            while (accumulator >= tickRate)
            {
                accumulator-=tickRate;
                if (!game_over)
                {
                    Update();
                }
            }
            Render();
            frames++;
        }
    }

    private void HandleEvent(Event ev)
    {
        
        switch ((EventType)ev.Type)
        {
            case EventType.Quit:
                running = false;
                break;
            case EventType.Keydown:
                HandleKey((KeyCode)ev.Key.Keysym.Scancode);
                break;
        }
    }

    private void HandleKey(KeyCode key)
    {
        if (game_over)
        {
            if (key==KeyCode.Space)
            {
                _ = RestartAsync();
            }

            return;
        }

        nextMove = key switch
        {
            KeyCode.Up when direction != Direction.Down
                => Direction.Up,
            KeyCode.Down when direction != Direction.Up
                => Direction.Down,
            KeyCode.Left when direction != Direction.Right
                => Direction.Left,
            KeyCode.Right when direction != Direction.Left
                => Direction.Right,

            _ => nextMove
        };
    }

    private async Task RestartAsync()
    {
        await Task.Delay(250);
        ResetGame();
    }

    private void ResetGame()
    {
        _snake.Clear();

        //Valori intiale pentru sarpe
        _snake.Add(new GridPosition(20, 20));
        _snake.Add(new GridPosition(19, 20));
        _snake.Add(new GridPosition(18, 20));

        direction= Direction.Right;
        nextMove= Direction.Right;

        score=0;
        game_over = false;

        SpawnFood();
    }

    private void Update()
    {
        direction=nextMove;
        var head= _snake[0];

        head = direction switch
        {
            // Aici calculam noua pozitiie a sarpelui in functie de directie
            Direction.Up => head with { Y = head.Y - 1 },
            Direction.Down => head with { Y = head.Y + 1 },
            Direction.Left => head with { X = head.X - 1 },
            Direction.Right => head with { X = head.X + 1 },
            _ => head
        };

        if (head.X < 0 ||head.X >= gridW ||head.Y < 0 || head.Y >= gridH)
        {
            game_over = true;
            return;
        }

        if (_snake.Skip(1).Any(s => s == head))
        {
            game_over= true;
            return;
        }

        _snake.Insert(0, head);

        if (head == apple)
        {
            score++;
            SpawnFood();
        }
        else
        {
            _snake.RemoveAt(_snake.Count - 1);
        }
    }

    private void SpawnFood()
    {
        //Utilizand LINQ gasim o pozitie pe grid care nu e ocupata de sarpe, si punem acolo marul
        IEnumerable<GridPosition> freeCells =
            Enumerable.Range(0, gridW)
                .SelectMany( x => Enumerable.Range(0, gridH), (x, y) => new GridPosition(x, y))
                .Except(_snake);

        var cells=freeCells.ToList();
        if (cells.Count == 0)
        {
            throw new SnakeGameException("No valid food position.");
        }

        apple = cells[_random.Next(cells.Count)];
    }
    // AI-generated
    private unsafe void DrawApple(Renderer* renderer)
    {
        _sdl.SetRenderDrawColor(renderer, 255, 50, 50, 255);
        int x= apple.X * cellSize;
        int y= apple.Y * cellSize;
        for (int row = 0; row < cellSize; row++)
        {
            _sdl.RenderDrawLine(
                renderer,
                x,
                y + row,
                x + cellSize - 1,
                y + row);
        }
    }

    private unsafe void DrawSnake(Renderer* renderer)
    {
        _sdl.SetRenderDrawColor(renderer, 0, 220, 220, 255);

        foreach (var segment in _snake)
        {
            int x=segment.X * cellSize;
            int y=segment.Y * cellSize;

            for (int row = 0; row < cellSize; row++)
            {
                _sdl.RenderDrawLine(
                    renderer,
                    x,
                    y + row,
                    x + cellSize - 1,
                    y + row);
            }
        }
    }
    // end AI-generated

    private unsafe void Render()
    {
        var renderer = (Renderer*)_renderer;

        _sdl.SetRenderDrawColor(renderer, 40, 40, 40, 255);
        _sdl.RenderClear(renderer);

        DrawApple(renderer);
        DrawSnake(renderer);

        _sdl.RenderPresent(renderer);

    }


    
    public void Dispose()
    {
        unsafe
        {
            if (_renderer != IntPtr.Zero)
            {
                _sdl.DestroyRenderer((Renderer*)_renderer);
            }

            if (_window != IntPtr.Zero)
            {
                _sdl.DestroyWindow((Window*)_window);
            }
        }

        _sdl.Quit();
    }

    /**
    Possible future improvements:
    1. Adaugam si pere pe langa pere, care vor avea un efect special
    2. Punctele vor fi vizibile pe screen pentru user
    3. Adaugam efecte sonore si facem jocul mai atractiv vizual
    **/
}