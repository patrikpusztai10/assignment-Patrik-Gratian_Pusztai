namespace patrikpusztai_snakegame;

public static class Program
{
    public static void Main()
    {
        using var game = new SnakeGame();
        game.Run();
    }
}