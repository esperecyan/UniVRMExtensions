#nullable enable
using System;
using System.Linq;
using UnityEngine;
#if VRC_SDK_VRCSDK3
using static VRC.Dynamics.VRCPhysBoneBase;
using Version = VRC.Dynamics.VRCPhysBoneBase.Version;
#endif

namespace Esperecyan.UniVRMExtensions.SwayingObjects
{
    /// <summary>
    /// <see cref="VRCPhysBone"/>の各種パラメータに対応する値。
    /// </summary>
    public class VRCPhysBoneParameters
    {

#if VRC_SDK_VRCSDK3
        public Version Version = (Version)Enum.GetValues(typeof(Version)).Cast<int>().Max();
        public IntegrationType IntegrationType = IntegrationType.Simplified;
#endif

        public float Pull = 0.2f;
        public AnimationCurve? PullCurve = null;
        public float Spring = 0.2f;
        public AnimationCurve? SpringCurve = null;
        public float Stiffness = 0.2f;
        public AnimationCurve? StiffnessCurve = null;
        public float Gravity = 0;
        public AnimationCurve? GravityCurve = null;
        public float GravityFalloff = 0;
        public AnimationCurve? GravityFalloffCurve = null;
#if VRC_SDK_VRCSDK3
        public ImmobileType ImmobileType;
#endif
        public float Immobile = 0;
        public AnimationCurve? ImmobileCurve = null;
        public float GrabMovement = 0;
        public float MaxStretch = 0;
        public AnimationCurve? MaxStretchCurve = null;
    }
}
