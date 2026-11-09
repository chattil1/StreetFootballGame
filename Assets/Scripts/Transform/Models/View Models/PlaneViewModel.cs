using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FootballGame.Common;
namespace FootballGame.TransformOperations.Rotation.Validators
{
    public class PlaneViewModel
    {
        public bool gonePastForwardTopLimit { get; set; }
        public bool gonePastRightTopLimit { get; set; }
        public float horizontalRotation { get; set; }
        public float verticalRotation { get; set; }
        public float verticalRotationRight { get; set; }
        public float counterClockwiseSign { get; set; }
        public float currentLeftRotation { get; set; }
        public float currentRightRotation { get; set; }
        public float currentUpRotation { get; set; }
        public float currentDownRotation { get; set; }
        public float maxHorizontalRotationClockwise { get; set; }
        public float maxHorizontalRotationCounterClockwise { get; set; }
        public float maxVerticalRotationClockwise { get; set; }
        public float maxVerticalRotationCounterClockwise { get; set; }
        public float maxVerticalRotationClockwiseRight { get; set; }
        public float maxVerticalRotationCounterClockwiseRight { get; set; }
        public Vector3 plane { get; set; }
        public Vector3 referenceVectorDirection { get; set; }
        public Vector3 targetDirection { get; set; }
    }
}