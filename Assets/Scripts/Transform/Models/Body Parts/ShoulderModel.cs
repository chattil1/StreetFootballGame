using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using FootballGame.Common;
using FootballGame.TransformOperations.Rotation.Validators;
namespace FootballGame.TransformOperations.Models
{
    public class ShoulderModel : BodyPartModel
    {
        public ShoulderModel(TransformViewModel viewModel) : base(viewModel) { }
        public override List<AxisRotationValidation> GetRotationValidatorsToPointAtTarget()
        {
            List<AxisRotationValidation> rotationValidators = new List<AxisRotationValidation> {
            new AxisRotationValidation()
            {
                axisRotationValidator = new AxisValidation {
                    sourceAxis = Axis.inverseRight,
                    conditionToUse = (base.transformViewModel.GetRootDirection().z <= 0.01f && base.transformViewModel.GetTargetDirection().y <= 0.01f),
                    targetVector = new Vector3(base.transformViewModel.GetTargetDirection().x, Mathf.Abs(base.transformViewModel.GetTargetDirection().y) <= Mathf.Abs(base.transformViewModel.GetTargetDirection().x) ? base.transformViewModel.GetTargetDirection().y : Mathf.Sign(base.transformViewModel.GetTargetDirection().y) * Mathf.Abs(base.transformViewModel.GetTargetDirection().x), base.transformViewModel.GetTargetDirection().y)
                },
                isFullRotation = false,
                rotationAmount = base.transformViewModel.GetRotationPerFrame(),
                sourceTransform = base.transformViewModel.GetSourceTransform(),
                rotationType = RotationType.useForwardAndUpAxes,
                axisOfRotation = Vector3.zero, //ONLY ROTATIONTYPE.SINGLEAXIS WILL SPECIFY AN AXIS OF ROTATION
            },
            new AxisRotationValidation()
            {
                axisRotationValidator = new AxisValidation {
                    sourceAxis = Axis.inverseRight,
                    conditionToUse = (base.transformViewModel.GetRootDirection().z <= 0.01f && base.transformViewModel.GetTargetDirection().y > 0.01f),
                    targetVector = base.transformViewModel.GetTargetDirection()
                },
                isFullRotation = false,
                rotationAmount = base.transformViewModel.GetRotationPerFrame(),
                sourceTransform = base.transformViewModel.GetSourceTransform(),
                rotationType = RotationType.useForwardAndUpAxes,
                axisOfRotation = Vector3.zero, //ONLY ROTATIONTYPE.SINGLEAXIS WILL SPECIFY AN AXIS OF ROTATION
            },
            new AxisRotationValidation()
            {
                axisRotationValidator = new AxisValidation {
                    sourceAxis = Axis.inverseRight,
                    conditionToUse = (base.transformViewModel.GetRootDirection().z > 0.01f && base.transformViewModel.GetTargetDirection().y <= 0.01f),
                    targetVector = new Vector3(base.transformViewModel.GetAnalogStickInput().x, Mathf.Abs(base.transformViewModel.GetAnalogStickInput().y), Mathf.Abs(base.transformViewModel.GetAnalogStickInput().x) <= 0.98f ? base.transformViewModel.GetAnalogStickInput().y : Mathf.Abs(base.transformViewModel.GetAnalogStickInput().x))
                },
                isFullRotation = false,
                rotationAmount = base.transformViewModel.GetRotationPerFrame(),
                sourceTransform = base.transformViewModel.GetSourceTransform(),
                rotationType = RotationType.useForwardAndUpAxes,
                axisOfRotation = Vector3.zero, //ONLY ROTATIONTYPE.SINGLEAXIS WILL SPECIFY AN AXIS OF ROTATION
            },
            new AxisRotationValidation()
            {
                axisRotationValidator = new AxisValidation {
                    sourceAxis = Axis.inverseRight,
                    conditionToUse = (base.transformViewModel.GetRootDirection().z > 0.01f && base.transformViewModel.GetTargetDirection().y > 0.01f),
                    targetVector = base.transformViewModel.GetTargetDirection()
                },
                isFullRotation = false,
                rotationAmount = base.transformViewModel.GetRotationPerFrame(),
                sourceTransform = base.transformViewModel.GetSourceTransform(),
                rotationType = RotationType.useForwardAndUpAxes,
                axisOfRotation = Vector3.zero, //ONLY ROTATIONTYPE.SINGLEAXIS WILL SPECIFY AN AXIS OF ROTATION
            }};
            return rotationValidators;
        }
    }
}

