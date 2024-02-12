#nullable enable
using VRM;

namespace Esperecyan.UniVRMExtensions.SwayingObjects
{
    /// <summary>
    /// ボーンに関する情報。
    /// </summary>
    public class BoneInfo
    {
        /// <summary>
        /// アバターの情報。
        /// </summary>
        public readonly VRMMeta VRMMeta;

        /// <summary>
        /// 揺れ物に関する情報。
        /// </summary>
        /// <remarks>
        /// <see cref="VRMSpringBone.m_comment"/>、および <see cref="VRC.Dynamics.VRCPhysBoneBase.parameter"/>。
        /// </remarks>
        public readonly string Comment;

        internal BoneInfo(VRMMeta vrmMeta, string comment)
        {
            this.VRMMeta = vrmMeta;
            this.Comment = comment;
        }
    }
}
