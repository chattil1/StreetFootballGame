using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using FootballGame.Common;
using FootballGame.TransformOperations.Interfaces;
using FootballGame.TransformOperations.Rotation.Validators;
using FootballGame.TransformOperations.Models;
namespace FootballGame.TransformOperations.Controllers {
    public class RightShoulderController : Controller
    {
        public RightShoulderController(TransformViewModel viewModel) : base(viewModel)
        { }
        public RightShoulderController(BodyPart transformType, Axis axis, Transform transformToControl, Transform handTransformToControl) : base(transformType, axis, transformToControl, handTransformToControl)
        { }
        public override BodyPartModel GetBodyPartModel(TransformViewModel viewModel)
        {
            return new RightShoulderModel(viewModel);
        }
    }
}

