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
    public class LeftArmController : Controller
    {
        public LeftArmController(TransformViewModel viewModel) : base(viewModel)
        { }
        public LeftArmController(BodyPart transformType, Axis axis, Transform transformToControl, Transform handTransformToControl) : base(transformType, axis, transformToControl, handTransformToControl)
        { }
        public override List<KeyValuePair<string, bool>> AlignWithOtherAxis(GameObject childItemToControl, Axis twistAxis, Transform sourceTransform, Transform targetTransform, Vector3 targetDirection, Vector3 palmCatchArea, float rotationQuantity, string childNameSource, string childNameTarget)
        {
            return new List<KeyValuePair<string, bool>>();
        }
        public override BodyPartModel GetBodyPartModel(TransformViewModel viewModel)
        {
            return new LeftArmModel(viewModel);
        }
    }
}

