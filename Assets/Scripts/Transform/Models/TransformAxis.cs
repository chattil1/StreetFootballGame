using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using FootballGame.Common;
using FootballGame.TransformOperations.Rotation.Validators;
namespace FootballGame.TransformOperations.Models {
    public class TransformAxis
    {
        protected TransformViewModel transformViewModel;

        public TransformAxis(TransformViewModel viewModel)
        {
            transformViewModel = viewModel;
        }
        public virtual List<KeyValuePair<string, bool>> RotateTowardsTarget() {
            return new List<KeyValuePair<string, bool>>();
        }
        public virtual List<KeyValuePair<string, bool>> TwistTowardsTarget()
        {
            return new List<KeyValuePair<string, bool>>();
        }
        public virtual PlaneViewModel RotateWithCheck(Vector3 expectedTarget) {
            return new PlaneViewModel() { };
        }
        public void InitializePlane() {
            Vector3 referenceVector = transformViewModel.GetPlaneDetails().referenceVectorDirection;
            Vector3 targetVector = transformViewModel.GetPlaneDetails().targetDirection;
            float deltaAngle = Vector3.SignedAngle(referenceVector, targetVector, Vector3.up);
            transformViewModel.SetPlaneDetails(UpdateRotationLimits(transformViewModel.GetPlaneDetails(), deltaAngle));
        }
        public bool SnapToDirection(Axis axisToRotate, Transform transformToRotate, Vector3 targetDirection)
        {
            Quaternion rotationToTarget = Quaternion.FromToRotation(axisToRotate == Axis.right ? transformToRotate.right : axisToRotate == Axis.forward ? transformToRotate.forward : axisToRotate == Axis.up ? transformToRotate.up : Vector3.zero, targetDirection);
            Quaternion rotationPerformed = rotationToTarget * transformToRotate.rotation;
            Vector3 axisRotated = axisToRotate == Axis.right ? rotationPerformed * Vector3.right : axisToRotate == Axis.forward ? rotationPerformed * Vector3.forward : axisToRotate == Axis.up ? rotationPerformed * Vector3.up : Vector3.zero;
            if (Mathf.Abs(axisRotated.normalized.x - targetDirection.normalized.x) <= 0.01f && Mathf.Abs(axisRotated.normalized.y - targetDirection.normalized.y) <= 0.01f && Mathf.Abs(axisRotated.normalized.z - targetDirection.normalized.z) <= 0.01f)
            {
                transformToRotate.rotation = rotationPerformed * transformToRotate.rotation;
                return true;
            }
            return false;
        }

        public Vector3 GetAxisVector(Axis sourceAxis, Transform sourceTransform)
        {
            return sourceAxis == Axis.right ? sourceTransform.right : sourceAxis == Axis.forward ? sourceTransform.forward : sourceAxis == Axis.up ? sourceTransform.up : sourceAxis == Axis.inverseRight ? -sourceTransform.right : sourceAxis == Axis.inverseForward ? -sourceTransform.forward : sourceAxis == Axis.inverseUp ? -sourceTransform.up : Vector3.zero;
        }


        public bool AlignUsingForwardAndUpAxes(Axis sourceAxis, Transform armToRotate, Vector3 targetDirection, float rotationQuantity, bool fullRotation)
        {
            Vector3 sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
            Vector3 localAxisOne = sourceAxis == Axis.inverseRight ? armToRotate.up : Vector3.zero;
            Vector3 localAxisTwo = sourceAxis == Axis.inverseRight ? armToRotate.forward : Vector3.zero;
            RotateTransformToDirection(sourceAxisVector, armToRotate, targetDirection, localAxisOne, rotationQuantity, fullRotation);
            sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
            RotateTransformToDirection(sourceAxisVector, armToRotate, targetDirection, localAxisTwo, rotationQuantity, fullRotation);
            sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
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
        public bool AlignUsingGlobalAndLocalAxes(Axis sourceAxis, Transform armToRotate, Vector3 targetDirection, float rotationQuantity, bool fullRotation)
        {
            Vector3 sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
            Vector3 localAxisOne = sourceAxis == Axis.inverseRight ? armToRotate.up : Vector3.zero;
            Vector3 localAxisTwo = sourceAxis == Axis.inverseRight ? armToRotate.forward : Vector3.zero;
            RotateTransformToDirection(sourceAxisVector, armToRotate, targetDirection, localAxisOne, rotationQuantity, fullRotation);
            sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
            RotateTransformToDirection(sourceAxisVector, armToRotate, targetDirection, localAxisTwo, rotationQuantity, fullRotation);
            sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
            RotateTransformToDirection(sourceAxisVector, armToRotate, targetDirection, Vector3.up, rotationQuantity, fullRotation);
            sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
            RotateTransformToDirection(sourceAxisVector, armToRotate, targetDirection, Vector3.right, rotationQuantity, fullRotation);
            sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
            RotateTransformToDirection(sourceAxisVector, armToRotate, targetDirection, Vector3.forward, rotationQuantity, fullRotation);
            sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);

            if (armToRotate.name.Contains("LeftArm")) {
                int j = 0;
            }
            if (Mathf.Abs(sourceAxisVector.normalized.x - targetDirection.normalized.x) <= 0.01f && Mathf.Abs(sourceAxisVector.normalized.y - targetDirection.normalized.y) <= 0.01f && Mathf.Abs(sourceAxisVector.normalized.z - targetDirection.normalized.z) <= 0.01f)
            {
                return true;
            }
            return false;
        }
        public void RotateTransformToDirection(Vector3 armToRotateDirection, Transform armToRotate, Vector3 analogAxisOfRotationInput3DVector, Vector3 rotationAxis, float angleMagnitude = 30, bool fullRotation = false)
        {
            Vector3 projectedForward = Vector3.ProjectOnPlane(armToRotateDirection, rotationAxis);
            Vector3 projectedTarget = Vector3.ProjectOnPlane(analogAxisOfRotationInput3DVector, rotationAxis);
            float angleForward = Vector3.SignedAngle(projectedForward, projectedTarget, rotationAxis);
            float amountToRotatePerFrame = angleMagnitude;
            if (fullRotation)
            {
                armToRotate.Rotate(rotationAxis, angleForward, Space.World);
            }
            else
            {
                if (Mathf.Abs(angleForward) > amountToRotatePerFrame)
                {
                    armToRotate.Rotate(rotationAxis, Mathf.Sign(angleForward) * amountToRotatePerFrame, Space.World);
                }
                else
                {
                    armToRotate.Rotate(rotationAxis, angleForward, Space.World);
                }
            }

        }

