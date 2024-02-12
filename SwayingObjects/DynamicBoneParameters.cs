#nullable enable
using UnityEngine;

namespace Esperecyan.UniVRMExtensions.SwayingObjects
{
    /// <summary>
    /// <see cref="DynamicBone"/>の各種パラメータに対応する値。
    /// </summary>
    public class DynamicBoneParameters
    {
        public float Damping = 0.1f;
        public AnimationCurve? DampingDistrib = null;
        public float Elasticity = 0.1f;
        public AnimationCurve? ElasticityDistrib = null;
        public float Stiffness = 0.1f;
        public AnimationCurve? StiffnessDistrib = null;
        public float Inert = 0;
        public AnimationCurve? InertDistrib = null;
        public Vector3 Gravity = new(0, 0, 0);

    }
}
