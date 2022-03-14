using UnityEngine;

namespace Esperecyan.UniVRMExtensions.SwayingObjects
{
    /// <summary>
    /// <see cref="VRCPhysBone"/>の各種パラメータに対応する値。
    /// </summary>
    public class VRCPhysBoneParameters
    {
        public float Pull = 0.2f;
        public AnimationCurve PullCurve = null;
        public float Spring = 0.2f;
        public AnimationCurve SpringCurve = null;
        public float Immobile = 0;
        public AnimationCurve ImmobileCurve = null;
        public float GrabMovement = 0;
        public float MaxStretch = 0;
        public AnimationCurve MaxStretchCurve = null;
    }
}
