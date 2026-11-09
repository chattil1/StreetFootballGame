using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using FootballGame.Common;
using FootballGame.TransformOperations.Rotation.Validators;
namespace FootballGame.TransformOperations.Models {
    public class LeftArmModel : ArmModel
    {
        public LeftArmModel(TransformViewModel viewModel) : base(viewModel) {
            
        }
        #region Methods Overridden
        public override List<KeyValuePair<string, bool>> RotateTowardsTarget()
        {
            TransformAxis transformAxis = GetTransformAxis(transformViewModel.GetSourceAxis());
            SetTarget();
            List<KeyValuePair<string, bool>> rotationOutcome = transformAxis.RotateTowardsTarget();
            SetTwistRotationValidators();
            rotationOutcome = transformAxis.TwistTowardsTarget();
            return rotationOutcome;
        }
        public override PlaneViewModel RotateTowardsTargetWithCheck(Vector3 expectedTarget)
        {
            TransformAxis transformAxis = GetTransformAxis(transformViewModel.GetSourceAxis());
            SetTargetWithCheck();
            transformViewModel.SetPlaneDetails(transformAxis.RotateWithCheck(expectedTarget));
            SetTwistRotationValidators();
            List<KeyValuePair<string, bool>> rotationOutcome = transformAxis.TwistTowardsTarget();
            return transformViewModel.GetPlaneDetails();
        }
        #endregion

        public void SetTarget() {
            base.transformViewModel.SetAxisRotationValidators(GetRotationValidatorsToPointAtTarget());
        }
        public void SetTargetWithCheck()
        {
            base.transformViewModel.SetAxisRotationValidators(GetRotationValidatorsToPointAtTargetWithCheck());
        }
        public void SetTwistRotationValidators()
        {
            base.transformViewModel.SetAxisRotationValidators(GetTwistAxisRotationValidators(base.transformViewModel.GetSourceAxis(), base.transformViewModel.GetTwistAxisOfRotation(), base.transformViewModel.GetAxisToTwist()));
        }
        public override List<AxisRotationValidation> GetRotationValidatorsToPointAtTarget()
        {
            List<AxisRotationValidation> rotationValidators = new List<AxisRotationValidation> {
            new AxisRotationValidation()
            {
                axisRotationValidator = new AxisValidation {
                    sourceAxis = Axis.inverseRight,
                    conditionToUse = (base.transformViewModel.GetTargetDirection().y <= 0.01f),
                    targetVector = base.transformViewModel.GetTargetDirection()
                },
                isFullRotation = false,
                rotationAmount = base.transformViewModel.GetRotationPerFrame(),
                sourceTransform = base.transformViewModel.GetSourceTransform(),
                rotationType = RotationType.useGlobalAndLocalAxes,
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
                rotationType = RotationType.useGlobalAndLocalAxes,
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
                rotationType = RotationType.useGlobalAndLocalAxes,
                axisOfRotation = Vector3.zero, //ONLY ROTATIONTYPE.SINGLEAXIS WILL SPECIFY AN AXIS OF ROTATION
            } };
            /*new AxisRotationValidation()
            {
                axisRotationValidator = new AxisValidation {
                    sourceAxis = Axis.inverseRight,
                    conditionToUse = (base.transformViewModel.GetTargetDirection().y <= 0.01f),
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
                    conditionToUse = (base.transformViewModel.GetRootDirection().z > 0.01f && base.transformViewModel.GetTargetDirection().y > 0.01f),
                    targetVector = base.transformViewModel.GetTargetDirection()
                },
                isFullRotation = false,
                rotationAmount = base.transformViewModel.GetRotationPerFrame(),
                sourceTransform = base.transformViewModel.GetSourceTransform(),
                rotationType = RotationType.useForwardAndUpAxes,
                axisOfRotation = Vector3.zero, //ONLY ROTATIONTYPE.SINGLEAXIS WILL SPECIFY AN AXIS OF ROTATION
            }
            };*/
            //rotationValidators.AddRange(GetTwistAxisRotationValidators(base.transformViewModel.GetSourceAxis(), base.transformViewModel.GetTwistAxisOfRotation(), base.transformViewModel.GetAxisToTwist()));
            return rotationValidators;
        }

