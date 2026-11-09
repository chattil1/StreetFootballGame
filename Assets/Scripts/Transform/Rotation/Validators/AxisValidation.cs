using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FootballGame.Common;
namespace FootballGame.TransformOperations.Rotation.Validators
{
    public class AxisValidation
    {
        public Axis sourceAxis { get; set; }
        public bool conditionToUse { get; set; }
        public Vector3 targetVector { get; set; }
    }
}

