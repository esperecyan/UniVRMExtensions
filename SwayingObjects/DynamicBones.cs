using System;

namespace Esperecyan.UniVRMExtensions.SwayingObjects
{
    internal static class DynamicBones
    {
        internal static readonly Type DynamicBoneType = Type.GetType("DynamicBone, Assembly-CSharp");
        internal static readonly Type DynamicBoneColliderType = Type.GetType("DynamicBoneCollider, Assembly-CSharp");
        internal static readonly Type DynamicBoneColliderBaseListType
            = Type.GetType("System.Collections.Generic.List`1[[DynamicBoneColliderBase, Assembly-CSharp]]");
    }
}