        PlaneViewModel UpdateRotationAmounts(PlaneViewModel planeDetails)
        {
            float yaw = Vector3.SignedAngle(
                planeDetails.referenceVectorDirection,
                planeDetails.targetDirection,
                planeDetails.plane);

            float horizontalAmount = 1 - Mathf.Abs(planeDetails.plane.normalized.x);
            float verticalAmount = 1 - Mathf.Abs(planeDetails.plane.normalized.y);
            planeDetails.currentRightRotation = Mathf.Max(yaw * planeDetails.counterClockwiseSign, 0) * horizontalAmount;
            planeDetails.currentLeftRotation = Mathf.Max(-yaw * planeDetails.counterClockwiseSign, 0) * horizontalAmount;

            planeDetails.currentUpRotation = Mathf.Max(yaw * planeDetails.counterClockwiseSign, 0) * verticalAmount;
            planeDetails.currentDownRotation = Mathf.Max(-yaw * planeDetails.counterClockwiseSign, 0) * verticalAmount;
            return planeDetails;
        }

        PlaneViewModel ConstructModelCopy(PlaneViewModel model) {
            return new PlaneViewModel
            {
                counterClockwiseSign = model.counterClockwiseSign,
                currentRightRotation = model.currentRightRotation,
                currentLeftRotation = model.currentLeftRotation,
                currentUpRotation = model.currentUpRotation,
                currentDownRotation = model.currentDownRotation,
                maxHorizontalRotationClockwise = model.maxHorizontalRotationClockwise,
                maxHorizontalRotationCounterClockwise = model.maxHorizontalRotationCounterClockwise,
                maxVerticalRotationClockwise = model.maxVerticalRotationClockwise,
                maxVerticalRotationCounterClockwise = model.maxVerticalRotationCounterClockwise,
                plane = model.plane,
                referenceVectorDirection = model.referenceVectorDirection,
                targetDirection = model.targetDirection
            };
            
        }

        /*public PlaneViewModel UpdateRotationLimits(PlaneViewModel model, float signedAngle)
        {
            PlaneViewModel modelCopy = ConstructModelCopy(model);
            // Determine how "horizontal" and "vertical" this plane is.
            float horizontalWeight = 1f - Mathf.Abs(model.plane.normalized.x);
            float verticalWeight = 1f - Mathf.Abs(model.plane.normalized.y);

            horizontalWeight = Mathf.Clamp01(horizontalWeight);
            verticalWeight = Mathf.Clamp01(verticalWeight);

            if (signedAngle >= 0f)
            {
                modelCopy.currentRightRotation = Mathf.Min(
                    signedAngle * horizontalWeight,
                    model.maxHorizontalRotationCounterClockwise);

                modelCopy.currentUpRotation = Mathf.Min(
                    signedAngle * verticalWeight,
                    model.maxVerticalRotationCounterClockwise);
            }
            else
            {
                float magnitude = Mathf.Abs(signedAngle);

                modelCopy.currentLeftRotation = Mathf.Min(
                    magnitude * horizontalWeight,
                    model.maxHorizontalRotationCounterClockwise);

                modelCopy.currentDownRotation = Mathf.Min(
                    magnitude * verticalWeight,
                    model.maxVerticalRotationClockwise);
            }

            return modelCopy;
        }*/

        public PlaneViewModel UpdateRotationLimits(PlaneViewModel model, float signedAngle)
        {
            PlaneViewModel modelCopy = ConstructModelCopy(model);
            // Determine how "horizontal" and "vertical" this plane is.
            float horizontalWeight = 1f - Mathf.Abs(model.plane.normalized.x);
            float verticalWeight = 1f - Mathf.Abs(model.plane.normalized.y);

            horizontalWeight = Mathf.Clamp01(horizontalWeight);
            verticalWeight = Mathf.Clamp01(verticalWeight);

            //IT NEEDS THE SIGN BECAUSE IT'S NOT ALWAYS ROTATING AROUND ONE OF THE REFERENCES TRANSFORM AXES
            //FOR EXAMPLE, WE ROTATE AROUND VECTOR3.UP OR VECTOR3.FORWARD FIRST
            if (signedAngle < 0f)
            {
                float rotationAmount = Mathf.Max((modelCopy.currentRightRotation + Mathf.Abs(signedAngle) * horizontalWeight) - modelCopy.currentLeftRotation, 0f);
                //modelCopy.currentRightRotation += (Mathf.Abs(signedAngle) * horizontalWeight);
                modelCopy.currentRightRotation = rotationAmount;
                rotationAmount = Mathf.Max((modelCopy.currentUpRotation + Mathf.Abs(signedAngle) * verticalWeight) - modelCopy.currentDownRotation, 0f);
                //modelCopy.currentUpRotation += Mathf.Abs(signedAngle) * verticalWeight;
                modelCopy.currentUpRotation = rotationAmount;
                if (modelCopy.currentLeftRotation + (-Mathf.Abs(signedAngle) * horizontalWeight) > 0) {
                    modelCopy.currentLeftRotation += -Mathf.Abs(signedAngle) * horizontalWeight;
                } else {
                    modelCopy.currentLeftRotation = 0f;
                }

                if (modelCopy.currentDownRotation + (-Mathf.Abs(signedAngle) * verticalWeight) > 0) {
                    modelCopy.currentDownRotation += -Mathf.Abs(signedAngle) * verticalWeight;
                } else {
                    modelCopy.currentDownRotation = 0f;
                }
                    
            }
            else
            {
                float magnitude = Mathf.Abs(signedAngle);
                float rotationAmount = Mathf.Max((modelCopy.currentLeftRotation + magnitude * horizontalWeight) - modelCopy.currentRightRotation, 0f);
                //modelCopy.currentLeftRotation += magnitude * horizontalWeight;
                modelCopy.currentLeftRotation = rotationAmount;
                rotationAmount = Mathf.Max((modelCopy.currentDownRotation + magnitude * verticalWeight) - modelCopy.currentUpRotation, 0f);
                //modelCopy.currentDownRotation += magnitude * verticalWeight;
                modelCopy.currentDownRotation = rotationAmount;
                if (modelCopy.currentRightRotation + (-magnitude * horizontalWeight) > 0)
                {
                    modelCopy.currentRightRotation += -magnitude * horizontalWeight;
                }
                else
                {
                    modelCopy.currentRightRotation = 0f;
                }

                if (modelCopy.currentUpRotation + (-magnitude * verticalWeight) > 0)
                {
                    modelCopy.currentUpRotation +=-magnitude * verticalWeight;
                }
                else
                {
                    modelCopy.currentUpRotation = 0f;
                }
            }

            return modelCopy;
        }

        public float GetUnitCircleEulerAngle(float angle)
        {
            float normalizedAngle = angle > 0 ? angle % 360 : (360 + angle) % 360;
            return normalizedAngle;
        }

