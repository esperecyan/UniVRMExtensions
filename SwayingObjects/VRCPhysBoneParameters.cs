using UnityEngine;
#if VRC_SDK_VRCSDK3
using static VRC.Dynamics.VRCPhysBoneBase;
#endif

namespace Esperecyan.UniVRMExtensions.SwayingObjects
{
    /// <summary>
    /// <see cref="VRCPhysBone"/>の各種パラメータに対応する値。
    /// </summary>
    public class VRCPhysBoneParameters
    {
        private static readonly float DefaultStiffness = 0.2f;

        public float Pull = 0.2f;
        public AnimationCurve PullCurve = null;
#if VRC_SDK_VRCSDK3
        internal IntegrationType IntegrationType =>
            this.Spring == VRCPhysBoneParameters.DefaultStiffness && this.StiffnessCurve == null
                ? IntegrationType.Simplified
                : IntegrationType.Advanced;
#endif
        public float Spring = 0.2f;
        public AnimationCurve SpringCurve = null;
        public float Stiffness = VRCPhysBoneParameters.DefaultStiffness;
        public AnimationCurve StiffnessCurve = null;
#if VRC_SDK_VRCSDK3
        public ImmobileType ImmobileType;
#endif
        public float Immobile = 0;
        public AnimationCurve ImmobileCurve = null;
        public float GrabMovement = 0;
        public float MaxStretch = 0;
        public AnimationCurve MaxStretchCurve = null;
    }
}
