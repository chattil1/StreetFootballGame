using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using FootballGame.Common;
using FootballGame.TransformOperations.Interfaces;
using FootballGame.TransformOperations.Rotation.Validators;
using FootballGame.TransformOperations.Models;
namespace FootballGame.TransformOperations.Models {
    public class TransformInverseRightAxis : TransformAxis
    {
        public TransformInverseRightAxis(TransformViewModel viewModel) : base(viewModel) { }

        public override List<KeyValuePair<string, bool>> RotateTowardsTarget() {
            Vector2 input = transformViewModel.GetAnalogStickInput().normalized;
            return AlignAxis(transformViewModel.GetPlaneDetails(), transformViewModel.GetAxisRotationValidators(), input);
            //return AlignAxis(rotationValidators);

        }

        public override PlaneViewModel RotateWithCheck(Vector3 expectedTarget)
        {
            Vector2 input = transformViewModel.GetAnalogStickInput().normalized;
            return AlignAxisWithCheck(transformViewModel.GetPlaneDetails(), transformViewModel.GetAxisRotationValidators(), input, expectedTarget);
            //return AlignAxis(rotationValidators);

        }

        public List<KeyValuePair<string, bool>> AlignAxis(PlaneViewModel planeDetails, List<AxisRotationValidation> rotationValidators, Vector2 movementInput)
        {
            List<KeyValuePair<string, bool>> transformRotationCheck = new List<KeyValuePair<string, bool>>();
            foreach (var axisDetails in rotationValidators)
            {
                if (ValidationUtility.IsFullyInitialized(axisDetails, int.MaxValue) && axisDetails.axisRotationValidator.conditionToUse)
                {
                    bool properlyRotated = axisDetails.rotationType == RotationType.snap ? SnapToDirection(axisDetails.axisRotationValidator.sourceAxis, axisDetails.sourceTransform, axisDetails.axisRotationValidator.targetVector) :
                                           axisDetails.rotationType == RotationType.useGlobalAndLocalAxes ? AlignUsingGlobalAndLocalAxes(axisDetails.axisRotationValidator.sourceAxis, planeDetails, axisDetails.sourceTransform, axisDetails.axisRotationValidator.targetVector, axisDetails.rotationAmount, axisDetails.isFullRotation, movementInput) :
                                           axisDetails.rotationType == RotationType.useForwardAndUpAxes ? AlignUsingForwardAndUpAxes(axisDetails.axisRotationValidator.sourceAxis, axisDetails.sourceTransform, axisDetails.axisRotationValidator.targetVector, axisDetails.rotationAmount, axisDetails.isFullRotation) :
                                           axisDetails.rotationType == RotationType.useSingleAxis ? RotateTransformToDirectionUsingAxis(GetAxisVector(axisDetails.axisRotationValidator.sourceAxis, axisDetails.sourceTransform), axisDetails.sourceTransform, axisDetails.axisRotationValidator.targetVector, axisDetails.axisOfRotation, axisDetails.rotationAmount, axisDetails.isFullRotation) : false;
                    transformRotationCheck.Add(new KeyValuePair<string, bool>(axisDetails.sourceTransform.name, properlyRotated));
                }
            }
            return transformRotationCheck;
        }
        public PlaneViewModel AlignAxisWithCheck(PlaneViewModel planeDetails, List<AxisRotationValidation> rotationValidators, Vector2 movementInput, Vector3 expectedTarget)
        {
            List<KeyValuePair<string, bool>> transformRotationCheck = new List<KeyValuePair<string, bool>>();
            foreach (var axisDetails in rotationValidators)
            {
                if (ValidationUtility.IsFullyInitialized(axisDetails, int.MaxValue) && axisDetails.axisRotationValidator.conditionToUse)
                {
                    planeDetails = axisDetails.rotationType == RotationType.snap ? planeDetails :
                                           axisDetails.rotationType == RotationType.useGlobalAndLocalAxes ? AlignAxisUsingGlobalAndLocalAxes(axisDetails.axisRotationValidator.sourceAxis, planeDetails, axisDetails.sourceTransform, axisDetails.axisRotationValidator.targetVector, expectedTarget, axisDetails.rotationAmount, axisDetails.isFullRotation, movementInput) :
                                           axisDetails.rotationType == RotationType.useForwardAndUpAxes ? planeDetails :
                                           axisDetails.rotationType == RotationType.useSingleAxis ? planeDetails : planeDetails;
                }
            }
            return planeDetails;
        }

        public override List<KeyValuePair<string, bool>> TwistTowardsTarget() {
            Vector2 input = transformViewModel.GetAnalogStickInput().normalized;
            return AlignAxis(transformViewModel.GetPlaneDetails(), transformViewModel.GetAxisRotationValidators(), input);
        }
        List<AxisRotationValidation> GetRotationValidators(BodyPart bodyPart) {
            switch (bodyPart) {
                case BodyPart.leftArm:
                    return GetLeftArmRotationValidators();
                default:
                    break;
            }
            return new List<AxisRotationValidation>();
        }