        public float GetLeverArmAngle(Vector2 movementInput)
        {
            float angle = 0f;
            //Q1
            if (movementInput.x >= 0 && movementInput.y >= 0)
            {
                angle = Mathf.Atan(movementInput.y / movementInput.x) * Mathf.Rad2Deg;
            }
            //Q3
            else if (movementInput.x < 0 && movementInput.y < 0)
            {
                angle = 180 + Mathf.Atan(movementInput.y / movementInput.x) * Mathf.Rad2Deg;
            }
            //Q2
            else if (movementInput.x < 0 && movementInput.y >= 0)
            {
                angle = 180 + Mathf.Atan(movementInput.y / movementInput.x) * Mathf.Rad2Deg;
            }
            //Q4
            else if (movementInput.x > 0 && movementInput.y < 0)
            {
                angle = 360 + Mathf.Atan(movementInput.y / movementInput.x) * Mathf.Rad2Deg;
            }

            return angle;
        }

        private bool FitsConstraints(PlaneViewModel model, float signedAngle) {
            PlaneViewModel expectedModel = UpdateRotationLimits(model, signedAngle);
            return expectedModel.currentRightRotation <= expectedModel.maxHorizontalRotationCounterClockwise &&
                expectedModel.currentLeftRotation <= expectedModel.maxHorizontalRotationClockwise &&
                expectedModel.currentUpRotation <= expectedModel.maxVerticalRotationCounterClockwise &&
                expectedModel.currentDownRotation <= expectedModel.maxVerticalRotationClockwise;
        }

        public static bool IsAngleBetween(
float angle,
float start,
float end)
        {
            angle = (angle + 360f) % 360f;
            start = (start + 360f) % 360f;
            end = (end + 360f) % 360f;

            if (start <= end)
                return angle >= start &&
                       angle <= end;

            return angle >= start ||
                   angle <= end;
        }

        bool Fits(Axis axis, float candidateAngle, Transform source, PlaneViewModel planeDetails)
        {
            Vector3 sDir = axis == Axis.inverseRight ? -source.right : Vector3.zero;
            Vector3 rotated =
                Quaternion.AngleAxis(
                    candidateAngle,
                    planeDetails.plane) *
                (sDir);

            /*Vector3 localDirection =
                transformViewModel.GetRootTransform()
                    .InverseTransformDirection(rotated)
                    .normalized;*/

            //Vector3 localDirection = sDir;
            Vector3 customForward = transformViewModel.GetRootTransform().forward;
            customForward = new Vector3(customForward.x, 0f, customForward.z);
            Vector3 customRight = -Vector3.Cross(Vector3.up, customForward);
            Vector3 customUp = Vector3.Cross(customRight, customForward);

            Vector3 localDirection = ToCustomLocal(rotated, customRight, customUp, customForward);
            // Compute horizontal angle

            // Compute vertical angle
            Vector2 mInput = transformViewModel.GetAnalogStickInput();
            mInput = new Vector2(mInput.x, Mathf.Abs(mInput.x) < Mathf.Abs(mInput.y) ? Mathf.Sign(mInput.y) * Mathf.Abs(mInput.x) : mInput.y);
            //THIS IS FOR WHEN INPUT IS (0, 1), AND THUS 3D INPUT IS (0, 1, 0) -> 2D HORIZONTAL VEC = (0, 0)
            if (mInput.magnitude < 0.01f) {
                mInput = new Vector2(0, 1);
            }
            Vector2 horizontalVector = new Vector2(mInput.x, mInput.y);
            Vector2 rootVector = new Vector2(customForward.x, customForward.z);
            float angleBetween = Vector2.Angle(rootVector, horizontalVector);
            float horizontalAngle = GetUnitCircleEulerAngle(Mathf.Atan2(horizontalVector.y, horizontalVector.x) * Mathf.Rad2Deg);
            Vector2 verticalVector = new Vector2(localDirection.z, localDirection.y);
            float verticalAngle = GetUnitCircleEulerAngle(Mathf.Atan2(verticalVector.y, verticalVector.x) * Mathf.Rad2Deg);

            bool horizontalOK = IsAngleBetween(horizontalAngle, 270f, 90f);
            bool verticalOK = IsAngleBetween(verticalAngle, 90f, 90f);
            return horizontalOK &&
                   verticalOK;
        }

        public static Vector3 ToCustomLocal(
        Vector3 worldDirection,
        Vector3 customRight,
        Vector3 customUp,
        Vector3 customForward)
        {
            return new Vector3(
                Vector3.Dot(worldDirection, customRight),
                Vector3.Dot(worldDirection, customUp),
                Vector3.Dot(worldDirection, customForward)
            );
        }

