namespace patrikpusztai_snakegame;


public sealed class SnakeGameException : Exception
{
    public SnakeGameException(string message)
        : base(message)
    {
    }
}