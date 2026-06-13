namespace patrikpusztai_snakegame;
using Silk.NET.SDL;
using Silk.NET.Maths;
using System.Diagnostics;
using System.Media;


public sealed class SnakeGame : ISdlGame,IDisposable, IAudioManager
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
    private readonly List<FoodItem> _food = new();
    private const int MaxFoodItems = 2; 
    private const double PearSpawnChance = 0.25;  
    private const double BananaSpawnChance = 0.15;  

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
    
    public void PlayApple()
    {
        if (OperatingSystem.IsWindows())
        {
            SoundPlayer player=new SoundPlayer("assets/apple.wav");
            player.Play();
        }
    }
    public void PlayPear()
    {
        if (OperatingSystem.IsWindows())
        {
            SoundPlayer player=new SoundPlayer("assets/pear.wav");
            player.Play();
        }
    }
    public void PlayBanana()
    {
        if (OperatingSystem.IsWindows())
        {
            SoundPlayer player=new SoundPlayer("assets/banana.wav");
            player.Play();
        }
    }
    public void PlayGameOver()
    {
        if (OperatingSystem.IsWindows())
        {
            SoundPlayer player=new SoundPlayer("assets/gameover.wav");
            player.Play();
        }
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

        _food.Clear();
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
            PlayGameOver();
            return;
        }

        if (_snake.Skip(1).Any(s => s == head))
        {
            game_over= true;
            PlayGameOver();
            return;
        }

        _snake.Insert(0, head);

        var eaten= _food.FirstOrDefault(f => f.Position == head);
        if (eaten != default)
        {
            _food.Remove(eaten);
            int extraGrowth = 1;
            if (eaten.Type == FoodType.Apple)
            {
                PlayApple();
                score += 10;
            }
            else if(eaten.Type == FoodType.Pear)
            {
                PlayPear();
                score += 20;
                extraGrowth = 2;

            }
            else if (eaten.Type == FoodType.Banana)
            {
                PlayBanana();
                score += 30;  
                extraGrowth = 3;  
            }
          
            for (int i = 0; i < extraGrowth; i++)
                    _snake.Add(_snake[^1]);
            SpawnFood();
        }
        else
        {
            _snake.RemoveAt(_snake.Count - 1);
        }
    }

    private void SpawnFood()
    {
        var occupied = _snake.Concat(_food.Select(f => f.Position));
        var freeCells = Enumerable.Range(0, gridW)
            .SelectMany(x => Enumerable.Range(0, gridH), (x, y) => new GridPosition(x, y))
            .Except(occupied)
            .ToList();

        if (freeCells.Count == 0)
            throw new SnakeGameException("No valid food position.");

        var position = freeCells[_random.Next(freeCells.Count)];
        bool pearAlreadyPresent = _food.Any(f => f.Type == FoodType.Pear);

        FoodType type;

        if (!pearAlreadyPresent && _random.NextDouble() < PearSpawnChance)
        {
            type = FoodType.Pear;
        }
        else if (_random.NextDouble() < BananaSpawnChance)
        {
            type = FoodType.Banana;
        }
        else
        {
            type = FoodType.Apple;
        }

        _food.Add(new FoodItem(position, type));
    }
    //AI Generated
    private unsafe void DrawFood(Renderer* renderer)
    {
        foreach (var item in _food)
        {
            int x = item.Position.X * cellSize;
            int y = item.Position.Y * cellSize;
            int pad = 2;

            //Culori pentru fiecare fruct
            (byte r, byte g, byte b) = item.Type switch
            {
                FoodType.Apple => ((byte)220, (byte)40, (byte)40),
                FoodType.Pear => ((byte)90, (byte)200, (byte)60),
                FoodType.Banana => ((byte)240, (byte)220, (byte)40),
                _ => ((byte)255, (byte)255, (byte)255)
            };

            var rect = new Rectangle<int>(x + pad, y + pad, cellSize - pad * 2, cellSize - pad * 2);

            // Shadow
            _sdl.SetRenderDrawColor(renderer, 0, 0, 0, 60);
            var shadow = new Rectangle<int>(x + pad + 1, y + pad + 2, cellSize - pad * 2, cellSize - pad * 2);
            _sdl.RenderFillRect(renderer, &shadow);

            // Main body
            _sdl.SetRenderDrawColor(renderer, r, g, b, 255);
            _sdl.RenderFillRect(renderer, &rect);

            // Outline
            _sdl.SetRenderDrawColor(renderer, (byte)(r / 2), (byte)(g / 2), (byte)(b / 2), 255);
            _sdl.RenderDrawRect(renderer, &rect);

            // Highlight dot (top-left)
            _sdl.SetRenderDrawColor(renderer, 255, 255, 255, 180);
            var highlight = new Rectangle<int>(x + pad + 2, y + pad + 2, 3, 3);
            _sdl.RenderFillRect(renderer, &highlight);

            // Small stem for apple/pear
            if (item.Type != FoodType.Banana)
            {
                _sdl.SetRenderDrawColor(renderer, 120, 80, 40, 255);
                var stem = new Rectangle<int>(x + cellSize / 2 - 1, y, 2, pad + 1);
                _sdl.RenderFillRect(renderer, &stem);
            }
        }
    }
    //End AI Generated
    private unsafe void DrawSnake(Renderer* renderer)
    {
        int pad = 1;

        for (int i = 0; i < _snake.Count; i++)
        {
            var segment = _snake[i];
            int x = segment.X * cellSize;
            int y = segment.Y * cellSize;

            bool isHead = i == 0;

            var rect = new Rectangle<int>(x + pad, y + pad, cellSize - pad * 2, cellSize - pad * 2);

            if (isHead)
            {
                //Capul sarpelui va fi mai deschis la culoare
                _sdl.SetRenderDrawColor(renderer, 60, 255, 255, 255);
            }
        
            _sdl.RenderFillRect(renderer, &rect);

            //Contur
            _sdl.SetRenderDrawColor(renderer, 0, 100, 100, 255);
            _sdl.RenderDrawRect(renderer, &rect);

            
        }
    }
   
    private unsafe void Render()
    {
        var renderer = (Renderer*)_renderer;

        _sdl.SetRenderDrawColor(renderer, 40, 40, 40, 255);
        _sdl.RenderClear(renderer);

        DrawFood(renderer);
        DrawSnake(renderer);
         string title = game_over
        ? $"Snake Game - GAME OVER | Score: {score} | Press SPACE to restart"
        : $"Snake Game - Patrik Pusztai | Score: {score}";
    _sdl.SetWindowTitle((Window*)_window, title);

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

}