        public List<AxisRotationValidation> GetRotationValidatorsToPointAtTargetWithCheck()
        {
            List<AxisRotationValidation> rotationValidators = new List<AxisRotationValidation> {
            new AxisRotationValidation()
            {
                axisRotationValidator = new AxisValidation {
                    sourceAxis = Axis.inverseRight,
                    conditionToUse = true,
                    targetVector = base.transformViewModel.GetTargetDirection()
                },
                isFullRotation = false,
                rotationAmount = base.transformViewModel.GetRotationPerFrame(),
                sourceTransform = base.transformViewModel.GetSourceTransform(),
                rotationType = RotationType.useGlobalAndLocalAxes,
                axisOfRotation = Vector3.zero, //ONLY ROTATIONTYPE.SINGLEAXIS WILL SPECIFY AN AXIS OF ROTATION
            }};
            return rotationValidators;
        }
        public List<AxisRotationValidation> GetTwistAxisRotationValidators(Axis sourceAxis, Axis twistAxisOfRotation, Axis axisToTwist) {
            switch (sourceAxis) {
                case Axis.inverseRight:
                    return GetTwistRotationValidatorsInverseRight(twistAxisOfRotation, axisToTwist);
                case Axis.up:
                    return GetTwistRotationValidatorsUp(twistAxisOfRotation);
                case Axis.forward:
                    return GetTwistForwardAxisRotationValidators();
                default:
                    break;
            }
            return new List<AxisRotationValidation>();
        }

