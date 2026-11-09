using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FootballGame.Common;
namespace FootballGame.TransformOperations.Interfaces
{
    public interface IPlayerStateTransition
    {
        PlayerState DeterminePlayerStateTransition();
    }
}
