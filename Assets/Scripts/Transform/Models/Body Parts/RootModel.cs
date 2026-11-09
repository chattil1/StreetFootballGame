using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using FootballGame.Common;
using FootballGame.TransformOperations.Rotation.Validators;
namespace FootballGame.TransformOperations.Models {
    public class RootModel : BodyPartModel
    {
        public RootModel(TransformViewModel viewModel) : base(viewModel) { }

        #region Methods Overridden
        public override List<KeyValuePair<string, bool>> RotateTowardsTarget()
        {
            TransformAxis transformAxis = GetTransformAxis(transformViewModel.GetSourceAxis());
            SetTarget();
            List<KeyValuePair<string, bool>> rotationOutcome = transformAxis.RotateTowardsTarget();
            return rotationOutcome;
        }
        #endregion

        public void SetTarget()
        {
            base.transformViewModel.SetAxisRotationValidators(GetRotationValidatorsToPointAtTarget());
        }
        public override List<AxisRotationValidation> GetRotationValidatorsToPointAtTarget()
        {
            List<AxisRotationValidation> rotationValidators = new List<AxisRotationValidation> {
            new AxisRotationValidation()
            {
                axisRotationValidator = new AxisValidation {
                    sourceAxis = base.transformViewModel.GetSourceAxis(),
                    conditionToUse = true,
                    targetVector = base.transformViewModel.GetTargetDirection()
                },
                isFullRotation = false,
                rotationAmount = base.transformViewModel.GetRotationPerFrame(),
                sourceTransform = base.transformViewModel.GetSourceTransform(),
                rotationType = RotationType.useSingleAxis,
                axisOfRotation = base.transformViewModel.GetTwistAxisOfRotationVector(), //ONLY ROTATIONTYPE.SINGLEAXIS WILL SPECIFY AN AXIS OF ROTATION
            }
            };
            //rotationValidators.AddRange(GetTwistAxisRotationValidators(base.transformViewModel.GetSourceAxis(), base.transformViewModel.GetTwistAxisOfRotation(), base.transformViewModel.GetAxisToTwist()));
            return rotationValidators;
        }
    }
}

