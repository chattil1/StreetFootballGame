using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using FootballGame.Common;
using FootballGame.TransformOperations.Rotation.Validators;
using FootballGame.TransformOperations.Controllers;

namespace FootballGame.TransformOperations.Models {
    public class BodyPartModel
    {

        protected TransformViewModel transformViewModel;
        public BodyPartModel(TransformViewModel viewModel)
        {
            transformViewModel = viewModel;
        }
        #region Methods to override
        public virtual List<AxisRotationValidation> GetRotationValidatorsToPointAtTarget() { return new List<AxisRotationValidation>(); }
        public virtual List<KeyValuePair<string, bool>> RotateTowardsTarget()
        {
            TransformAxis transformAxis = GetTransformAxis(transformViewModel.GetSourceAxis());
            List<KeyValuePair<string, bool>> rotationOutcome = transformAxis.RotateTowardsTarget();
            return rotationOutcome;
        }
        public virtual PlaneViewModel RotateTowardsTargetWithCheck(Vector3 expectedTarget) {
            TransformAxis transformAxis = GetTransformAxis(transformViewModel.GetSourceAxis());
            transformViewModel.SetPlaneDetails(transformAxis.RotateWithCheck(expectedTarget));
            return transformViewModel.GetPlaneDetails();
        }
        #endregion
        public void InitializePlane() {
            TransformAxis transformAxis = GetTransformAxis(transformViewModel.GetSourceAxis());
            transformAxis.InitializePlane();
        }
        public TransformAxis GetTransformAxis(Axis axis)
        {
            switch (axis)
            {
                case Axis.inverseRight:
                    return new TransformInverseRightAxis(transformViewModel);
                case Axis.forward:
                    return new TransformForwardAxis(transformViewModel);
                case Axis.up:
                    return new TransformUpAxis(transformViewModel);
                default:
                    break;
            }
            return new TransformAxis(new TransformViewModel());
        }
        bool SnapToDirection(Axis axisToRotate, Transform transformToRotate, Vector3 targetDirection)
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

        Vector3 GetAxisVector(Axis sourceAxis, Transform sourceTransform)
        {
            return sourceAxis == Axis.right ? sourceTransform.right : sourceAxis == Axis.forward ? sourceTransform.forward : sourceAxis == Axis.up ? sourceTransform.up : sourceAxis == Axis.inverseRight ? -sourceTransform.right : sourceAxis == Axis.inverseForward ? -sourceTransform.forward : sourceAxis == Axis.inverseUp ? -sourceTransform.up : Vector3.zero;
        }
        bool AlignUsingGlobalAndLocalAxes(Axis sourceAxis, Transform armToRotate, Vector3 targetDirection, float rotationQuantity, bool fullRotation)
        {
            Vector3 sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
            RotateTransformToDirection(sourceAxisVector, armToRotate, targetDirection, Vector3.up, rotationQuantity, fullRotation);
            sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
            RotateTransformToDirection(sourceAxisVector, armToRotate, targetDirection, Vector3.right, rotationQuantity, fullRotation);
            sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
            RotateTransformToDirection(sourceAxisVector, armToRotate, targetDirection, Vector3.forward, rotationQuantity, fullRotation);
            sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
            RotateTransformToDirection(sourceAxisVector, armToRotate, targetDirection, armToRotate.up, rotationQuantity, fullRotation);
            sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
            RotateTransformToDirection(sourceAxisVector, armToRotate, targetDirection, armToRotate.right, rotationQuantity, fullRotation);
            sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
            if (Mathf.Abs(sourceAxisVector.normalized.x - targetDirection.normalized.x) <= 0.01f && Mathf.Abs(sourceAxisVector.normalized.y - targetDirection.normalized.y) <= 0.01f && Mathf.Abs(sourceAxisVector.normalized.z - targetDirection.normalized.z) <= 0.01f)
            {
                return true;
            }
            return false;
        }
        private void RotateTransformToDirection(Vector3 armToRotateDirection, Transform armToRotate, Vector3 analogAxisOfRotationInput3DVector, Vector3 rotationAxis, float angleMagnitude = 30, bool fullRotation = false)
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
        private bool RotateTransformToDirectionUsingAxis(Vector3 armToRotateDirection, Transform armToRotate, Vector3 analogAxisOfRotationInput3DVector, Vector3 rotationAxis, float angleMagnitude = 30, bool fullRotation = false)
        {
            try
            {
                Vector3 projectedSource = Vector3.ProjectOnPlane(armToRotateDirection, rotationAxis);
                Vector3 projectedTarget = Vector3.ProjectOnPlane(analogAxisOfRotationInput3DVector, rotationAxis);
                float angleSource = Vector3.SignedAngle(projectedSource, projectedTarget, rotationAxis);
                float amountToRotatePerFrame = angleMagnitude;
                if (fullRotation)
                {
                    armToRotate.Rotate(rotationAxis, angleSource, Space.World);
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
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Trying to Rotate Transform: " + armToRotate.name + ". Error: " + ex.Message);
                return false;
            }
            return false;
        }
        List<KeyValuePair<string, bool>> AlignAxis(List<AxisRotationValidation> rotationValidators)
        {
            List<KeyValuePair<string, bool>> transformRotationCheck = new List<KeyValuePair<string, bool>>();
            foreach (var axisDetails in rotationValidators)
            {
                if (ValidationUtility.IsFullyInitialized(axisDetails, int.MaxValue) && axisDetails.axisRotationValidator.conditionToUse)
                {
                    bool properlyRotated = axisDetails.rotationType == RotationType.snap ? SnapToDirection(axisDetails.axisRotationValidator.sourceAxis, axisDetails.sourceTransform, axisDetails.axisRotationValidator.targetVector) : axisDetails.rotationType == RotationType.useGlobalAndLocalAxes ? AlignUsingGlobalAndLocalAxes(axisDetails.axisRotationValidator.sourceAxis, axisDetails.sourceTransform, axisDetails.axisRotationValidator.targetVector, axisDetails.rotationAmount, axisDetails.isFullRotation) :
                                           axisDetails.rotationType == RotationType.useSingleAxis ? RotateTransformToDirectionUsingAxis(GetAxisVector(axisDetails.axisRotationValidator.sourceAxis, axisDetails.sourceTransform), axisDetails.sourceTransform, axisDetails.axisRotationValidator.targetVector, axisDetails.axisOfRotation, 30f, axisDetails.isFullRotation) : false;
                    transformRotationCheck.Add(new KeyValuePair<string, bool>(axisDetails.sourceTransform.name, properlyRotated));
                }
            }
            return transformRotationCheck;
        }
    }

}

