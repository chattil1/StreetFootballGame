using System;
namespace FootballGame.Common
{
    public enum PlayerState
    {
        none,
        idle,
        running,
        aimingForwardDirection,
        attemptingCatch,
        catchAndRun
    }
}
