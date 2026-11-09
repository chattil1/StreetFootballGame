using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using FootballGame.Common;
using FootballGame.TransformOperations.Rotation.Validators;
namespace FootballGame.TransformOperations.Models
{
    public class TransformViewModel
    {
        protected Axis sourceAxis { get; set; }
        protected Axis twistAxisOfRotation { get; set; }
        protected Axis axisToTwist { get; set; }
        protected BodyPart bodyPart { get; set; }
        protected PlaneViewModel planeDetails { get; set; }
        protected Transform sourceTransform { get; set; }
        protected Transform rootTransform { get; set; }
        protected Transform handTransform { get; set; }
        protected Vector2 movementInput { get; set; }
        protected Vector3 rootDirection { get; set; }
        protected Vector3 targetDirection { get; set; }
        protected Vector3 twistAxisOfRotationVector { get; set; }
        protected Vector3 defaultTargetXAxis { get; set; }
        protected float rotationPerFrame { get; set; }
        protected string twistTransformName { get; set; }
        protected List<AxisRotationValidation> axisRotationValidators { get; set; }

        public TransformViewModel(BodyPart transformType, PlaneViewModel planeInformation, Axis sourceAxisToView, Axis twistAxisOfRotationToView, Axis axisToTwistView, Transform transformToView, Transform rootTransformToView, Transform handTransformToPoint, Vector2 analogStickInput, Vector3 parentDirection, Vector3 target, Vector3 xAxisOfRotation, Vector3 defaultXAxisOfRotation, float rotationQuantity, string twistName) {
            bodyPart = transformType;
            planeDetails = planeInformation;
            sourceAxis = sourceAxisToView;
            rootTransform = rootTransformToView;
            twistAxisOfRotation = twistAxisOfRotationToView;
            axisToTwist = axisToTwistView;
            sourceTransform = transformToView;
            handTransform = handTransformToPoint;
            rootDirection = parentDirection;
            targetDirection = target;
            twistAxisOfRotationVector = xAxisOfRotation;
            defaultTargetXAxis = defaultXAxisOfRotation;
            rotationPerFrame = rotationQuantity;
            twistTransformName = twistName;
            movementInput = analogStickInput;
        }
        public TransformViewModel(BodyPart transformType, Axis sourceAxisToView, Axis twistAxisOfRotationToView, Transform transformToView, Transform handTransformToPoint)
        {
            bodyPart = transformType;
            sourceAxis = sourceAxisToView;
            twistAxisOfRotation = twistAxisOfRotationToView;
            sourceTransform = transformToView;
            handTransform = handTransformToPoint;
        }
        public TransformViewModel() { }

        #region Getters AND Setters
        public BodyPart GetBodyPart()
        {
            return bodyPart;
        }
        public PlaneViewModel GetPlaneDetails() {
            return planeDetails;
        }
        public Axis GetSourceAxis()
        {
            return sourceAxis;
        }
        public Axis GetTwistAxisOfRotation()
        {
            return twistAxisOfRotation;
        }
        public Axis GetAxisToTwist()
        {
            return axisToTwist;
        }
        public Transform GetSourceTransform()
        {
            return sourceTransform;
        }
        public Transform GetRootTransform()
        {
            return rootTransform;
        }
        public Transform GetHandTransform()
        {
            return handTransform;
        }
        public Vector2 GetAnalogStickInput()
        {
            return movementInput;
        }
        public Vector3 GetRootDirection()
        {
            return rootDirection;
        }
        public Vector3 GetTwistAxisOfRotationVector()
        {
            return twistAxisOfRotationVector;
        }
        public Vector3 GetTargetDirection() {
            return targetDirection;
        }
        public Vector3 GetDefaultTwistTarget()
        {
            return defaultTargetXAxis;
        }
        public float GetRotationPerFrame() {
            return rotationPerFrame;
        }
        public string GetTwistTransformName() {
            return twistTransformName;
        }
        public List<AxisRotationValidation> GetAxisRotationValidators()
        {
            return axisRotationValidators;
        }
        public void SetAxisRotationValidators(List<AxisRotationValidation> validators) {
            axisRotationValidators = validators;
        }
        public void SetTransformViewModel(BodyPart transformType, Axis axis, Axis twistAxis, Transform transformToView, Transform handTransformToView) {
            bodyPart = transformType;
            sourceAxis = axis;
            sourceTransform = transformToView;
            handTransform = handTransformToView;
        }

        public void SetPlaneDetails(PlaneViewModel model) {
            planeDetails = model;
        }

        #endregion
    }
}

