using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using FootballGame.Common;
using FootballGame.TransformOperations.Interfaces;
using FootballGame.TransformOperations.Rotation.Validators;
using FootballGame.TransformOperations.Models;
namespace FootballGame.TransformOperations.Controllers
{
    public class Controller : ITransformRotation
    {
        protected TransformViewModel transformViewModel;
        public Controller(TransformViewModel viewModel)
        {
            transformViewModel = viewModel;
        }
        public Controller(BodyPart transformType, Axis axis, Transform transformToControl, Transform handTransformToControl) {

        }
        #region ITransformRotation Implementations
        public virtual List<KeyValuePair<string, bool>> AlignWithOtherAxis(GameObject childItemToControl, Axis twistAxis, Transform sourceTransform, Transform targetTransform, Vector3 targetDirection, Vector3 palmCatchArea, float rotationQuantity, string childNameSource, string childNameTarget)
        {
            return new List<KeyValuePair<string, bool>>();
        }
        public TransformViewModel GetTransformViewModel()
        {
            return transformViewModel;
        }
        public virtual List<KeyValuePair<string, bool>> AlignTransformAxis()
        {
            BodyPartModel bodyPartModel = GetBodyPartModel(transformViewModel);
            return bodyPartModel.RotateTowardsTarget();
        }
        public virtual PlaneViewModel AlignWithCheck(Vector3 expectedTarget) {
            BodyPartModel bodyPartModel = GetBodyPartModel(transformViewModel);
            return bodyPartModel.RotateTowardsTargetWithCheck(expectedTarget);
        }
        #endregion
        public void InitializePlane()
        {
            BodyPartModel bodyPartModel = GetBodyPartModel(transformViewModel);
            bodyPartModel.InitializePlane();
        }
        public virtual BodyPartModel GetBodyPartModel(TransformViewModel viewModel)
        {
            switch (viewModel.GetBodyPart())
            {
                case BodyPart.root:
                    return new RootModel(viewModel);
                case BodyPart.leftShoulder:
                    return new LeftShoulderModel(viewModel);
                case BodyPart.rightShoulder:
                    return new RightShoulderModel(viewModel);
                case BodyPart.leftArm:
                    return new LeftArmModel(viewModel);
                /*case BodyPart.rightArm:
                    return new ShoulderModel("Right", axis);*/
                default:
                    break;

            }
            return new BodyPartModel(new TransformViewModel());
        }
    }
}
