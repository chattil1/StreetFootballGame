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




        /// <summary>
        /// true  = old behaviour: step around 5 axes (Vector3.up, arm.forward, arm.up, Vector3.forward, Vector3.right).
        /// false = step yaw and pitch directly, clamped to the limits on every step (AlignArmWithinConstraints).
        /// </summary>
        public static bool UseLegacyMultiAxisAiming = false;

        /// <summary>When the arm can't reach the target inside its horizontal limits, turn the root (Vector3.up only).</summary>
        public static bool AllowRootTurnAssist = true;

        /// <summary>
        /// If the target is no more than this many degrees further from the closest reachable point than from
        /// a point that needs a much longer route (e.g. slightly past straight down at the side, which would
        /// otherwise need a 180 degree sweep through the front), stop at the closest point instead.
        /// </summary>
        public static float StopShortTolerance = 15f;

        /// <summary>
        /// The root turns when the shortest route leaves the horizontal limits and the best route inside the
        /// limits is longer by more than this (degrees of yaw + pitch travel), or can't reach the target.
        /// </summary>
        public static float RootTurnAssistMargin = 90f;

        const float ReachedTolerance = 0.5f;
        const int TargetCandidateCount = YawPitchCandidateCount * 3;

        struct YawPitchCandidate
        {
            public float horizontal;
            public float vertical;
            public float error;   // degrees between where this (yaw, pitch) points and the target
            public float travel;  // yaw + pitch distance from the arm now, plus a small over-the-top cost
        }

        /// <summary>
        /// Moves the source axis (-arm.right) towards targetDirection by stepping
        /// horizontalRotation (yaw around up) and the total vertical rotation (pitch inside the yaw)
        /// directly, so the arm can never leave the limits:
        ///   - yaw stays inside [-maxHorizontalCounterClockwise, maxHorizontalClockwise], so going from
        ///     left (0) to right (180) always goes clockwise through the front, never around the back.
        ///   - pitch stays inside the vertical limits for the current yaw (forward-plane limits at the
        ///     side, right-plane limits in front).
        ///   - if the shortest route would leave the horizontal limits (and staying inside them means a much
        ///     longer route or can't reach), the arm goes to the limit and the root turns around Vector3.up
        ///     to bring the target in (AllowRootTurnAssist). targetDirection must be in world space for this.
        /// The rotations are applied with transform.Rotate (Space.World).
        /// </summary>
        public PlaneViewModel AlignArmWithinConstraints(Axis sourceAxis, PlaneViewModel planeDetails, Transform armToRotate, Vector3 targetDirection, float horizontalStep, float verticalStep, bool fullRotation)
        {
            SyncRecordedRotation(planeDetails, armToRotate, sourceAxis);
            if (targetDirection.sqrMagnitude < 1e-6f)
            {
                return planeDetails;
            }
            Vector3 target = targetDirection.normalized;
            GetRootBasis(out Vector3 rootForward, out Vector3 rootUp, out Vector3 rootRight);

            float horizontal = planeDetails.horizontalRotation;
            float vertical = GetTotalVerticalRotation(planeDetails);
            float maxHorizontalStep = fullRotation ? float.MaxValue : Mathf.Abs(horizontalStep);
            float maxVerticalStep = fullRotation ? float.MaxValue : Mathf.Abs(verticalStep);
            GetHorizontalLimits(planeDetails, out float minHorizontal, out float maxHorizontal);

            float targetHorizontal;
            float targetVertical;
            Transform root = transformViewModel != null ? transformViewModel.GetRootTransform() : null;
            if (AllowRootTurnAssist && root != null && root != armToRotate &&
                NeedsRootTurn(planeDetails, target, horizontal, vertical, rootForward, rootUp, rootRight, out float outsideHorizontal, out float outsideVertical))
            {
                // Turn the root by however far the shortest route is past the horizontal limit (one step per frame).
                // The arm is a child of the root, so its recorded (yaw, pitch) relative to the root don't change.
                float limit = Mathf.Clamp(outsideHorizontal, minHorizontal, maxHorizontal);
                float rootTurn = Mathf.Clamp(outsideHorizontal - limit, -maxHorizontalStep, maxHorizontalStep);
                root.Rotate(Vector3.up, rootTurn, Space.World);
                GetRootBasis(out rootForward, out rootUp, out rootRight);
                targetHorizontal = limit;
                GetVerticalLimits(planeDetails, limit, out float minLimitVertical, out float maxLimitVertical);
                targetVertical = Mathf.Clamp(outsideVertical, minLimitVertical, maxLimitVertical);
            }
            else
            {
                ChooseTargetYawPitch(planeDetails, target, horizontal, vertical, rootForward, rootUp, rootRight, out targetHorizontal, out targetVertical);
            }

            float newHorizontal = Mathf.Clamp(horizontal + Mathf.Clamp(targetHorizontal - horizontal, -maxHorizontalStep, maxHorizontalStep), minHorizontal, maxHorizontal);
            GetVerticalLimits(planeDetails, newHorizontal, out float minVertical, out float maxVertical);
            float newVertical = Mathf.Clamp(vertical + Mathf.Clamp(targetVertical - vertical, -maxVerticalStep, maxVerticalStep), minVertical, maxVertical);

            // Rotate from the current (yaw, pitch) to the new one. Going through the yaw/pitch frame keeps the
            // arm's twist the same relative to that frame.
            Quaternion delta = YawPitchRotation(newHorizontal, newVertical, rootForward, rootUp) *
                               Quaternion.Inverse(YawPitchRotation(horizontal, vertical, rootForward, rootUp));
            delta.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f)
            {
                angle -= 360f;
            }
            if (Mathf.Abs(angle) > 0.0001f && !float.IsNaN(axis.x) && !float.IsInfinity(axis.x) && axis.sqrMagnitude > 0f)
            {
                armToRotate.Rotate(axis, angle, Space.World);
            }

            WriteRecordedRotation(planeDetails, newHorizontal, newVertical);
            planeDetails.targetDirection = targetDirection;
            return planeDetails;
        }

        /// <summary>
        /// Every way to point at the target (in front / over the top / past +-180, each with yaw +-360),
        /// clamped into the limits.
        /// </summary>
        YawPitchCandidate[] GetClampedCandidates(PlaneViewModel planeDetails, Vector3 target, float horizontal, float vertical, Vector3 rootForward, Vector3 rootUp, Vector3 rootRight)
        {
            float[] headings = new float[YawPitchCandidateCount];
            float[] verticals = new float[YawPitchCandidateCount];
            GetYawPitchCandidates(target, rootForward, rootUp, rootRight, horizontal, headings, verticals);
            GetHorizontalLimits(planeDetails, out float minHorizontal, out float maxHorizontal);

            YawPitchCandidate[] candidates = new YawPitchCandidate[TargetCandidateCount];
            int index = 0;
            for (int reading = 0; reading < YawPitchCandidateCount; reading++)
            {
                for (int turns = -1; turns <= 1; turns++)
                {
                    float candidateHorizontal = Mathf.Clamp(UnwrapAngleNear(headings[reading], horizontal) + 360f * turns, minHorizontal, maxHorizontal);
                    GetVerticalLimits(planeDetails, candidateHorizontal, out float minVertical, out float maxVertical);
                    float candidateVertical = Mathf.Clamp(verticals[reading], minVertical, maxVertical);
                    Vector3 candidateDirection = DirectionFromYawPitch(candidateHorizontal, candidateVertical, rootForward, rootUp, rootRight);
                    candidates[index++] = new YawPitchCandidate
                    {
                        horizontal = candidateHorizontal,
                        vertical = candidateVertical,
                        error = Vector3.Angle(candidateDirection, target),
                        travel = Travel(horizontal, vertical, candidateHorizontal, candidateVertical),
                    };
                }
            }
            return candidates;
        }

        // Yaw + pitch distance, plus a small cost for being past straight up/down so a tie
        // (e.g. rest -> (1,0,0)) goes through the front instead of over the top.
        static float Travel(float fromHorizontal, float fromVertical, float toHorizontal, float toVertical)
        {
            return Mathf.Abs(toHorizontal - fromHorizontal) + Mathf.Abs(toVertical - fromVertical) +
                   0.5f * Mathf.Max(0f, Mathf.Abs(toVertical) - 90f);
        }

        /// <summary>
        /// Picks the (yaw, pitch) to aim for inside the limits:
        ///   1. candidates that point at the target, or within StopShortTolerance of the closest any can get
        ///   2. of those, the one with the shortest route from where the arm is now
        /// </summary>
        void ChooseTargetYawPitch(PlaneViewModel planeDetails, Vector3 target, float horizontal, float vertical, Vector3 rootForward, Vector3 rootUp, Vector3 rootRight, out float targetHorizontal, out float targetVertical)
        {
            YawPitchCandidate[] candidates = GetClampedCandidates(planeDetails, target, horizontal, vertical, rootForward, rootUp, rootRight);
            float bestError = float.MaxValue;
            foreach (YawPitchCandidate candidate in candidates)
            {
                bestError = Mathf.Min(bestError, candidate.error);
            }
            float bestScore = float.MaxValue;
            targetHorizontal = horizontal;
            targetVertical = vertical;
            foreach (YawPitchCandidate candidate in candidates)
            {
                if (candidate.error > bestError + StopShortTolerance)
                {
                    continue;
                }
                float score = candidate.error * 4f + candidate.travel;
                if (score < bestScore)
                {
                    bestScore = score;
                    targetHorizontal = candidate.horizontal;
                    targetVertical = candidate.vertical;
                }
            }
        }

        /// <summary>
        /// True when the shortest route to the target goes outside the horizontal limits and staying inside
        /// them either can't reach the target or is longer by more than RootTurnAssistMargin.
        /// outsideHorizontal / outsideVertical are where that shortest route ends (yaw outside the limits).
        /// </summary>
        bool NeedsRootTurn(PlaneViewModel planeDetails, Vector3 target, float horizontal, float vertical, Vector3 rootForward, Vector3 rootUp, Vector3 rootRight, out float outsideHorizontal, out float outsideVertical)
        {
            outsideHorizontal = horizontal;
            outsideVertical = vertical;
            float[] headings = new float[YawPitchCandidateCount];
            float[] verticals = new float[YawPitchCandidateCount];
            GetYawPitchCandidates(target, rootForward, rootUp, rootRight, horizontal, headings, verticals);
            GetHorizontalLimits(planeDetails, out float minHorizontal, out float maxHorizontal);

            float bestOutsideTravel = float.MaxValue;
            for (int reading = 0; reading < YawPitchCandidateCount; reading++)
            {
                for (int turns = -1; turns <= 1; turns++)
                {
                    float candidateHorizontal = UnwrapAngleNear(headings[reading], horizontal) + 360f * turns;
                    if (candidateHorizontal >= minHorizontal && candidateHorizontal <= maxHorizontal)
                    {
                        continue; // inside the limits: no turn needed for this one
                    }
                    GetVerticalLimits(planeDetails, candidateHorizontal, out float minVertical, out float maxVertical);
                    if (verticals[reading] < minVertical || verticals[reading] > maxVertical)
                    {
                        continue;
                    }
                    float travel = Travel(horizontal, vertical, candidateHorizontal, verticals[reading]);
                    if (travel < bestOutsideTravel)
                    {
                        bestOutsideTravel = travel;
                        outsideHorizontal = candidateHorizontal;
                        outsideVertical = verticals[reading];
                    }
                }
            }
            if (bestOutsideTravel == float.MaxValue)
            {
                return false;
            }

            float bestInsideTravel = float.MaxValue;
            foreach (YawPitchCandidate candidate in GetClampedCandidates(planeDetails, target, horizontal, vertical, rootForward, rootUp, rootRight))
            {
                if (candidate.error <= ReachedTolerance)
                {
                    bestInsideTravel = Mathf.Min(bestInsideTravel, candidate.travel);
                }
            }
            return bestInsideTravel > bestOutsideTravel + RootTurnAssistMargin;
        }

        public PlaneViewModel AlignAxisUsingGlobalAndLocalAxes(Axis sourceAxis, PlaneViewModel planeDetails, Transform armToRotate, Vector3 targetDirection, Vector3 expectedTarget, float rotationQuantity, bool fullRotation, Vector2 movementInput)
        {
            if (!UseLegacyMultiAxisAiming)
            {
                return AlignArmWithinConstraints(sourceAxis, planeDetails, armToRotate, targetDirection, rotationQuantity, rotationQuantity, fullRotation);
            }
            // Start from where the arm really is this frame (the animator / LateUpdate restore can move it
            // between frames), so the constraint checks below compare against accurate values.
            SyncRecordedRotation(planeDetails, armToRotate, sourceAxis);
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
                    rotationTuple = RotateTransformWithConstraints(planeDetails, sourceAxisVector, originalAxisVector, originalPivotAxis, armToRotate, targetDirection, expectedTarget, axesInOrder[4], rotationQuantity * magsInOrder[4], fullRotation);
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

