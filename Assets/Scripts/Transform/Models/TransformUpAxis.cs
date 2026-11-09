using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using FootballGame.Common;
using FootballGame.TransformOperations.Interfaces;
using FootballGame.TransformOperations.Rotation.Validators;
using FootballGame.TransformOperations.Models;
namespace FootballGame.TransformOperations.Models {
    public class TransformUpAxis : TransformAxis
    {
        public TransformUpAxis(TransformViewModel viewModel) : base(viewModel) { }
        public override List<KeyValuePair<string, bool>> RotateTowardsTarget()
        {
            List<AxisRotationValidation> rotationValidators = new List<AxisRotationValidation> {
                new AxisRotationValidation()
                {
                    axisRotationValidator = new AxisValidation {
                        sourceAxis = Axis.up,
                        conditionToUse = true,
                        targetVector = base.transformViewModel.GetTargetDirection()
                    },
                    isFullRotation = false,
                    rotationAmount =  base.transformViewModel.GetRotationPerFrame(),
                    sourceTransform = base.transformViewModel.GetSourceTransform(),
                    rotationType = RotationType.useGlobalAndLocalAxes,
                    axisOfRotation = Vector3.zero, //ONLY ROTATIONTYPE.SINGLEAXIS WILL SPECIFY AN AXIS OF ROTATION
                },
                //Twist Right or Inverse Right to Its Target Direction
                new AxisRotationValidation()
                {
                    axisRotationValidator = new AxisValidation {
                        sourceAxis = Axis.up,
                        conditionToUse = true,
                        targetVector = new Vector3(0, 0, 0)
                    },
                    isFullRotation = false,
                    rotationAmount = base.transformViewModel.GetRotationPerFrame(),
                    sourceTransform = base.transformViewModel.GetSourceTransform().Find(base.transformViewModel.GetTwistTransformName()),
                    rotationType = RotationType.useSingleAxis,
                    //FOR SINGLE AXIS SPECIFY AXIS OF ROTATION
                    axisOfRotation = base.transformViewModel.GetTwistAxisOfRotation() == Axis.right ? base.transformViewModel.GetSourceTransform().Find(base.transformViewModel.GetTwistTransformName()).right
                    : base.transformViewModel.GetTwistAxisOfRotation() == Axis.inverseRight ? -base.transformViewModel.GetSourceTransform().Find(base.transformViewModel.GetTwistTransformName()).right
                    : new Vector3(0, 0, 0) 
                },
                new AxisRotationValidation()
                {
                    axisRotationValidator = new AxisValidation {
                        sourceAxis = Axis.none, //THIS MEANS THIS IS A SNAP ROTATION OR SOME OTHER TYPE OF ROTATION THAT DOESN'T TAKE A VECTOR AXIS AS INPUT
                        conditionToUse = true,
                        targetVector = base.transformViewModel.GetTargetDirection()
                    },
                    isFullRotation = true,
                    rotationAmount = base.transformViewModel.GetRotationPerFrame(),
                    sourceTransform = base.transformViewModel.GetHandTransform(),
                    rotationType = RotationType.snap,
                    axisOfRotation = Vector3.zero //ONLY ROTATIONTYPE.SINGLEAXIS WILL SPECIFY AN AXIS OF ROTATION
                }
            };
            return AlignAxis(rotationValidators);
        }
    }
}