        public float DetermineSignedAngle(PlaneViewModel planeDetails, Vector3 forward, Vector3 target, Vector3 rotationAxis) {
            Vector3 projectedForward = Vector3.ProjectOnPlane(forward, rotationAxis);
            Vector3 projectedTarget = Vector3.ProjectOnPlane(target, rotationAxis);
            float angleForward = Vector3.SignedAngle(projectedForward, projectedTarget, rotationAxis);
            float oppositeAngle = angleForward >= 0 ? angleForward - 360f : angleForward + 360f;
            bool originalFitsConstraints = false;
            bool oppositeFitsConstraints = false;
            if (angleForward < 0)
            {
                originalFitsConstraints = Mathf.Sign(planeDetails.counterClockwiseSign) == Mathf.Sign(angleForward) ? planeDetails.horizontalRotation + angleForward >= Mathf.Sign(angleForward) * Mathf.Abs(planeDetails.maxHorizontalRotationCounterClockwise) : planeDetails.horizontalRotation + angleForward >= Mathf.Sign(angleForward) * Mathf.Abs(planeDetails.maxHorizontalRotationClockwise);
                oppositeFitsConstraints = Mathf.Sign(planeDetails.counterClockwiseSign) == Mathf.Sign(oppositeAngle) ? planeDetails.horizontalRotation + oppositeAngle <= Mathf.Sign(oppositeAngle) * Mathf.Abs(planeDetails.maxHorizontalRotationCounterClockwise) : planeDetails.horizontalRotation + oppositeAngle <= Mathf.Sign(oppositeAngle) * Mathf.Abs(planeDetails.maxHorizontalRotationClockwise);
            }
            else
            {
                originalFitsConstraints = Mathf.Sign(planeDetails.counterClockwiseSign) == Mathf.Sign(angleForward) ? planeDetails.horizontalRotation + angleForward <= Mathf.Sign(angleForward) * Mathf.Abs(planeDetails.maxHorizontalRotationCounterClockwise) : planeDetails.horizontalRotation + angleForward <= Mathf.Sign(angleForward) * Mathf.Abs(planeDetails.maxHorizontalRotationClockwise);
                oppositeFitsConstraints = Mathf.Sign(planeDetails.counterClockwiseSign) == Mathf.Sign(oppositeAngle) ? planeDetails.horizontalRotation + oppositeAngle >= Mathf.Sign(oppositeAngle) * Mathf.Abs(planeDetails.maxHorizontalRotationCounterClockwise) : planeDetails.horizontalRotation + oppositeAngle >= Mathf.Sign(oppositeAngle) * Mathf.Abs(planeDetails.maxHorizontalRotationClockwise);
            }
            planeDetails.plane = rotationAxis;
            float rotationAmount = 0f;
            if (originalFitsConstraints)
            {
                rotationAmount = angleForward;
            }
            if (oppositeFitsConstraints)
            {
                if (Mathf.Abs(angleForward) > Mathf.Abs(oppositeAngle) || !originalFitsConstraints)
                {
                    rotationAmount = oppositeAngle;
                }
            }
            return rotationAmount;
        }
        public PlaneViewModel RotateTransformToDirection(PlaneViewModel planeDetails, Vector3 armToRotateDirection, Transform armToRotate, Vector3 analogAxisOfRotationInput3DVector, Vector3 expectedTarget, Vector3 rotationAxis, float angleMagnitude = 30, bool fullRotation = false, Axis forwardAxis = Axis.inverseRight)
        {
            Vector3 projectedForward = Vector3.ProjectOnPlane(armToRotateDirection, rotationAxis);
            Vector3 projectedTarget = Vector3.ProjectOnPlane(analogAxisOfRotationInput3DVector, rotationAxis);
            Vector3 projectedExpectedTarget = Vector3.ProjectOnPlane(expectedTarget, rotationAxis);
            if (projectedForward.sqrMagnitude > 0.05f && projectedTarget.sqrMagnitude > 0.05f) {
                float angleForward = Vector3.SignedAngle(projectedForward, projectedTarget, rotationAxis);
                float oppositeAngle = angleForward >= 0 ? angleForward - 360f : angleForward + 360f;
                float expectedAngleForward = Vector3.SignedAngle(projectedForward, projectedExpectedTarget, rotationAxis);
                float expectedOppositeAngle = expectedAngleForward >= 0 ? expectedAngleForward - 360f : expectedAngleForward + 360f;
                planeDetails.plane = rotationAxis;
                float rotationAmount = angleForward;
                float amountToRotatePerFrame = angleMagnitude;
                float amountRotated = 0f;
                Vector3 horiontalDirectionBefore = armToRotateDirection;
                if (fullRotation)
                {
                    armToRotate.Rotate(rotationAxis, rotationAmount, Space.World);
                }
                else
                {
                    if (Mathf.Abs(rotationAmount) > amountToRotatePerFrame)
                    {
                        armToRotate.Rotate(rotationAxis, Mathf.Sign(rotationAmount) * amountToRotatePerFrame, Space.World);
                    }
                    else
                    {
                        armToRotate.Rotate(rotationAxis, rotationAmount, Space.World);
                    }
                }
            }
            return planeDetails;

        }
        public Tuple<bool, bool> DetermineIfAngleAndReverseFit(PlaneViewModel planeDetails, Vector3 rotationAxis, float angleForward, float expectedAngleForward, float expectedOppositeAngle, bool usingForwardAxis, bool usingRightAxis) {
            float horizontalMagnitude = Mathf.Abs(rotationAxis.normalized.y);
            float verticalMagnitude = 1 - Mathf.Abs(rotationAxis.normalized.y);
            bool originalFitsContraints = false;
            bool oppositeFitsContraints = false;
            if (usingRightAxis)
            {
                if (angleForward < 0)
                {
                    originalFitsContraints = Mathf.Sign(planeDetails.counterClockwiseSign) == Mathf.Sign(expectedAngleForward) ? planeDetails.verticalRotationRight + (expectedAngleForward * verticalMagnitude) >= Mathf.Sign(expectedAngleForward) * Mathf.Abs(planeDetails.maxVerticalRotationCounterClockwiseRight) : planeDetails.verticalRotationRight + (expectedAngleForward * verticalMagnitude) >= Mathf.Sign(expectedAngleForward) * Mathf.Abs(planeDetails.maxVerticalRotationClockwiseRight);
                    oppositeFitsContraints = Mathf.Sign(planeDetails.counterClockwiseSign) == Mathf.Sign(expectedOppositeAngle) ? planeDetails.verticalRotationRight + expectedOppositeAngle <= Mathf.Sign(expectedOppositeAngle) * Mathf.Abs(planeDetails.maxVerticalRotationCounterClockwiseRight) : planeDetails.verticalRotationRight + expectedOppositeAngle <= Mathf.Sign(expectedOppositeAngle) * Mathf.Abs(planeDetails.maxVerticalRotationClockwiseRight);
                }
                else
                {
                    originalFitsContraints = Mathf.Sign(planeDetails.counterClockwiseSign) == Mathf.Sign(expectedAngleForward) ? planeDetails.verticalRotationRight + (expectedAngleForward * verticalMagnitude) <= Mathf.Sign(expectedAngleForward) * Mathf.Abs(planeDetails.maxVerticalRotationCounterClockwiseRight) : planeDetails.verticalRotationRight + (expectedAngleForward * verticalMagnitude) <= Mathf.Sign(expectedAngleForward) * Mathf.Abs(planeDetails.maxVerticalRotationClockwiseRight);
                    oppositeFitsContraints = Mathf.Sign(planeDetails.counterClockwiseSign) == Mathf.Sign(expectedOppositeAngle) ? planeDetails.verticalRotationRight + expectedOppositeAngle >= Mathf.Sign(expectedOppositeAngle) * Mathf.Abs(planeDetails.maxVerticalRotationCounterClockwiseRight) : planeDetails.verticalRotationRight + expectedOppositeAngle >= Mathf.Sign(expectedOppositeAngle) * Mathf.Abs(planeDetails.maxVerticalRotationClockwiseRight);
                }
            }
            else if (usingForwardAxis)
            {

                if (angleForward < 0)
                {
                    originalFitsContraints = Mathf.Sign(planeDetails.counterClockwiseSign) == Mathf.Sign(expectedAngleForward) ? planeDetails.verticalRotation + (expectedAngleForward * verticalMagnitude) >= Mathf.Sign(expectedAngleForward) * Mathf.Abs(planeDetails.maxVerticalRotationCounterClockwise) : planeDetails.verticalRotation + (expectedAngleForward * verticalMagnitude) >= Mathf.Sign(expectedAngleForward) * Mathf.Abs(planeDetails.maxVerticalRotationClockwise);
                    oppositeFitsContraints = Mathf.Sign(planeDetails.counterClockwiseSign) == Mathf.Sign(expectedOppositeAngle) ? planeDetails.verticalRotation + expectedOppositeAngle <= Mathf.Sign(expectedOppositeAngle) * Mathf.Abs(planeDetails.maxVerticalRotationCounterClockwise) : planeDetails.verticalRotation + expectedOppositeAngle <= Mathf.Sign(expectedOppositeAngle) * Mathf.Abs(planeDetails.maxVerticalRotationClockwise);
                }
                else
                {
                    originalFitsContraints = Mathf.Sign(planeDetails.counterClockwiseSign) == Mathf.Sign(expectedAngleForward) ? planeDetails.verticalRotation + (expectedAngleForward * verticalMagnitude) <= Mathf.Sign(expectedAngleForward) * Mathf.Abs(planeDetails.maxVerticalRotationCounterClockwise) : planeDetails.verticalRotation + (expectedAngleForward * verticalMagnitude) <= Mathf.Sign(expectedAngleForward) * Mathf.Abs(planeDetails.maxVerticalRotationClockwise);
                    oppositeFitsContraints = Mathf.Sign(planeDetails.counterClockwiseSign) == Mathf.Sign(expectedOppositeAngle) ? planeDetails.verticalRotation + expectedOppositeAngle >= Mathf.Sign(expectedOppositeAngle) * Mathf.Abs(planeDetails.maxVerticalRotationCounterClockwise) : planeDetails.verticalRotation + expectedOppositeAngle >= Mathf.Sign(expectedOppositeAngle) * Mathf.Abs(planeDetails.maxVerticalRotationClockwise);
                }
            }
            else
            {
                if (angleForward < 0)
                {
                    originalFitsContraints = Mathf.Sign(planeDetails.counterClockwiseSign) == Mathf.Sign(expectedAngleForward) ? planeDetails.horizontalRotation + (expectedAngleForward * horizontalMagnitude) >= Mathf.Sign(expectedAngleForward) * Mathf.Abs(planeDetails.maxHorizontalRotationCounterClockwise) : planeDetails.horizontalRotation + (expectedAngleForward * horizontalMagnitude) >= Mathf.Sign(expectedAngleForward) * Mathf.Abs(planeDetails.maxHorizontalRotationClockwise);
                    oppositeFitsContraints = Mathf.Sign(planeDetails.counterClockwiseSign) == Mathf.Sign(expectedOppositeAngle) ? planeDetails.horizontalRotation + expectedOppositeAngle <= Mathf.Sign(expectedOppositeAngle) * Mathf.Abs(planeDetails.maxHorizontalRotationCounterClockwise) : planeDetails.horizontalRotation + expectedOppositeAngle <= Mathf.Sign(expectedOppositeAngle) * Mathf.Abs(planeDetails.maxHorizontalRotationClockwise);
                }
                else
                {
                    originalFitsContraints = Mathf.Sign(planeDetails.counterClockwiseSign) == Mathf.Sign(expectedAngleForward) ? planeDetails.horizontalRotation + (expectedAngleForward * horizontalMagnitude) <= Mathf.Sign(expectedAngleForward) * Mathf.Abs(planeDetails.maxHorizontalRotationCounterClockwise) : planeDetails.horizontalRotation + (expectedAngleForward * horizontalMagnitude) <= Mathf.Sign(expectedAngleForward) * Mathf.Abs(planeDetails.maxHorizontalRotationClockwise);
                    oppositeFitsContraints = Mathf.Sign(planeDetails.counterClockwiseSign) == Mathf.Sign(expectedOppositeAngle) ? planeDetails.horizontalRotation + expectedOppositeAngle >= Mathf.Sign(expectedOppositeAngle) * Mathf.Abs(planeDetails.maxHorizontalRotationCounterClockwise) : planeDetails.horizontalRotation + expectedOppositeAngle >= Mathf.Sign(expectedOppositeAngle) * Mathf.Abs(planeDetails.maxHorizontalRotationClockwise);
                }
            }
            return new Tuple<bool, bool>(originalFitsContraints, oppositeFitsContraints);
        }
        /// <summary>
        /// Records the arm's current rotation into planeDetails from where the source axis
        /// (-arm.right by default) actually points, measured in the root's frame
        /// (root forward flattened, Vector3.up, and the matching right).
        ///
        /// Decomposition (yaw first, then pitch inside the yaw):
        ///   horizontalRotation   = yaw around up, measured from the rest direction (-root.right).
        ///                          Clockwise (towards the front) is positive.
        ///   total pitch v        = pitch inside that yaw. Raising the arm is negative; going over
        ///                          the top past straight up goes beyond -90.
        ///   verticalRotation     = v * cos^2(yaw)  -> the part of the pitch in the Vector3.forward plane
        ///   verticalRotationRight= v * sin^2(yaw)  -> the part of the pitch in the Vector3.right plane
        ///   (so verticalRotation + verticalRotationRight == v)
        ///
        /// Every direction has two valid (yaw, pitch) readings: (h, v) and (h + 180, over-the-top v).
        /// The one closest to the previously recorded values wins (inside the horizontal and vertical
        /// limits), which is what keeps 180 from flipping to -180 and tells "behind the head" apart
        /// from "out in front".
        /// </summary>
        public void SyncRecordedRotation(PlaneViewModel planeDetails, Transform arm, Axis sourceAxis = Axis.inverseRight)
        {
            if (planeDetails == null || arm == null)
            {
                return;
            }
            Vector3 direction = GetAxisVector(sourceAxis, arm);
            if (direction.sqrMagnitude < 1e-6f)
            {
                return;
            }
            GetRootBasis(out Vector3 rootForward, out Vector3 rootUp, out Vector3 rootRight);

            float previousHorizontal = planeDetails.horizontalRotation;
            float previousVertical = GetTotalVerticalRotation(planeDetails);

            GetYawPitchReadings(direction, rootForward, rootUp, rootRight, previousHorizontal,
                out float headingInFront, out float verticalInFront, out float headingOverTop, out float verticalOverTop);

            float horizontalInFront = UnwrapAngleNear(headingInFront, previousHorizontal);
            float horizontalOverTop = UnwrapAngleNear(headingOverTop, previousHorizontal);
            float costInFront = RotationReadingCost(planeDetails, horizontalInFront, verticalInFront, previousHorizontal, previousVertical);
            float costOverTop = RotationReadingCost(planeDetails, horizontalOverTop, verticalOverTop, previousHorizontal, previousVertical);
            if (costInFront <= costOverTop)
            {
                WriteRecordedRotation(planeDetails, horizontalInFront, verticalInFront);
            }
            else
            {
                WriteRecordedRotation(planeDetails, horizontalOverTop, verticalOverTop);
            }
        }

