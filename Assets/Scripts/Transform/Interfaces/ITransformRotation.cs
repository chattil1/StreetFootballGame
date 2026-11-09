using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FootballGame.Common;
using FootballGame.TransformOperations.Models;
using FootballGame.TransformOperations.Rotation.Validators;
namespace FootballGame.TransformOperations.Interfaces
{
    interface ITransformRotation
    {
        void InitializePlane();
        List<KeyValuePair<string, bool>> AlignWithOtherAxis(GameObject childItemToControl, Axis twistAxis, Transform sourceTransform, Transform targetTransform, Vector3 targetDirection, Vector3 palmCatchArea, float rotationQuantity, string childNameSource, string childNameTarget);
        List<KeyValuePair<string, bool>> AlignTransformAxis();
        PlaneViewModel AlignWithCheck(Vector3 expectedTarget);
        TransformViewModel GetTransformViewModel();
    }
}

