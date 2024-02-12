#nullable enable
using UnityEngine;

namespace Esperecyan.UniVRMExtensions.SwayingObjects
{
    /// <summary>
    /// <see cref="VRM.VRMSpringBone"/>の各種パラメータに対応する値。
    /// </summary>
    public class VRMSpringBoneParameters
    {
        public float StiffnessForce = 1.0f;
        public float GravityPower = 0.0f;
        public Vector3 GravityDir = new(0, -1.0f, 0);
        public float DragForce = 0.4f;
    }
}
