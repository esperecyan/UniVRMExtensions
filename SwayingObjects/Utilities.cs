using System.Linq;
using UnityEngine;
using UnityEditor;
using VRM;

namespace Esperecyan.UniVRMExtensions.SwayingObjects
{
    internal static class Utilities
    {
        internal static void DestroyDynamicBones(GameObject instance, bool isAsset)
        {
            foreach (var component in instance.GetComponentsInChildren<Component>(includeInactive: true))
            {
                if (!new[] { "DynamicBone", "DynamicBoneCollider" }.Contains(component.GetType().FullName))
                {
                    continue;
                }

                if (isAsset)
                {
                    Object.DestroyImmediate(component);
                }
                else
                {
                    Undo.DestroyObjectImmediate(component);
                }
            }
        }

        internal static void DestroyVRMSpringBones(GameObject instance, bool isAsset)
        {
            foreach (var component in instance.GetComponentsInChildren<VRMSpringBone>(includeInactive: true)
                .Cast<Component>()
                .Concat(instance.GetComponentsInChildren<VRMSpringBoneColliderGroup>(includeInactive: true)))
            {
                if (isAsset)
                {
                    Object.DestroyImmediate(component);
                }
                else
                {
                    Undo.DestroyObjectImmediate(component);
                }
            }
        }
    }
}