        /// <summary>Total pitch inside the yaw (raising the arm is negative).</summary>
        public static float GetTotalVerticalRotation(PlaneViewModel planeDetails)
        {
            return planeDetails.verticalRotation + planeDetails.verticalRotationRight;
        }

        /// <summary>
        /// Stores a (yaw, pitch) reading. The pitch is split between the two planes:
        /// verticalRotation gets cos^2(yaw) of it (Vector3.forward plane, arm out to the side),
        /// verticalRotationRight gets sin^2(yaw) of it (Vector3.right plane, arm out in front).
        /// </summary>
        public static void WriteRecordedRotation(PlaneViewModel planeDetails, float horizontal, float vertical)
        {
            float forwardWeight = ForwardPlaneWeight(horizontal);
            planeDetails.horizontalRotation = horizontal;
            planeDetails.verticalRotation = vertical * forwardWeight;
            planeDetails.verticalRotationRight = vertical * (1f - forwardWeight);
            planeDetails.gonePastForwardTopLimit = planeDetails.verticalRotation < -90f;
            planeDetails.gonePastRightTopLimit = planeDetails.verticalRotationRight < -90f;
        }

        static float ForwardPlaneWeight(float horizontal)
        {
            float c = Mathf.Cos(horizontal * Mathf.Deg2Rad);
            return c * c;
        }