        public List<AxisRotationValidation> GetTwistRotationValidatorsInverseRight(Axis twistAxisOfRotation, Axis axisToTwist)
        {
            List<AxisRotationValidation> rotationValidators = new List<AxisRotationValidation>();
            switch (twistAxisOfRotation) {
                case Axis.inverseRight:
                    if (axisToTwist == Axis.forward) {
                        float zSign = Mathf.Sign(base.transformViewModel.GetDefaultTwistTarget().normalized.z);
                        rotationValidators = new List<AxisRotationValidation> {
                                                        /*new AxisRotationValidation()
                            { //Rotating X Axis (TWISTING ARM)
                                axisRotationValidator = new AxisValidation {
                                    sourceAxis = Axis.forward,
                                    conditionToUse = true,
                                    targetVector = base.transformViewModel.GetAnalogStickInput().y > 0.01f ? new Vector3(base.transformViewModel.GetDefaultTwistTarget().normalized.x,
                                        Mathf.Abs(base.transformViewModel.GetDefaultTwistTarget().normalized.x) < Mathf.Abs(base.transformViewModel.GetDefaultTwistTarget().normalized.y) ? Mathf.Abs(base.transformViewModel.GetDefaultTwistTarget().normalized.x) : base.transformViewModel.GetDefaultTwistTarget().normalized.y,
                                        Mathf.Abs(base.transformViewModel.GetDefaultTwistTarget().normalized.x) > Mathf.Abs(base.transformViewModel.GetDefaultTwistTarget().normalized.y) ? zSign * Mathf.Abs(base.transformViewModel.GetDefaultTwistTarget().normalized.x) : zSign * Mathf.Abs(base.transformViewModel.GetDefaultTwistTarget().normalized.y)) :
                                        new Vector3(0f,
                                        Mathf.Abs(base.transformViewModel.GetDefaultTwistTarget().normalized.x),
                                        Mathf.Abs(base.transformViewModel.GetDefaultTwistTarget().normalized.y))
                                },
                                isFullRotation = true,
                                rotationAmount = base.transformViewModel.GetRotationPerFrame(),
                                sourceTransform = base.transformViewModel.GetSourceTransform(),
                                rotationType = RotationType.useSingleAxis,
                                axisOfRotation = -base.transformViewModel.GetSourceTransform().right, //FOR SINGLE AXIS SPECIFY AXIS OF ROTATION
                            }*/
                            new AxisRotationValidation()
                            { //Rotating X Axis (TWISTING ARM)
                                axisRotationValidator = new AxisValidation {
                                    sourceAxis = Axis.forward,
                                    conditionToUse = true,
                                    targetVector = base.transformViewModel.GetDefaultTwistTarget().normalized
                                },
                                isFullRotation = true,
                                rotationAmount = base.transformViewModel.GetRotationPerFrame(),
                                sourceTransform = base.transformViewModel.GetSourceTransform(),
                                rotationType = RotationType.useSingleAxis,
                                axisOfRotation = -base.transformViewModel.GetSourceTransform().right, //FOR SINGLE AXIS SPECIFY AXIS OF ROTATION
                            }
                            /*new AxisRotationValidation()
                            { //Rotating X Axis (TWISTING ARM)
                                axisRotationValidator = new AxisValidation {
                                    sourceAxis = Axis.forward,
                                    conditionToUse = true,
                                    targetVector = base.transformViewModel.GetAnalogStickInput().y > 0.01f ? new Vector3(0f,
                                        Mathf.Abs(base.transformViewModel.GetDefaultTwistTarget().normalized.x) < Mathf.Abs(base.transformViewModel.GetDefaultTwistTarget().normalized.y) ? Mathf.Abs(base.transformViewModel.GetDefaultTwistTarget().normalized.x) : base.transformViewModel.GetDefaultTwistTarget().normalized.y,
                                        Mathf.Abs(base.transformViewModel.GetDefaultTwistTarget().normalized.x) > Mathf.Abs(base.transformViewModel.GetDefaultTwistTarget().normalized.y) ? Mathf.Abs(base.transformViewModel.GetDefaultTwistTarget().normalized.x) : base.transformViewModel.GetDefaultTwistTarget().normalized.y) :
                                        new Vector3(0f,
                                        Mathf.Abs(base.transformViewModel.GetDefaultTwistTarget().normalized.x),
                                        Mathf.Abs(base.transformViewModel.GetDefaultTwistTarget().normalized.y))
                                },
                                isFullRotation = true,
                                rotationAmount = base.transformViewModel.GetRotationPerFrame(),
                                sourceTransform = base.transformViewModel.GetSourceTransform(),
                                rotationType = RotationType.useSingleAxis,
                                axisOfRotation = -base.transformViewModel.GetSourceTransform().right, //FOR SINGLE AXIS SPECIFY AXIS OF ROTATION
                            }*/
                        };
                    }
                    break;
                default:
                    break;
            }
            /*List<AxisRotationValidation> rotationValidators = new List<AxisRotationValidation> {
                new AxisRotationValidation()
                { //Rotating X Axis (TWISTING ARM)
                    axisRotationValidator = new AxisValidation {
                        sourceAxis = Axis.up,
                        conditionToUse = true,
                        targetVector = base.transformViewModel.GetAnalogStickInput().y > 0.01f ? new Vector3(0f,
                            Mathf.Abs(base.transformViewModel.GetTwistAxisOfRotation().normalized.x) < Mathf.Abs(base.transformViewModel.GetTwistAxisOfRotation().normalized.y) ? Mathf.Abs(base.transformViewModel.GetTwistAxisOfRotation().normalized.x) : base.transformViewModel.GetTwistAxisOfRotation().normalized.y,
                            Mathf.Abs(base.transformViewModel.GetTwistAxisOfRotation().normalized.x) > Mathf.Abs(base.transformViewModel.GetTwistAxisOfRotation().normalized.y) ? Mathf.Abs(base.transformViewModel.GetTwistAxisOfRotation().normalized.x) : base.transformViewModel.GetTwistAxisOfRotation().normalized.y) :
                            new Vector3(0f,
                            Mathf.Abs(base.transformViewModel.GetTwistAxisOfRotation().normalized.x),
                            Mathf.Abs(base.transformViewModel.GetTwistAxisOfRotation().normalized.y))
                    },
                    isFullRotation = true,
                    rotationAmount = base.transformViewModel.GetRotationPerFrame(),
                    sourceTransform = base.transformViewModel.GetSourceTransform(),
                    rotationType = RotationType.useSingleAxis,
                    axisOfRotation = -base.transformViewModel.GetSourceTransform().right, //FOR SINGLE AXIS SPECIFY AXIS OF ROTATION
                }
            };*/
            return rotationValidators;
        }


