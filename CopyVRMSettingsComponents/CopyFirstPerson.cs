using System.Collections.Generic;
using Esperecyan.UniVRMExtensions.Utilities;
using UnityEngine;
using VRM;

namespace Esperecyan.UniVRMExtensions.CopyVRMSettingsComponents
{
    internal class CopyFirstPerson
    {
        /// <summary>
        /// 一人称表示の設定をコピーします。
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="sourceSkeletonBones"></param>
        internal static void Copy(
            GameObject source,
            GameObject destination,
            Dictionary<HumanBodyBones, Transform> sourceSkeletonBones
        )
        {
            var sourceFirstPerson = source.GetComponent<VRMFirstPerson>();
            var destinationFirstPerson = destination.GetComponent<VRMFirstPerson>();
            if (!sourceFirstPerson)
            {
                if (destinationFirstPerson)
                {
                    Object.DestroyImmediate(destinationFirstPerson);
                }
                return;
            }

            if (sourceFirstPerson.FirstPersonBone)
            {
                destinationFirstPerson.FirstPersonBone = BoneMapper.FindCorrespondingBone(
                    sourceBone: sourceFirstPerson.FirstPersonBone,
                    source: source,
                    destination: destination,
                    sourceSkeletonBones: sourceSkeletonBones
                );
            }

            destinationFirstPerson.FirstPersonOffset = sourceFirstPerson.FirstPersonOffset;

            foreach (VRMFirstPerson.RendererFirstPersonFlags sourceFlags in sourceFirstPerson.Renderers)
            {
                if (sourceFlags.FirstPersonFlag == FirstPersonFlag.Auto)
                {
                    continue;
                }

                Mesh sourceMesh = sourceFlags.SharedMesh;
                if (!sourceMesh)
                {
                    continue;
                }

                var sourceMeshName = sourceMesh.name;

                var index = destinationFirstPerson.Renderers.FindIndex(match: flags => {
                    Mesh destinationMesh = flags.SharedMesh;
                    return destinationMesh && destinationMesh.name == sourceMeshName;
                });
                if (index == -1)
                {
                    continue;
                }

                var destinationFlags = destinationFirstPerson.Renderers[index];
                destinationFlags.FirstPersonFlag = sourceFlags.FirstPersonFlag;
                destinationFirstPerson.Renderers[index] = destinationFlags;
            }
        }
    }
}
