using System;

public interface IGameplayService : IDisposable
{
    GameplayInfo Info { get; }
}
