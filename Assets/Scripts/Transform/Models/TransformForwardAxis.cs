using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using FootballGame.Common;
namespace FootballGame.TransformOperations.Models {
    public class TransformForwardAxis : TransformAxis {
        public TransformForwardAxis(TransformViewModel viewModel) : base(viewModel) { }

        public override List<KeyValuePair<string, bool>> RotateTowardsTarget()
        {
            return AlignAxis(transformViewModel.GetAxisRotationValidators());
        }
    }
}