        public List<AxisRotationValidation> GetTwistRotationValidatorsUp(Axis twistAxis) {
            List<AxisRotationValidation> rotationValidators = new List<AxisRotationValidation>();
            /*switch (twistAxis) {
                case Axis.inverseRight:
                    rotationValidators = new List<AxisRotationValidation> {
                        new AxisRotationValidation()
                        { //Rotating X Axis (TWISTING ARM)
                            axisRotationValidator = new AxisValidation {
                                sourceAxis = Axis.up,
                                conditionToUse = true,
                                targetVector = base.transformViewModel.GetAnalogStickInput().y > 0.01f ? new Vector3(0f,
                                    Mathf.Abs(base.transformViewModel.GetTwistAxisOfRotationVector().normalized.x) < Mathf.Abs(base.transformViewModel.GetTwistAxisOfRotationVector().normalized.y) ? Mathf.Abs(base.transformViewModel.GetTwistAxisOfRotationVector().normalized.x) : base.transformViewModel.GetTwistAxisOfRotationVector().normalized.y,
                                    Mathf.Abs(base.transformViewModel.GetTwistAxisOfRotationVector().normalized.x) > Mathf.Abs(base.transformViewModel.GetTwistAxisOfRotationVector().normalized.y) ? Mathf.Abs(base.transformViewModel.GetTwistAxisOfRotationVector().normalized.x) : base.transformViewModel.GetTwistAxisOfRotationVector().normalized.y) :
                                    new Vector3(0f,
                                    Mathf.Abs(base.transformViewModel.GetTwistAxisOfRotationVector().normalized.x),
                                    Mathf.Abs(base.transformViewModel.GetTwistAxisOfRotationVector().normalized.y))
                            },
                            isFullRotation = true,
                            rotationAmount = base.transformViewModel.GetRotationPerFrame(),
                            sourceTransform = base.transformViewModel.GetSourceTransform(),
                            rotationType = RotationType.useSingleAxis,
                            axisOfRotation = -base.transformViewModel.GetSourceTransform().right, //FOR SINGLE AXIS SPECIFY AXIS OF ROTATION
                        }
                    };
                    break;
                default:
                    break;
            }*/
            /*rotationValidators = new List<AxisRotationValidation> {
                new AxisRotationValidation()
                { //Rotating X Axis (TWISTING ARM)
                    axisRotationValidator = new AxisValidation {
                        sourceAxis = Axis.up,
                        conditionToUse = true,
                        targetVector = base.transformViewModel.GetAnalogStickInput().y > 0.01f ? new Vector3(0f,
                            Mathf.Abs(base.transformViewModel.GetTwistAxisOfRotation().normalized.x) < Mathf.Abs(base.transformViewModel.GetTwistAxisOfRotation().normalized.y) ? Mathf.Abs(base.transformViewModel.GetTwistAxisOfRotation().normalized.x) : base.transformViewModel.GetTwistAxisOfRotation().normalized.y,
                            Mathf.Abs(base.transformViewModel.GetTwistAxisOfRotation().normalized.x) > Mathf.Abs(base.transformViewModel.GetTwistAxisOfRotation().normalized.y) ? Mathf.Abs(base.transformViewModel.GetTwistAxisOfRotation().normalized.x) : base.transformViewModel.GetTwistAxisOfRotation().normalized.y) :
                            new Vector3(0f,
                            Mathf.Abs(base.transformViewModel.GetTwistAxisOfRotation().normalized.x),
                            Mathf.Abs(base.transformViewModel.GetTwistAxisOfRotation().normalized.y))
                    },
                    isFullRotation = true,
                    rotationAmount = base.transformViewModel.GetRotationPerFrame(),
                    sourceTransform = base.transformViewModel.GetSourceTransform(),
                    rotationType = RotationType.useSingleAxis,
                    axisOfRotation = -base.transformViewModel.GetSourceTransform().right, //FOR SINGLE AXIS SPECIFY AXIS OF ROTATION
                }
            };*/
            return rotationValidators;
        }
        public List<AxisRotationValidation> GetTwistForwardAxisRotationValidators() {
            List<AxisRotationValidation> rotationValidators = new List<AxisRotationValidation> {
                new AxisRotationValidation()
                { //Rotating X Axis (TWISTING ARM)
                    axisRotationValidator = new AxisValidation {
                        sourceAxis = Axis.forward,
                        conditionToUse = (base.transformViewModel.GetRootDirection().z <= 0.01f && base.transformViewModel.GetTargetDirection().y <= 0.01f),
                        targetVector = new Vector3(base.transformViewModel.GetAnalogStickInput().x, Mathf.Abs(base.transformViewModel.GetAnalogStickInput().x) >= Mathf.Abs(base.transformViewModel.GetAnalogStickInput().y) ? 0f : Mathf.Abs(base.transformViewModel.GetAnalogStickInput().y), Mathf.Abs(base.transformViewModel.GetAnalogStickInput().x) >= Mathf.Abs(base.transformViewModel.GetAnalogStickInput().y) ? Mathf.Abs(base.transformViewModel.GetAnalogStickInput().x) : 0f)
                    },
                    isFullRotation = true,
                    rotationAmount = base.transformViewModel.GetRotationPerFrame(),
                    sourceTransform = base.transformViewModel.GetSourceTransform(),
                    rotationType = RotationType.useSingleAxis,
                    axisOfRotation = -base.transformViewModel.GetSourceTransform().right, //FOR SINGLE AXIS SPECIFY AXIS OF ROTATION
                },
                new AxisRotationValidation()
                {
                    axisRotationValidator = new AxisValidation {
                        sourceAxis = Axis.forward,
                        conditionToUse = (base.transformViewModel.GetRootDirection().z <= 0.01f && base.transformViewModel.GetTargetDirection().y > 0.01f),
                        targetVector = base.transformViewModel.GetDefaultTwistTarget()
                    },
                    isFullRotation = true,
                    rotationAmount = base.transformViewModel.GetRotationPerFrame(),
                    sourceTransform = base.transformViewModel.GetSourceTransform(),
                    rotationType = RotationType.useSingleAxis,
                    axisOfRotation = -base.transformViewModel.GetSourceTransform().right, //FOR SINGLE AXIS SPECIFY AXIS OF ROTATION
                },
                new AxisRotationValidation()
                { //Rotating X Axis (TWISTING ARM)
                    axisRotationValidator = new AxisValidation {
                        sourceAxis = Axis.forward,
                        conditionToUse = (base.transformViewModel.GetRootDirection().z > 0.01f && base.transformViewModel.GetTargetDirection().y <= 0.01f),
                        targetVector = base.transformViewModel.GetAnalogStickInput().y < 0f ? -base.transformViewModel.GetDefaultTwistTarget() : base.transformViewModel.GetDefaultTwistTarget()
                    },
                    isFullRotation = true,
                    rotationAmount = base.transformViewModel.GetRotationPerFrame(),
                    sourceTransform = base.transformViewModel.GetSourceTransform(),
                    rotationType = RotationType.useSingleAxis,
                    axisOfRotation = -base.transformViewModel.GetSourceTransform().right, //FOR SINGLE AXIS SPECIFY AXIS OF ROTATION
                },
                new AxisRotationValidation()
                {
                    axisRotationValidator = new AxisValidation {
                        sourceAxis = Axis.forward,
                        conditionToUse = (base.transformViewModel.GetRootDirection().z > 0.01f && base.transformViewModel.GetTargetDirection().y > 0.01f),
                        targetVector = base.transformViewModel.GetDefaultTwistTarget()
                    },
                    isFullRotation = true,
                    rotationAmount = base.transformViewModel.GetRotationPerFrame(),
                    sourceTransform = base.transformViewModel.GetSourceTransform(),
                    rotationType = RotationType.useSingleAxis,
                    axisOfRotation = -base.transformViewModel.GetSourceTransform().right, //FOR SINGLE AXIS SPECIFY AXIS OF ROTATION
                }
            };
            return rotationValidators;
        }

    }
}