        /// <summary>Root frame: flattened root forward, world up, and the matching right.</summary>
        protected void GetRootBasis(out Vector3 rootForward, out Vector3 rootUp, out Vector3 rootRight)
        {
            Transform root = transformViewModel != null ? transformViewModel.GetRootTransform() : null;
            rootForward = root != null ? Vector3.ProjectOnPlane(root.forward, Vector3.up) : Vector3.forward;
            if (rootForward.sqrMagnitude < 1e-6f)
            {
                rootForward = Vector3.forward;
            }
            rootForward.Normalize();
            rootUp = Vector3.up;
            rootRight = Vector3.Cross(rootUp, rootForward);
        }

        /// <summary>
        /// Rotation that takes the rest direction (-rootRight) to (yaw, pitch):
        /// pitch around rootForward first, then yaw around rootUp.
        /// </summary>
        public static Quaternion YawPitchRotation(float horizontal, float vertical, Vector3 rootForward, Vector3 rootUp)
        {
            return Quaternion.AngleAxis(horizontal, rootUp) * Quaternion.AngleAxis(vertical, rootForward);
        }

        public static Vector3 DirectionFromYawPitch(float horizontal, float vertical, Vector3 rootForward, Vector3 rootUp, Vector3 rootRight)
        {
            return YawPitchRotation(horizontal, vertical, rootForward, rootUp) * -rootRight;
        }

        /// <summary>
        /// The two (heading, pitch) readings of a direction: "in front" and "over the top" (heading + 180).
        /// Headings are not unwrapped. When the direction is straight up/down the heading is undefined,
        /// so fallbackHeading is used.
        /// </summary>
        public static void GetYawPitchReadings(Vector3 direction, Vector3 rootForward, Vector3 rootUp, Vector3 rootRight, float fallbackHeading,
            out float headingInFront, out float verticalInFront, out float headingOverTop, out float verticalOverTop)
        {
            direction.Normalize();
            float x = Vector3.Dot(direction, rootRight);
            float y = Vector3.Dot(direction, rootUp);
            float z = Vector3.Dot(direction, rootForward);

            float elevation = Mathf.Asin(Mathf.Clamp(y, -1f, 1f)) * Mathf.Rad2Deg;
            verticalInFront = -elevation;
            verticalOverTop = verticalInFront < 0f ? -180f - verticalInFront : 180f - verticalInFront;

            if (Mathf.Sqrt(x * x + z * z) < 0.001f)
            {
                headingInFront = fallbackHeading;
                headingOverTop = fallbackHeading;
                verticalOverTop = verticalInFront;
            }
            else
            {
                // 0 at rest (-root.right), +90 straight in front, 180 at +root.right through the front.
                headingInFront = Mathf.Atan2(z, -x) * Mathf.Rad2Deg;
                headingOverTop = headingInFront + 180f;
            }
        }

        public static float UnwrapAngleNear(float angle, float reference)
        {
            return angle + 360f * Mathf.Round((reference - angle) / 360f);
        }

        /// <summary>Allowed horizontalRotation range, e.g. [-45, 225].</summary>
        public static void GetHorizontalLimits(PlaneViewModel planeDetails, out float min, out float max)
        {
            float counterClockwiseSign = CounterClockwiseSign(planeDetails);
            float counterClockwiseLimit = counterClockwiseSign * Mathf.Abs(planeDetails.maxHorizontalRotationCounterClockwise);
            float clockwiseLimit = -counterClockwiseSign * Mathf.Abs(planeDetails.maxHorizontalRotationClockwise);
            min = Mathf.Min(counterClockwiseLimit, clockwiseLimit);
            max = Mathf.Max(counterClockwiseLimit, clockwiseLimit);
            if (min == 0f && max == 0f)
            {
                min = -180f;
                max = 180f;
            }
        }

        /// <summary>
        /// Allowed total pitch at a given yaw. Out to the side the Vector3.forward-plane limits apply,
        /// out in front the Vector3.right-plane limits apply, blended by cos^2 / sin^2 in between.
        /// e.g. up to -135 (45 past straight up) and down to +90 at the side, +135 in front.
        /// </summary>
        public static void GetVerticalLimits(PlaneViewModel planeDetails, float horizontal, out float min, out float max)
        {
            float counterClockwiseSign = CounterClockwiseSign(planeDetails);
            float forwardUp = Mathf.Abs(planeDetails.maxVerticalRotationCounterClockwise);
            float forwardDown = Mathf.Abs(planeDetails.maxVerticalRotationClockwise);
            float rightUp = planeDetails.maxVerticalRotationCounterClockwiseRight != 0f ? Mathf.Abs(planeDetails.maxVerticalRotationCounterClockwiseRight) : forwardUp;
            float rightDown = planeDetails.maxVerticalRotationClockwiseRight != 0f ? Mathf.Abs(planeDetails.maxVerticalRotationClockwiseRight) : forwardDown;
            if (forwardUp == 0f && forwardDown == 0f)
            {
                forwardUp = rightUp;
                forwardDown = rightDown;
            }
            if (forwardUp == 0f && forwardDown == 0f)
            {
                min = -180f;
                max = 180f;
                return;
            }
            float forwardWeight = ForwardPlaneWeight(horizontal);
            float up = forwardUp * forwardWeight + rightUp * (1f - forwardWeight);
            float down = forwardDown * forwardWeight + rightDown * (1f - forwardWeight);
            float counterClockwiseLimit = counterClockwiseSign * up;
            float clockwiseLimit = -counterClockwiseSign * down;
            min = Mathf.Min(counterClockwiseLimit, clockwiseLimit);
            max = Mathf.Max(counterClockwiseLimit, clockwiseLimit);
        }

        static float CounterClockwiseSign(PlaneViewModel planeDetails)
        {
            return planeDetails.counterClockwiseSign != 0f ? Mathf.Sign(planeDetails.counterClockwiseSign) : -1f;
        }

        // Lower is better: distance from the previous reading, plus a large penalty for
        // readings outside the horizontal / vertical limits.
        static float RotationReadingCost(PlaneViewModel planeDetails, float horizontal, float vertical, float previousHorizontal, float previousVertical)
        {
            float deltaHorizontal = horizontal - previousHorizontal;
            float deltaVertical = vertical - previousVertical;
            float cost = deltaHorizontal * deltaHorizontal + deltaVertical * deltaVertical;
            GetHorizontalLimits(planeDetails, out float minHorizontal, out float maxHorizontal);
            if (horizontal < minHorizontal - 1f || horizontal > maxHorizontal + 1f)
            {
                cost += 1e6f;
            }
            GetVerticalLimits(planeDetails, horizontal, out float minVertical, out float maxVertical);
            if (vertical < minVertical - 1f || vertical > maxVertical + 1f)
            {
                cost += 1e6f;
            }
            return cost;
        }

