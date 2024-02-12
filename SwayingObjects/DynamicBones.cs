#nullable enable
using System;

namespace Esperecyan.UniVRMExtensions.SwayingObjects
{
    public static class DynamicBones
    {
        internal static readonly Type DynamicBoneType = Type.GetType("DynamicBone, Assembly-CSharp");
        internal static readonly Type DynamicBoneColliderType = Type.GetType("DynamicBoneCollider, Assembly-CSharp");
        internal static readonly Type DynamicBoneColliderBaseListType
            = Type.GetType("System.Collections.Generic.List`1[[DynamicBoneColliderBase, Assembly-CSharp]]");

        /// <summary>
        /// DynamicBoneアセットがインポートされていれば <c>true</c> を返します。
        /// </summary>
        /// <returns></returns>
        public static bool IsImported()
        {
            return DynamicBoneType != null;
        }
    }
}