        List<AxisRotationValidation> GetLeftArmRotationValidators() {
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
                    conditionToUse = (base.transformViewModel.GetRootDirection().z > 0.01f && base.transformViewModel.GetTargetDirection().y <= 0.01f),
                    targetVector = new Vector3(base.transformViewModel.GetAnalogStickInput().x, Mathf.Abs(base.transformViewModel.GetAnalogStickInput().y), Mathf.Abs(base.transformViewModel.GetAnalogStickInput().x) <= 0.98f ? base.transformViewModel.GetAnalogStickInput().y : Mathf.Abs(base.transformViewModel.GetAnalogStickInput().x))
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
            }/*,
            new AxisRotationValidation()
            { //Rotating X Axis (TWISTING ARM)
                axisRotationValidator = new AxisValidation {
                    sourceAxis = Axis.forward,
                    conditionToUse = (sourceAxis == Axis.forward) && (rootDirection.z <= 0.01f && inputVector.y <= 0.01f),
                    targetVector = new Vector3(movementInput.x, Mathf.Abs(movementInput.x) >= Mathf.Abs(movementInput.y) ? 0f : Mathf.Abs(movementInput.y), Mathf.Abs(movementInput.x) >= Mathf.Abs(movementInput.y) ? Mathf.Abs(movementInput.x) : 0f)
                },
                isFullRotation = true,
                rotationAmount = base.transformViewModel.GetRotationPerFrame(),
                sourceTransform = base.transformViewModel.GetSourceTransform(),
                rotationType = RotationType.useSingleAxis,
                axisOfRotation = -sourceTransform.right, //FOR SINGLE AXIS SPECIFY AXIS OF ROTATION
            },
            new AxisRotationValidation()
            {
                axisRotationValidator = new AxisValidation {
                    sourceAxis = Axis.forward,
                    conditionToUse = (sourceAxis == Axis.forward) && (rootDirection.z <= 0.01f && inputVector.y > 0.01f),
                    targetVector = defaultTargetXAxis
                },
                isFullRotation = true,
                rotationAmount = rotationQuantity,
                sourceTransform = sourceTransform,
                rotationType = RotationType.useSingleAxis,
                axisOfRotation = -sourceTransform.right, //FOR SINGLE AXIS SPECIFY AXIS OF ROTATION
            },
            new AxisRotationValidation()
            { //Rotating X Axis (TWISTING ARM)
                axisRotationValidator = new AxisValidation {
                    sourceAxis = Axis.forward,
                    conditionToUse = (sourceAxis == Axis.forward) && (rootDirection.z > 0.01f && inputVector.y <= 0.01f),
                    targetVector = movementInput.y < 0f ? -defaultTargetXAxis : defaultTargetXAxis
                },
                isFullRotation = true,
                rotationAmount = rotationQuantity,
                sourceTransform = sourceTransform,
                rotationType = RotationType.useSingleAxis,
                axisOfRotation = -sourceTransform.right, //FOR SINGLE AXIS SPECIFY AXIS OF ROTATION
            },
            new AxisRotationValidation()
            {
                axisRotationValidator = new AxisValidation {
                    sourceAxis = Axis.forward,
                    conditionToUse = (sourceAxis == Axis.forward) && (rootDirection.z > 0.01f && inputVector.y > 0.01f),
                    targetVector = defaultTargetXAxis
                },
                isFullRotation = true,
                rotationAmount = rotationQuantity,
                sourceTransform = sourceTransform,
                rotationType = RotationType.useSingleAxis,
                axisOfRotation = -sourceTransform.right, //FOR SINGLE AXIS SPECIFY AXIS OF ROTATION
            }*/};
            return rotationValidators;
        }


        /*List<Vector3> RetrieveOrderOfAxes(bool xMagnitudeGreater)
        {
            List<Vector3> orderedAxesOfRotation = new List
        }*/
        public bool AlignUsingGlobalAndLocalAxes(Axis sourceAxis, PlaneViewModel planeDetails, Transform armToRotate, Vector3 targetDirection, float rotationQuantity, bool fullRotation, Vector2 movementInput)
        {
            Vector3 sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
            bool xMagGreater = Mathf.Abs(movementInput.x) > Mathf.Abs(movementInput.y);
            Vector2 twoDimHorVectorUp = new Vector2(armToRotate.up.x, armToRotate.up.z).normalized;
            Vector2 twoDimHorVectorForward = new Vector2(armToRotate.forward.x, armToRotate.forward.z).normalized;
            bool upHorizontalGreater = Mathf.Abs(twoDimHorVectorUp.magnitude) > Mathf.Abs(twoDimHorVectorForward.magnitude);
            bool upVerticalGreater = Mathf.Abs(armToRotate.up.y) > Mathf.Abs(armToRotate.forward.y);
            Vector3 localAxisOne = Vector3.zero;
            Vector3 localAxisTwo = Vector3.zero;
            Vector3 localAxisThree = Vector3.zero;
            Vector3 localAxisFour = Vector3.zero;
            Vector3 localAxisFive = Vector3.zero;
            float firstMag = 0f;
            float secondMag = 0f;
            float thirdMag = 0f;
            float fourthMag = 0f;
            float fifthMag = 0f;

            if (xMagGreater)
            {
                localAxisOne = Vector3.up;
                firstMag = 1f * Mathf.Abs(movementInput.normalized.x);
                localAxisTwo = upVerticalGreater ? armToRotate.up : armToRotate.forward;
                secondMag = (1f - Mathf.Abs(localAxisTwo.x)) * Mathf.Abs(movementInput.normalized.y);
                localAxisThree = upVerticalGreater ? armToRotate.forward : armToRotate.up;
                thirdMag = (1f - Mathf.Abs(localAxisThree.x)) * Mathf.Abs(movementInput.normalized.y);
                localAxisFour = Vector3.right;
                fourthMag = (1f - Mathf.Abs(localAxisFour.x)) * Mathf.Abs(movementInput.normalized.y);
                localAxisFive = Vector3.forward;
                fifthMag = (1f - Mathf.Abs(localAxisFive.x)) * Mathf.Abs(movementInput.normalized.y);
            }
            else {
                localAxisOne = Vector3.forward;
                firstMag = 1f * Mathf.Abs(movementInput.normalized.y);
                localAxisTwo = upVerticalGreater ? armToRotate.forward : armToRotate.up;
                secondMag = (1f - Mathf.Abs(localAxisTwo.y)) * Mathf.Abs(movementInput.normalized.y);
                localAxisThree = upVerticalGreater ? armToRotate.up : armToRotate.forward;
                thirdMag = (1f - Mathf.Abs(localAxisThree.y)) * Mathf.Abs(movementInput.normalized.y);
                localAxisFour = Vector3.right;
                fourthMag = (1f - Mathf.Abs(localAxisFour.y)) * Mathf.Abs(movementInput.normalized.y);
                localAxisFive = Vector3.up;
                fifthMag = (1f - Mathf.Abs(localAxisFive.y)) * Mathf.Abs(movementInput.normalized.y);
            }
            List<Vector3> axesinOrder = new List<Vector3>() {
                localAxisOne, localAxisTwo, localAxisThree, localAxisFour, localAxisFive
            };
            List<float> magsinOrder = new List<float>() {
                firstMag, secondMag, thirdMag, fourthMag, fifthMag
            };

            RotateTransformToDirection(sourceAxisVector, armToRotate, targetDirection, localAxisOne, rotationQuantity * firstMag, fullRotation);
            sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
            RotateTransformToDirection(sourceAxisVector, armToRotate, targetDirection, localAxisTwo, rotationQuantity * secondMag, fullRotation);
            sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
            RotateTransformToDirection(sourceAxisVector, armToRotate, targetDirection, localAxisThree, rotationQuantity * thirdMag, fullRotation);
            sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
            RotateTransformToDirection(sourceAxisVector, armToRotate, targetDirection, localAxisFour, rotationQuantity * fourthMag, fullRotation);
            sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
            RotateTransformToDirection(sourceAxisVector, armToRotate, targetDirection, localAxisFive, rotationQuantity * fifthMag, fullRotation);
            sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);

            //sourceAxisVector = RotateSource(sourceAxis, armToRotate, targetDirection, rotationQuantity, fullRotation, sourceAxisVector, axesinOrder, magsinOrder);
            /*planeDetails = RotateSource(sourceAxis, planeDetails, armToRotate, targetDirection, rotationQuantity, fullRotation, sourceAxisVector, axesinOrder, magsinOrder);
            sourceAxisVector = planeDetails.referenceVectorDirection;
            if (xMagGreater)
            {
                localAxisTwo = upVerticalGreater ? armToRotate.forward : armToRotate.up;
                localAxisThree = upVerticalGreater ? armToRotate.up : armToRotate.forward;
            }
            else {
                localAxisTwo = upVerticalGreater ? armToRotate.up : armToRotate.forward;
                localAxisThree = upVerticalGreater ? armToRotate.forward : armToRotate.up;
            }

            axesinOrder = new List<Vector3>() {
                localAxisFive, localAxisFour, localAxisThree, localAxisTwo, localAxisOne
            };
            magsinOrder = new List<float>() {
                1f, 1f, 1f, 1f, 1f
            };
            planeDetails = RotateSource(sourceAxis, planeDetails, armToRotate, targetDirection, rotationQuantity, fullRotation, sourceAxisVector, axesinOrder, magsinOrder);
            sourceAxisVector = planeDetails.referenceVectorDirection;*/
            axesinOrder = new List<Vector3>() {
                localAxisFive, localAxisFour, localAxisThree, localAxisTwo, localAxisOne
            };
            magsinOrder = new List<float>() {
                1f, 1f, 1f, 1f, 1f
            };
            RotateTransformToDirection(sourceAxisVector, armToRotate, targetDirection, axesinOrder[0], rotationQuantity, fullRotation);
            sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
            RotateTransformToDirection(sourceAxisVector, armToRotate, targetDirection, axesinOrder[1], rotationQuantity, fullRotation);
            sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
            RotateTransformToDirection(sourceAxisVector, armToRotate, targetDirection, axesinOrder[2], rotationQuantity, fullRotation);
            sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
            RotateTransformToDirection(sourceAxisVector, armToRotate, targetDirection, axesinOrder[3], rotationQuantity, fullRotation);
            sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
            RotateTransformToDirection(sourceAxisVector, armToRotate, targetDirection, axesinOrder[4], rotationQuantity, fullRotation);
            sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);

            Vector3 diffDirection = (targetDirection - sourceAxisVector).normalized;
            //float sixthMag = diffDirection
            if (armToRotate.name.Contains("LeftArm"))
            {
                int j = 0;
            }
            if (Mathf.Abs(sourceAxisVector.normalized.x - targetDirection.normalized.x) <= 0.01f && Mathf.Abs(sourceAxisVector.normalized.y - targetDirection.normalized.y) <= 0.01f && Mathf.Abs(sourceAxisVector.normalized.z - targetDirection.normalized.z) <= 0.01f)
            {
                return true;
            }
            return false;
        }

        enum RotationRole { Horizontal, Vertical, Twist };
        /// <summary>

        /// Rotates the arm around all 5 tracked axes, in order:

        ///   1) Vector3.up        -> yaw, measured from -arm.right (which starts at -Vector3.right at rest)

        ///   2) Vector3.right     -> pitch

        ///   3) arm.forward       -> pitch (re-read here, after the yaw + Vector3.right rotations above)

        ///   4) Vector3.forward   -> pitch

        ///   5) arm.up            -> twist-stability correction only (not accumulated into horizontal/vertical)

        /// Each step rotates via transform.Rotate, then measures how much of that rotation actually

        /// counts using DetermineSignedAngle (forward = -arm.right before the step, target = -arm.right after).

        /// </summary>

        public void RotateArmAcrossAxes(Transform arm, PlaneViewModel planeViewModel, float rotationAmount)

        {

            // 1) Horizontal / yaw

            RotateAndTrack(arm, planeViewModel, Vector3.up, rotationAmount, RotationRole.Horizontal);



            // 2-4) Vertical / pitch, across three different axes as the arm's own orientation shifts

            RotateAndTrack(arm, planeViewModel, Vector3.right, rotationAmount, RotationRole.Vertical);

            RotateAndTrack(arm, planeViewModel, arm.forward, rotationAmount, RotationRole.Vertical);

            RotateAndTrack(arm, planeViewModel, Vector3.forward, rotationAmount, RotationRole.Vertical);



            // 5) Twist stabilization

            RotateAndTrack(arm, planeViewModel, arm.up, rotationAmount, RotationRole.Twist);

        }



        private void RotateAndTrack(Transform arm, PlaneViewModel planeViewModel, Vector3 axis, float rotationAmount, RotationRole role)

        {

            Vector3 beforeDir = -arm.right;

            arm.Rotate(axis, rotationAmount, Space.World);

            Vector3 afterDir = -arm.right;



            float appliedAngle = DetermineSignedAngle(planeViewModel, beforeDir, afterDir, axis);



            switch (role)

            {

                case RotationRole.Horizontal:

                    planeViewModel.horizontalRotation += appliedAngle;

                    break;

                case RotationRole.Vertical:

                    planeViewModel.verticalRotation += appliedAngle;

                    break;

                case RotationRole.Twist:

                    // arm.up pass: keeps the roll stable; intentionally not folded into

                    // horizontalRotation or verticalRotation since it isn't a yaw/pitch measurement.

                    break;

            }

        }




        public PlaneViewModel AlignAxisUsingGlobalAndLocalAxes(Axis sourceAxis, PlaneViewModel planeDetails, Transform armToRotate, Vector3 targetDirection, Vector3 expectedTarget, float rotationQuantity, bool fullRotation, Vector2 movementInput)
        {
            Vector3 sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
            Vector3 originalAxisVector = sourceAxisVector;
            Vector3 originalPivotAxis = armToRotate.forward;
            bool xMagGreater = Mathf.Abs(movementInput.x) > Mathf.Abs(movementInput.y);
            Vector2 twoDimHorVectorUp = new Vector2(armToRotate.up.x, armToRotate.up.z).normalized;
            Vector2 twoDimHorVectorForward = new Vector2(armToRotate.forward.x, armToRotate.forward.z).normalized;
            bool upHorizontalGreater = Mathf.Abs(twoDimHorVectorUp.magnitude) > Mathf.Abs(twoDimHorVectorForward.magnitude);
            bool upVerticalGreater = Mathf.Abs(armToRotate.up.y) > Mathf.Abs(armToRotate.forward.y);
            Vector3 localAxisOne = Vector3.zero;
            Vector3 localAxisTwo = Vector3.zero;
            Vector3 localAxisThree = Vector3.zero;
            Vector3 localAxisFour = Vector3.zero;
            Vector3 localAxisFive = Vector3.zero;
            float firstMag = 0f;
            float secondMag = 0f;
            float thirdMag = 0f;
            float fourthMag = 0f;
            float fifthMag = 0f;
            Vector3 projectedForward = Vector3.ProjectOnPlane(sourceAxisVector, Vector3.up);
            Vector3 projectedTarget = Vector3.ProjectOnPlane(targetDirection, Vector3.up);
            Vector3 projectedForwardF = Vector3.ProjectOnPlane(sourceAxisVector, -Vector3.forward);
            Vector3 projectedTargetF = Vector3.ProjectOnPlane(targetDirection, -Vector3.forward);
            //Vector3 aOR = projectedForward.sqrMagnitude >= 0.99f && projectedTarget.sqrMagnitude >= 0.99f ? Vector3.up : projectedForwardF.sqrMagnitude >= 0.99f && projectedTargetF.sqrMagnitude >= 0.99f ? -Vector3.forward : Vector3.right * -1f;
            //Vector3 aOR = projectedForward.sqrMagnitude >= 0.99f && projectedTarget.sqrMagnitude >= 0.99f ? Vector3.up : -Vector3.forward;
            Vector3 aOR = Vector3.up;
            if (xMagGreater)
            {
                //localAxisOne = Vector3.up;
                localAxisOne = aOR;
                firstMag = 1f * Mathf.Abs(movementInput.normalized.x);
                localAxisTwo = upVerticalGreater ? armToRotate.up : armToRotate.forward;
                secondMag = (1f - Mathf.Abs(localAxisTwo.x)) * Mathf.Abs(movementInput.normalized.y);
                localAxisThree = upVerticalGreater ? armToRotate.forward : armToRotate.up;
                thirdMag = (1f - Mathf.Abs(localAxisThree.x)) * Mathf.Abs(movementInput.normalized.y);
                localAxisFour = Vector3.right;
                fourthMag = (1f - Mathf.Abs(localAxisFour.x)) * Mathf.Abs(movementInput.normalized.y);
                localAxisFive = Vector3.forward;
                fifthMag = (1f - Mathf.Abs(localAxisFive.x)) * Mathf.Abs(movementInput.normalized.y);
            }
            else
            {
                //localAxisOne = Vector3.up;
                localAxisOne = aOR;
                firstMag = 1f * Mathf.Abs(movementInput.normalized.y);
                localAxisTwo = upVerticalGreater ? armToRotate.forward : armToRotate.up;
                //localAxisTwo = Vector3.forward;
                secondMag = 1f * Mathf.Abs(movementInput.normalized.y);
                //localAxisThree = upVerticalGreater ? armToRotate.forward : armToRotate.up;
                localAxisThree = upVerticalGreater ? armToRotate.up: armToRotate.forward;
                thirdMag = (1f - Mathf.Abs(localAxisTwo.y)) * Mathf.Abs(movementInput.normalized.y);
                //localAxisFour = upVerticalGreater ? armToRotate.up : armToRotate.forward;
                localAxisFour = Vector3.forward;
                fourthMag = (1f - Mathf.Abs(localAxisThree.y)) * Mathf.Abs(movementInput.normalized.y);
                localAxisFive = Vector3.right;
                fifthMag = (1f - Mathf.Abs(localAxisFour.y)) * Mathf.Abs(movementInput.normalized.y);

                /*localAxisOne = Vector3.forward;
                firstMag = 1f * Mathf.Abs(movementInput.normalized.y);
                localAxisTwo = upVerticalGreater ? armToRotate.forward : armToRotate.up;
                secondMag = (1f - Mathf.Abs(localAxisTwo.y)) * Mathf.Abs(movementInput.normalized.y);
                localAxisThree = upVerticalGreater ? armToRotate.up : armToRotate.forward;
                thirdMag = (1f - Mathf.Abs(localAxisThree.y)) * Mathf.Abs(movementInput.normalized.y);
                localAxisFour = Vector3.right;
                fourthMag = (1f - Mathf.Abs(localAxisFour.y)) * Mathf.Abs(movementInput.normalized.y);
                localAxisFive = Vector3.up;
                fifthMag = (1f - Mathf.Abs(localAxisFive.y)) * Mathf.Abs(movementInput.normalized.x);*/
            }
            List<Vector3> axesInOrder = new List<Vector3>() {
                localAxisOne, localAxisTwo, localAxisThree, localAxisFour, localAxisFive
            };
            List<float> magsInOrder = new List<float>() {
                firstMag, secondMag, thirdMag, fourthMag, fifthMag
            };
            Vector3 upAxis = Vector3.zero;
            Vector3 forwardAxis = Vector3.zero;
            Tuple<PlaneViewModel, bool> rotationTuple;
            if (xMagGreater)
            {
                //planeDetails = RotateTransformToDirection(planeDetails, sourceAxisVector, armToRotate, targetDirection, expectedTarget, axesInOrder[0], rotationQuantity * magsInOrder[0], fullRotation);
                rotationTuple = RotateTransformWithConstraints(planeDetails, sourceAxisVector, originalAxisVector, originalPivotAxis, armToRotate, targetDirection, expectedTarget, axesInOrder[0], rotationQuantity * magsInOrder[0], fullRotation);
            }
            else {
                //planeDetails = RotateTransformToDirection(planeDetails, sourceAxisVector, armToRotate, targetDirection, expectedTarget, localAxisOne, rotationQuantity * firstMag, fullRotation);
                rotationTuple = RotateTransformWithConstraints(planeDetails, sourceAxisVector, originalAxisVector, originalPivotAxis, armToRotate, targetDirection, expectedTarget, axesInOrder[0], rotationQuantity * magsInOrder[0], fullRotation);
            }
            sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
            upAxis = GetAxisVector(Axis.up, armToRotate);
            forwardAxis = GetAxisVector(Axis.forward, armToRotate);
            originalAxisVector = sourceAxisVector;
            originalPivotAxis = armToRotate.forward;
            if (rotationTuple.Item2) {
                if (xMagGreater)
                {
                    //planeDetails = RotateTransformToDirection(planeDetails, sourceAxisVector, armToRotate, targetDirection, expectedTarget, localAxisTwo, rotationQuantity * secondMag, fullRotation);
                    rotationTuple = RotateTransformWithConstraints(planeDetails, sourceAxisVector, originalAxisVector, originalPivotAxis, armToRotate, targetDirection, expectedTarget, axesInOrder[1], rotationQuantity * magsInOrder[1], fullRotation);
                }
                else
                {
                    rotationTuple = RotateTransformWithConstraints(planeDetails, sourceAxisVector, originalAxisVector, originalPivotAxis, armToRotate, targetDirection, expectedTarget, axesInOrder[1], rotationQuantity * magsInOrder[1], fullRotation);
                    //planeDetails = RotateTransformToDirection(planeDetails, sourceAxisVector, armToRotate, targetDirection, expectedTarget, localAxisTwo, rotationQuantity * secondMag, fullRotation);
                }
                //RotateTransformToDirection(sourceAxisVector, armToRotate, targetDirection, localAxisTwo, rotationQuantity * secondMag, fullRotation);
                sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
                upAxis = GetAxisVector(Axis.up, armToRotate);
                forwardAxis = GetAxisVector(Axis.forward, armToRotate);
                if (xMagGreater)
                {
                    //planeDetails = RotateTransformToDirection(planeDetails, sourceAxisVector, armToRotate, targetDirection, expectedTarget, localAxisThree, rotationQuantity * thirdMag, fullRotation);
                    rotationTuple = RotateTransformWithConstraints(planeDetails, sourceAxisVector, originalAxisVector, originalPivotAxis, armToRotate, targetDirection, expectedTarget, axesInOrder[2], rotationQuantity * magsInOrder[2], fullRotation);
                }
                else
                {
                    rotationTuple = RotateTransformWithConstraints(planeDetails, sourceAxisVector, originalAxisVector, originalPivotAxis, armToRotate, targetDirection, expectedTarget, axesInOrder[2], rotationQuantity * magsInOrder[2], fullRotation);
                    //planeDetails = RotateTransformToDirection(planeDetails, sourceAxisVector, armToRotate, targetDirection, expectedTarget, localAxisThree, rotationQuantity * thirdMag, fullRotation);
                }
                //RotateTransformToDirection(sourceAxisVector, armToRotate, targetDirection, localAxisThree, rotationQuantity * thirdMag, fullRotation);
                sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
                upAxis = GetAxisVector(Axis.up, armToRotate);
                forwardAxis = GetAxisVector(Axis.forward, armToRotate);
                if (xMagGreater)
                {
                    //planeDetails = RotateTransformToDirection(planeDetails, sourceAxisVector, armToRotate, targetDirection, expectedTarget, localAxisThree, rotationQuantity * thirdMag, fullRotation);
                    //planeDetails = RotateTransformToDirection(planeDetails, sourceAxisVector, armToRotate, targetDirection, expectedTarget, localAxisFour, rotationQuantity * fourthMag, fullRotation);
                    rotationTuple = RotateTransformWithConstraints(planeDetails, sourceAxisVector, originalAxisVector, originalPivotAxis, armToRotate, targetDirection, expectedTarget, axesInOrder[3], rotationQuantity * magsInOrder[3], fullRotation);
                }
                else
                {
                    rotationTuple = RotateTransformWithConstraints(planeDetails, sourceAxisVector, originalAxisVector, originalPivotAxis, armToRotate, targetDirection, expectedTarget, axesInOrder[3], rotationQuantity * magsInOrder[3], fullRotation);
                    //planeDetails = RotateTransformToDirection(planeDetails, sourceAxisVector, armToRotate, targetDirection, expectedTarget, localAxisThree, rotationQuantity * thirdMag, fullRotation);
                }
                //planeDetails = RotateTransformToDirection(planeDetails, sourceAxisVector, armToRotate, targetDirection, expectedTarget, localAxisFour, rotationQuantity * fourthMag, fullRotation);
                sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
                upAxis = GetAxisVector(Axis.up, armToRotate);
                forwardAxis = GetAxisVector(Axis.forward, armToRotate);
                if (!xMagGreater)
                {
                    //planeDetails = RotateTransformToDirection(planeDetails, sourceAxisVector, armToRotate, targetDirection, expectedTarget, axesInOrder[4], rotationQuantity * magsInOrder[4], fullRotation);
                    rotationTuple = RotateTransformWithConstraints(planeDetails, sourceAxisVector, originalAxisVector, originalPivotAxis, armToRotate, targetDirection, expectedTarget, axesInOrder[4], rotationQuantity * magsInOrder[4], fullRotation);
                }
                else
                {
                    rotationTuple = RotateTransformWithConstraints(planeDetails, sourceAxisVector, originalAxisVector, originalPivotAxis, armToRotate, targetDirection, expectedTarget, axesInOrder[4], magsInOrder[4] * fifthMag, fullRotation);
                    //planeDetails = RotateTransformToDirection(planeDetails, sourceAxisVector, armToRotate, targetDirection, expectedTarget, localAxisFive, rotationQuantity * fifthMag, fullRotation);
                }
                //RotateTransformToDirection(sourceAxisVector, armToRotate, targetDirection, localAxisFive, rotationQuantity * fifthMag, fullRotation);
                sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
                axesInOrder = new List<Vector3>() {
                    localAxisFive, localAxisFour, localAxisThree, localAxisTwo, localAxisOne
                };
                magsInOrder = new List<float>() {
                    1f, 1f, 1f, 1f, 1f
                };
                if (!xMagGreater)
                {
                    //planeDetails = RotateTransformToDirection(planeDetails, sourceAxisVector, armToRotate, targetDirection, expectedTarget, axesInOrder[0], rotationQuantity * magsInOrder[0], fullRotation);
                    rotationTuple = RotateTransformWithConstraints(planeDetails, sourceAxisVector, originalAxisVector, originalPivotAxis, armToRotate, targetDirection, expectedTarget, axesInOrder[0], rotationQuantity * magsInOrder[0], fullRotation);
                    //RotateTransformToDirection(sourceAxisVector, armToRotate, targetDirection, axesInOrder[0], rotationQuantity, fullRotation);
                }
                else
                {
                    rotationTuple = RotateTransformWithConstraints(planeDetails, sourceAxisVector, originalAxisVector, originalPivotAxis, armToRotate, targetDirection, expectedTarget, axesInOrder[0], rotationQuantity * magsInOrder[0], fullRotation);
                    //planeDetails = RotateTransformToDirection(planeDetails, sourceAxisVector, armToRotate, targetDirection, expectedTarget, axesInOrder[0], rotationQuantity, fullRotation);
                }
                //RotateTransformToDirection(sourceAxisVector, armToRotate, targetDirection, axesInOrder[0], rotationQuantity, fullRotation);
                sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
                upAxis = GetAxisVector(Axis.up, armToRotate);
                forwardAxis = GetAxisVector(Axis.forward, armToRotate);
                if (xMagGreater)
                {
                    //planeDetails = RotateTransformToDirection(planeDetails, sourceAxisVector, armToRotate, targetDirection, expectedTarget, axesInOrder[1], rotationQuantity, fullRotation);
                    rotationTuple = RotateTransformWithConstraints(planeDetails, sourceAxisVector, originalAxisVector, originalPivotAxis, armToRotate, targetDirection, expectedTarget, axesInOrder[1], rotationQuantity * magsInOrder[1], fullRotation);
                }
                else {
                    rotationTuple = RotateTransformWithConstraints(planeDetails, sourceAxisVector, originalAxisVector, originalPivotAxis, armToRotate, targetDirection, expectedTarget, axesInOrder[1], rotationQuantity * magsInOrder[1], fullRotation);
                }
                //planeDetails = RotateTransformToDirection(planeDetails, sourceAxisVector, armToRotate, targetDirection, expectedTarget, axesInOrder[1], rotationQuantity, fullRotation);
                sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
                upAxis = GetAxisVector(Axis.up, armToRotate);
                forwardAxis = GetAxisVector(Axis.forward, armToRotate);
                Vector3 axisToUse;
                //RotateTransformToDirection(sourceAxisVector, armToRotate, targetDirection, axesInOrder[2], rotationQuantity, fullRotation);
                if (xMagGreater)
                {
                    axisToUse = forwardAxis;
                    rotationTuple = RotateTransformWithConstraints(planeDetails, sourceAxisVector, originalAxisVector, originalPivotAxis, armToRotate, targetDirection, expectedTarget, axisToUse, rotationQuantity * magsInOrder[2], fullRotation);
                    //planeDetails = RotateTransformToDirection(planeDetails, sourceAxisVector, armToRotate, targetDirection, expectedTarget, axesInOrder[2], rotationQuantity, fullRotation);
                }
                else
                {
                    axisToUse = upAxis;
                    rotationTuple = RotateTransformWithConstraints(planeDetails, sourceAxisVector, originalAxisVector, originalPivotAxis, armToRotate, targetDirection, expectedTarget, axisToUse, rotationQuantity * magsInOrder[2], fullRotation);
                    //planeDetails = RotateTransformToDirection(planeDetails, sourceAxisVector, armToRotate, targetDirection, expectedTarget, axesInOrder[2], rotationQuantity, fullRotation);
                }
                sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
                upAxis = GetAxisVector(Axis.up, armToRotate);
                forwardAxis = GetAxisVector(Axis.forward, armToRotate);
                //RotateTransformToDirection(sourceAxisVector, armToRotate, targetDirection, axesInOrder[3], rotationQuantity, fullRotation);
                if (xMagGreater)
                {
                    axisToUse = upAxis;
                    //planeDetails = RotateTransformToDirection(planeDetails, sourceAxisVector, armToRotate, targetDirection, expectedTarget, axesInOrder[3], rotationQuantity, fullRotation);
                    rotationTuple = RotateTransformWithConstraints(planeDetails, sourceAxisVector, originalAxisVector, originalPivotAxis, armToRotate, targetDirection, expectedTarget, axisToUse, rotationQuantity * magsInOrder[3], fullRotation);
                    //rotationTuple = RotateTransformWithConstraints(planeDetails, sourceAxisVector, originalAxisVector, originalPivotAxis, armToRotate, targetDirection, expectedTarget, axesInOrder[3], rotationQuantity * magsInOrder[3], fullRotation);
                }
                else
                {
                    axisToUse = forwardAxis;
                    //planeDetails = RotateTransformToDirection(planeDetails, sourceAxisVector, armToRotate, targetDirection, expectedTarget, axesInOrder[3], rotationQuantity, fullRotation);
                    rotationTuple = RotateTransformWithConstraints(planeDetails, sourceAxisVector, originalAxisVector, originalPivotAxis, armToRotate, targetDirection, expectedTarget, axisToUse, rotationQuantity * magsInOrder[3], fullRotation);
                    //rotationTuple = RotateTransformWithConstraints(planeDetails, sourceAxisVector, originalAxisVector, originalPivotAxis, armToRotate, targetDirection, expectedTarget, axesInOrder[3], rotationQuantity * magsInOrder[3], fullRotation);
                }
                sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
                upAxis = GetAxisVector(Axis.up, armToRotate);
                forwardAxis = GetAxisVector(Axis.forward, armToRotate);
                if (xMagGreater)
                {
                    //planeDetails = RotateTransformToDirection(planeDetails, sourceAxisVector, armToRotate, targetDirection, expectedTarget, axesInOrder[4], rotationQuantity * magsInOrder[4], fullRotation);
                    rotationTuple = RotateTransformWithConstraints(planeDetails, sourceAxisVector, originalAxisVector, originalPivotAxis, armToRotate, targetDirection, expectedTarget, axesInOrder[4], rotationQuantity * magsInOrder[4], fullRotation);
                }
                else
                {
                    //planeDetails = RotateTransformToDirection(planeDetails, sourceAxisVector, armToRotate, targetDirection, expectedTarget, axesInOrder[4], rotationQuantity, fullRotation);
                    rotationTuple = RotateTransformWithConstraints(planeDetails, sourceAxisVector, originalAxisVector, originalPivotAxis, armToRotate, targetDirection, expectedTarget, axesInOrder[4], rotationQuantity * magsInOrder[4], fullRotation);
                }
                //RotateTransformToDirection(sourceAxisVector, armToRotate, targetDirection, axesInOrder[4], rotationQuantity, fullRotation);
                sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
                upAxis = GetAxisVector(Axis.up, armToRotate);
                forwardAxis = GetAxisVector(Axis.forward, armToRotate);

                Vector3 diffDirection = (targetDirection - sourceAxisVector).normalized;
                //float sixthMag = diffDirection
                if (armToRotate.name.Contains("LeftArm"))
                {
                    int j = 0;
                }
            }

            return rotationTuple.Item1;
        }

        /*public PlaneViewModel AlignAxisUsingGlobalAndLocalAxes(Axis sourceAxis, PlaneViewModel planeDetails, Transform armToRotate, Vector3 targetDirection, float rotationQuantity, bool fullRotation, Vector2 movementInput)
        {
            //Vector3 sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
            Vector3 sourceAxisVector = planeDetails.referenceVectorDirection.magnitude > 0 ? planeDetails.referenceVectorDirection.normalized : GetAxisVector(sourceAxis, armToRotate);
            bool xMagGreater = Mathf.Abs(movementInput.x) > Mathf.Abs(movementInput.y);
            Vector2 twoDimHorVectorUp = new Vector2(armToRotate.up.x, armToRotate.up.z).normalized;
            Vector2 twoDimHorVectorForward = new Vector2(armToRotate.forward.x, armToRotate.forward.z).normalized;
            bool upHorizontalGreater = Mathf.Abs(twoDimHorVectorUp.magnitude) > Mathf.Abs(twoDimHorVectorForward.magnitude);
            bool upVerticalGreater = Mathf.Abs(armToRotate.up.y) > Mathf.Abs(armToRotate.forward.y);
            Vector3 localAxisOne = Vector3.zero;
            Vector3 localAxisTwo = Vector3.zero;
            Vector3 localAxisThree = Vector3.zero;
            Vector3 localAxisFour = Vector3.zero;
            Vector3 localAxisFive = Vector3.zero;
            float firstMag = 0f;
            float secondMag = 0f;
            float thirdMag = 0f;
            float fourthMag = 0f;
            float fifthMag = 0f;

            if (xMagGreater)
            {
                localAxisOne = Vector3.up;
                firstMag = 1f * Mathf.Abs(movementInput.normalized.x);
                localAxisTwo = upVerticalGreater ? armToRotate.up : armToRotate.forward;
                secondMag = (1f - Mathf.Abs(localAxisTwo.x)) * Mathf.Abs(movementInput.normalized.y);
                localAxisThree = upVerticalGreater ? armToRotate.forward : armToRotate.up;
                thirdMag = (1f - Mathf.Abs(localAxisThree.x)) * Mathf.Abs(movementInput.normalized.y);
                localAxisFour = Vector3.right;
                fourthMag = (1f - Mathf.Abs(localAxisFour.x)) * Mathf.Abs(movementInput.normalized.y);
                localAxisFive = Vector3.forward;
                fifthMag = (1f - Mathf.Abs(localAxisFive.x)) * Mathf.Abs(movementInput.normalized.y);
            }
            else
            {
                localAxisOne = Vector3.forward;
                firstMag = 1f * Mathf.Abs(movementInput.normalized.y);
                localAxisTwo = upVerticalGreater ? armToRotate.forward : armToRotate.up;
                secondMag = (1f - Mathf.Abs(localAxisTwo.y)) * Mathf.Abs(movementInput.normalized.y);
                localAxisThree = upVerticalGreater ? armToRotate.up : armToRotate.forward;
                thirdMag = (1f - Mathf.Abs(localAxisThree.y)) * Mathf.Abs(movementInput.normalized.y);
                localAxisFour = Vector3.right;
                fourthMag = (1f - Mathf.Abs(localAxisFour.y)) * Mathf.Abs(movementInput.normalized.y);
                localAxisFive = Vector3.up;
                fifthMag = (1f - Mathf.Abs(localAxisFive.y)) * Mathf.Abs(movementInput.normalized.y);
            }
            List<Vector3> axesinOrder = new List<Vector3>() {
                localAxisOne, localAxisTwo, localAxisThree, localAxisFour, localAxisFive
            };
            List<float> magsinOrder = new List<float>() {
                Mathf.Abs(firstMag) > 0.01f ? firstMag : 0f, Mathf.Abs(secondMag) > 0.01f ? secondMag : 0f, Mathf.Abs(thirdMag) > 0.01f ? thirdMag : 0f, Mathf.Abs(fourthMag) > 0.01f ? fourthMag : 0f, Mathf.Abs(fifthMag) > 0.01f ? fifthMag : 0f
            };
            float deltaVert = Mathf.Clamp01(Mathf.Abs(Mathf.Abs(targetDirection.y) - Mathf.Abs(sourceAxisVector.y)));
            planeDetails = RotateSource(sourceAxis, planeDetails, armToRotate, targetDirection, rotationQuantity, fullRotation, sourceAxisVector, axesinOrder, magsinOrder);
            //sourceAxisVector = planeDetails.referenceVectorDirection;
            sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);


            axesinOrder = new List<Vector3>() {
                localAxisFive, localAxisFour, localAxisThree, localAxisTwo, localAxisOne
            };

            if (xMagGreater)
            {
                magsinOrder = new List<float>() {
                    deltaVert, deltaVert, deltaVert, deltaVert, deltaVert
                };
            }
            else {
                magsinOrder = new List<float>() {
                    0f, 0f, 0f, 0f, 0f
                };
            }
            planeDetails = RotateSource(sourceAxis, planeDetails, armToRotate, targetDirection, rotationQuantity, fullRotation, sourceAxisVector, axesinOrder, magsinOrder);
            sourceAxisVector = planeDetails.referenceVectorDirection;
            return planeDetails;
        }*/

        public static float GetRotationSign(
        Vector3 currentDirection,
        Vector3 targetDirection,
        Vector3 rotationAxis)
        {
            Vector3 cross = Vector3.Cross(
                currentDirection.normalized,
                targetDirection.normalized);

            float dot = Vector3.Dot(
                cross,
                rotationAxis.normalized);

            return Mathf.Sign(dot);
        }
        float DetermineAngle(PlaneViewModel planeDetails, float counterClockwiseSign)
        {
            bool isCounterClockwise = Mathf.Sign(Vector3.SignedAngle(planeDetails.referenceVectorDirection, planeDetails.targetDirection, planeDetails.plane)) == counterClockwiseSign ? true : false;
            return 0f;
        }
        float GetDotProduct(Vector3 vector1, Vector3 vector2, Vector3 axisOfRotation) {
            Vector3 cross = Vector3.Cross(vector1, vector2);
            return Mathf.Sign(Vector3.Dot(cross, axisOfRotation)); }



        /*PlaneViewModel RotateSource(Axis sourceAxis, PlaneViewModel planeDetails, Transform armToRotate, Vector3 targetDirection, float rotationQuantity, bool fullRotation, Vector3 sourceAxisVector, List<Vector3> axesInOrder, List<float> magsInOrder) {
            if (axesInOrder.Count == 5 && axesInOrder.Count == magsInOrder.Count) {
                planeDetails = RotateTransformToDirection(planeDetails, sourceAxisVector, armToRotate, targetDirection, axesInOrder[0], rotationQuantity * magsInOrder[0], fullRotation);
                sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
                planeDetails = RotateTransformToDirection(planeDetails, sourceAxisVector, armToRotate, targetDirection, axesInOrder[1], rotationQuantity * magsInOrder[1], fullRotation);
                sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
                planeDetails = RotateTransformToDirection(planeDetails, sourceAxisVector, armToRotate, targetDirection, axesInOrder[2], rotationQuantity * magsInOrder[2], fullRotation);
                sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
                planeDetails = RotateTransformToDirection(planeDetails, sourceAxisVector, armToRotate, targetDirection, axesInOrder[3], rotationQuantity * magsInOrder[3], fullRotation);
                sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
                planeDetails = RotateTransformToDirection(planeDetails, sourceAxisVector, armToRotate, targetDirection, axesInOrder[4], rotationQuantity * magsInOrder[4], fullRotation);
                sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
            }

            return planeDetails;

        } */

    }
}