        public Tuple<PlaneViewModel, bool> RotateTransformWithConstraints(PlaneViewModel planeDetails, Vector3 armToRotateDirection, Vector3 originalArmToRotateDirection, Vector3 originalPivotAxisDirection, Transform armToRotate, Vector3 analogAxisOfRotationInput3DVector, Vector3 expectedTarget, Vector3 rotationAxis, float angleMagnitude = 30, bool fullRotation = false, Axis forwardAxis = Axis.inverseRight)
        {
            bool useForward = false;
            bool useRight = false;
            //bool useVerticalAxis = (rotationAxis - armToRotate.forward).sqrMagnitude <= 0.01f;
            bool useHorizontalAxis = (rotationAxis - Vector3.up).sqrMagnitude <= 0.01f;
            bool useVerticalAxis = (rotationAxis - armToRotate.forward).sqrMagnitude <= 0.01f ||
                (rotationAxis - Vector3.forward).sqrMagnitude <= 0.01f ||
                (rotationAxis - Vector3.right).sqrMagnitude <= 0.01f ||
                (rotationAxis - -Vector3.forward).sqrMagnitude <= 0.01f ||
                (rotationAxis - -Vector3.right).sqrMagnitude <= 0.01f;
            Vector3 verticalRotationAxis = new Vector3(0, 0, 0);
            if ((rotationAxis - armToRotate.forward).sqrMagnitude <= 0.01f &&
                Vector3.ProjectOnPlane(armToRotateDirection, new Vector3(armToRotate.forward.x, 0, armToRotate.forward.z).normalized).sqrMagnitude > 0.01f)
            {
                useForward = true;
                verticalRotationAxis = new Vector3(armToRotate.forward.x, 0, armToRotate.forward.z).normalized;
            }
            else if ((rotationAxis - Vector3.forward).sqrMagnitude <= 0.01f &&
                Vector3.ProjectOnPlane(armToRotateDirection, Vector3.forward).sqrMagnitude > 0.01f)
            {
                useForward = true;
                verticalRotationAxis = Vector3.forward;
            }
            else if ((rotationAxis - -Vector3.forward).sqrMagnitude <= 0.01f &&
                Vector3.ProjectOnPlane(armToRotateDirection, -Vector3.forward).sqrMagnitude > 0.01f)
            {
                useForward = true;
                verticalRotationAxis = -Vector3.forward;
            }
            else if ((rotationAxis - Vector3.right).sqrMagnitude <= 0.01f &&
                Vector3.ProjectOnPlane(armToRotateDirection, Vector3.right).sqrMagnitude > 0.01f)
            {
                useRight = true;
                verticalRotationAxis = Vector3.right;
            }
            else if ((rotationAxis - -Vector3.right).sqrMagnitude <= 0.01f &&
                Vector3.ProjectOnPlane(armToRotateDirection, -Vector3.right).sqrMagnitude > 0.01f)
            {
                useRight = true;
                verticalRotationAxis = -Vector3.right;
            }
            else {
                useVerticalAxis = false;
            }
            //bool useVerticalAxis = (rotationAxis - new Vector3(armToRotate.forward.x, 0f, armToRotate.forward.z).normalized).sqrMagnitude <= 0.01f;
            Vector3 projectedForward = Vector3.ProjectOnPlane(armToRotateDirection, rotationAxis);
            Vector3 projectedTarget = Vector3.ProjectOnPlane(analogAxisOfRotationInput3DVector, rotationAxis);
            Vector3 projectedExpectedTarget = Vector3.ProjectOnPlane(expectedTarget, rotationAxis);
            float horizontalMagnitude = Mathf.Abs(rotationAxis.normalized.y);
            float verticalMagnitude = 1 - Mathf.Abs(rotationAxis.normalized.y);
            bool originalFitsContraints = true;
            bool originalVerticalFitsContraints = false;
            Tuple<bool, bool> constraintsCheck = new Tuple<bool, bool>(false, false);
            bool oppositeFitsContraints = true;
            float horizontalMultiplier = Mathf.Abs(rotationAxis.normalized.y);
            float horizontalRotationSign = Mathf.Sign(DetermineSignedAngle(planeDetails, armToRotateDirection, analogAxisOfRotationInput3DVector, Vector3.up));
            bool ableToRotate = true;
            bool usingRightAxis = (rotationAxis.normalized - Vector3.right).magnitude <= 0.01f || (rotationAxis.normalized - -Vector3.right).magnitude <= 0.01f;
            bool usingForwardAxis = (rotationAxis.normalized - Vector3.forward).magnitude <= 0.01f || (rotationAxis.normalized - -Vector3.forward).magnitude <= 0.01f;
            //Vector3 horizontalDirectionBefore = originalArmToRotateDirection;
            Vector3 horizontalDirectionBefore = armToRotateDirection;
            Vector3 projectedBefore = Vector3.ProjectOnPlane(horizontalDirectionBefore, originalPivotAxisDirection);
            Vector3 projectedAfter = Vector3.zero;
            
            if (projectedForward.sqrMagnitude > 0.99f && projectedTarget.sqrMagnitude > 0.99f)
            {
                float angleForward = Vector3.SignedAngle(projectedForward, projectedTarget, rotationAxis);
                float oppositeAngle = angleForward >= 0 ? angleForward - 360f : angleForward + 360f;
                float expectedAngleForward = Vector3.SignedAngle(projectedForward, projectedExpectedTarget, rotationAxis);
                float expectedOppositeAngle = expectedAngleForward >= 0 ? expectedAngleForward - 360f : expectedAngleForward + 360f;
                Quaternion rotation = Quaternion.AngleAxis(angleForward, rotationAxis);

                Quaternion fQ = rotation * armToRotate.rotation;
                Vector3 fRight = fQ * Vector3.up;
                Vector3 eInverseRight = -fRight;

                float hAngle = Vector3.SignedAngle(armToRotateDirection, eInverseRight, Vector3.up);
                float vAngle = Vector3.SignedAngle(armToRotateDirection, eInverseRight, Vector3.forward);
                float vAngleRecorded = angleForward;
                
                /*if (useForward && angleForward < 0f && ((planeDetails.verticalRotation) < -90f || planeDetails.gonePastForwardTopLimit))
                {
                    planeDetails.gonePastForwardTopLimit = true;
                    vAngleRecorded *= -1f;
                    expectedAngleForward = Mathf.Sign(vAngleRecorded) * Mathf.Abs(expectedAngleForward);
                    constraintsCheck = DetermineIfAngleAndReverseFit(planeDetails, rotationAxis, vAngleRecorded, expectedAngleForward, expectedOppositeAngle);
                }
                else {
                    if ((planeDetails.verticalRotation) >= -90f) {
                        planeDetails.gonePastForwardTopLimit = false;
                    }
                    constraintsCheck = DetermineIfAngleAndReverseFit(planeDetails, rotationAxis, angleForward, expectedAngleForward, expectedOppositeAngle);
                }*/
                constraintsCheck = DetermineIfAngleAndReverseFit(planeDetails, rotationAxis, angleForward, expectedAngleForward, expectedOppositeAngle,useForward, useRight);

                planeDetails.plane = rotationAxis;
                //ableToRotate = originalFitsContraints || oppositeFitsContraints;
                ableToRotate = constraintsCheck.Item1 || constraintsCheck.Item2;


                float rotationAmount = 0f;
                bool useOriginal = false;
                //if (originalFitsContraints)
                if (constraintsCheck.Item1)
                {
                    rotationAmount = angleForward;
                }
                //if (oppositeFitsContraints)
                if (constraintsCheck.Item2)
                {
                    if (Mathf.Abs(angleForward) > Mathf.Abs(oppositeAngle) || !constraintsCheck.Item1)
                    {
                        rotationAmount = oppositeAngle;
                    }
                }
                float amount = Mathf.Abs(rotationAmount) > angleMagnitude ? Mathf.Sign(angleForward) * angleMagnitude : rotationAmount;
                float oppositeAmount = Mathf.Abs(rotationAmount) > angleMagnitude ? Mathf.Sign(oppositeAngle) * angleMagnitude : rotationAmount;

                float amountToRotatePerFrame = angleMagnitude;
                float amountRotated = 0f;
                //Vector3 horiontalDirectionBefore = armToRotateDirection;
                
                if (fullRotation)
                {
                    armToRotate.Rotate(rotationAxis, rotationAmount, Space.World);
                }
                else
                {
                    if (Mathf.Abs(rotationAmount) > amountToRotatePerFrame)
                    {
                        armToRotate.Rotate(rotationAxis, Mathf.Sign(rotationAmount) * amountToRotatePerFrame, Space.World);
                        //planeDetails.horizontalRotation += Mathf.Sign(rotationAmount) * amountToRotatePerFrame;
                    }
                    else
                    {
                        armToRotate.Rotate(rotationAxis, rotationAmount, Space.World);
                        //planeDetails.horizontalRotation += rotationAmount;
                    }
                }
                // RECORD THE ROTATION: re-derive horizontal / vertical from where the arm now points,
                // instead of adding a per-axis delta (the old per-axis deltas drifted, see SyncRecordedRotation).
                SyncRecordedRotation(planeDetails, armToRotate, forwardAxis);
            }
            /*else {
                float amountRotated = 0f;
                if (useVerticalAxis)
                {
                    projectedAfter = Vector3.ProjectOnPlane(horizontalDirectionAfter, armToRotate.forward);
                    Vector3 aOR = projectedBefore.magnitude >= 0.99f && projectedAfter.magnitude >= 0.99f ? armToRotate.forward : Vector3.zero;
                    //Vector3 aOR = verticalRotationAxis.magnitude > 0 ? verticalRotationAxis : Vector3.zero;
                    if (aOR.magnitude > 0)
                    {
                        float verticalAmountRotated = DetermineSignedAngle(planeDetails, horizontalDirectionBefore, horizontalDirectionAfter, aOR);
                        planeDetails.verticalRotation += verticalAmountRotated;
                    }
                }
                else if (useHorizontalAxis)
                {
                    amountRotated = DetermineSignedAngle(planeDetails, horizontalDirectionBefore, horizontalDirectionAfter, rotationAxis);
                    planeDetails.horizontalRotation += amountRotated;
                }
            }*/


            return new Tuple<PlaneViewModel, bool>(planeDetails, ableToRotate);

        }
        public bool RotateTransformToDirectionUsingAxis(Vector3 armToRotateDirection, Transform armToRotate, Vector3 analogAxisOfRotationInput3DVector, Vector3 rotationAxis, float angleMagnitude = 30, bool fullRotation = false)
        {
            bool completedRotation = false;
            try
            {
                Vector3 projectedSource = Vector3.ProjectOnPlane(armToRotateDirection, rotationAxis);
                Vector3 projectedTarget = Vector3.ProjectOnPlane(analogAxisOfRotationInput3DVector, rotationAxis);
                float angleSource = Vector3.SignedAngle(projectedSource, projectedTarget, rotationAxis);
                float amountToRotatePerFrame = angleMagnitude;
                if (fullRotation)
                {
                    armToRotate.Rotate(rotationAxis, angleSource, Space.World);
                    completedRotation = true;
                }
                else
                {
                    if (Mathf.Abs(angleSource) > amountToRotatePerFrame)
                    {
                        armToRotate.Rotate(rotationAxis, Mathf.Sign(angleSource) * amountToRotatePerFrame, Space.World);
                    }
                    else
                    {
                        armToRotate.Rotate(rotationAxis, angleSource, Space.World);
                        completedRotation = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Trying to Rotate Transform: " + armToRotate.name + ". Error: " + ex.Message);
                return false;
            }
            return completedRotation;
        }

        public List<KeyValuePair<string, bool>> AlignAxis(List<AxisRotationValidation> rotationValidators)
        {
            List<KeyValuePair<string, bool>> transformRotationCheck = new List<KeyValuePair<string, bool>>();
            foreach (var axisDetails in rotationValidators)
            {
                if (ValidationUtility.IsFullyInitialized(axisDetails, int.MaxValue) && axisDetails.axisRotationValidator.conditionToUse)
                {
                    bool properlyRotated = axisDetails.rotationType == RotationType.snap ? SnapToDirection(axisDetails.axisRotationValidator.sourceAxis, axisDetails.sourceTransform, axisDetails.axisRotationValidator.targetVector) :
                                           axisDetails.rotationType == RotationType.useGlobalAndLocalAxes ? AlignUsingGlobalAndLocalAxes(axisDetails.axisRotationValidator.sourceAxis, axisDetails.sourceTransform, axisDetails.axisRotationValidator.targetVector, axisDetails.rotationAmount, axisDetails.isFullRotation) :
                                           axisDetails.rotationType == RotationType.useForwardAndUpAxes ? AlignUsingForwardAndUpAxes(axisDetails.axisRotationValidator.sourceAxis, axisDetails.sourceTransform, axisDetails.axisRotationValidator.targetVector, axisDetails.rotationAmount, axisDetails.isFullRotation) :
                                           axisDetails.rotationType == RotationType.useSingleAxis ? RotateTransformToDirectionUsingAxis(GetAxisVector(axisDetails.axisRotationValidator.sourceAxis, axisDetails.sourceTransform), axisDetails.sourceTransform, axisDetails.axisRotationValidator.targetVector, axisDetails.axisOfRotation, axisDetails.rotationAmount, axisDetails.isFullRotation) : false;
                    transformRotationCheck.Add(new KeyValuePair<string, bool>(axisDetails.sourceTransform.name, properlyRotated));
                }
            }
            return transformRotationCheck;
        }
    }
}

