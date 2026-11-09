using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FootballGame.Common;
namespace FootballGame.TransformOperations.Rotation.Validators {
    public class AxisRotationValidation
    {
        public Transform sourceTransform { get; set; }
        public AxisValidation axisRotationValidator { get; set; }
        public float rotationAmount { get; set; }
        public bool isFullRotation { get; set; }
        public RotationType rotationType { get; set; }
        public Vector3 axisOfRotation { get; set; }
    }
}






