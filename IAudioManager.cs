namespace patrikpusztai_snakegame;
public interface IAudioManager : IDisposable
{
    void PlayApple();
    void PlayPear();
    void PlayBanana();
    void PlayGameOver();
}