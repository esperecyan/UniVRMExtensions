#nullable enable
using UnityEngine;

namespace Esperecyan.UniVRMExtensions.Utilities
{
    /// <summary>
    /// HumanoidボーンとTransformの対応関係。
    /// </summary>
    internal static class TransformUtilities
    {
        /// <summary>
        /// 向きが異なる <see cref="Transform"/> 同士で、同じワールド座標へのオフセットを計算します。
        /// </summary>
        /// <param name="sourceTransform"></param>
        /// <param name="sourceOffset"></param>
        /// <param name="destinationTransform"></param>
        /// <returns></returns>
        internal static Vector3 CalculateOffset(
            Transform sourceTransform,
            Vector3 sourceOffset,
            Transform destinationTransform
        )
        {
            return destinationTransform.InverseTransformPoint(
                sourceTransform.TransformPoint(sourceOffset) - sourceTransform.position
                    + destinationTransform.position
            );
        }

        /// <summary>
        /// 縮尺が異なる <see cref="Transform"/> 同士で、長さを計算します。
        /// </summary>
        /// <param name="sourceTransform">X方向のスケールを利用します。</param>
        /// <param name="distance">長さ。</param>
        /// <param name="destinationTransform">X方向のスケールを利用します。指定しなかった場合は正規化します。</param>
        /// <returns></returns>
        internal static float CalculateDistance(
            Transform sourceTransform,
            float distance,
            Transform? destinationTransform = null
        )
        {
            return distance * sourceTransform.lossyScale.x
                / (destinationTransform != null ? destinationTransform.lossyScale.x : 1);
        }
    }
}
