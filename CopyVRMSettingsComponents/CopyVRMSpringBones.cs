using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditorInternal;
using VRM;
using Esperecyan.UniVRMExtensions.Utilities;

namespace Esperecyan.UniVRMExtensions.CopyVRMSettingsComponents
{
    internal class CopyVRMSpringBones
    {
        /// <summary>
        /// VRMSpringBone、およびVRMSpringBoneColliderGroupをコピーします。
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
            foreach (var component in new[] { typeof(VRMSpringBone), typeof(VRMSpringBoneColliderGroup) }
                .SelectMany(type => destination.GetComponentsInChildren(type)))
            {
                Object.DestroyImmediate(component);
            }

            IDictionary<Transform, Transform> transformMapping = new Dictionary<Transform, Transform>();

            foreach (var sourceSpringBone in source.GetComponentsInChildren<VRMSpringBone>())
            {
                if (sourceSpringBone.RootBones.Count == 0)
                {
                    continue;
                }

                transformMapping = CopyVRMSpringBones.CopySpringBone(
                    sourceSpringBone,
                    destination,
                    sourceSkeletonBones,
                    transformMapping
                );
            }

            CopyVRMSpringBones.CopySpringBoneColliderGroupForVirtualCast(source, destination, sourceSkeletonBones);
        }

        /// <summary>
        /// VRMSpringBone、およびVRMSpringBoneColliderGroupをコピーします。
        /// </summary>
        /// <param name="sourceSpringBone"></param>
        /// <param name="destination"></param>
        /// <param name="sourceSkeletonBones"></param>
        /// <param name="transformMapping"></param>
        /// <returns>更新された <c>transformMapping</c> を返します。</returns>
        private static IDictionary<Transform, Transform> CopySpringBone(
            VRMSpringBone sourceSpringBone,
            GameObject destination,
            Dictionary<HumanBodyBones, Transform> sourceSkeletonBones,
            IDictionary<Transform, Transform> transformMapping
        )
        {
            var destinationSecondary = destination.transform.Find("secondary").gameObject;

            ComponentUtility.CopyComponent(sourceSpringBone);
            ComponentUtility.PasteComponentAsNew(destinationSecondary);
            var destinationSpringBone = destinationSecondary.GetComponents<VRMSpringBone>().Last();

            if (destinationSpringBone.m_center)
            {
                destinationSpringBone.m_center = transformMapping.ContainsKey(destinationSpringBone.m_center)
                    ? transformMapping[destinationSpringBone.m_center]
                    : (transformMapping[destinationSpringBone.m_center] = BoneMapper.FindCorrespondingBone(
                        sourceBone: destinationSpringBone.m_center,
                        source: sourceSpringBone.transform.root.gameObject,
                        destination,
                        sourceSkeletonBones
                    ));
            }

            for (var i = 0; i < destinationSpringBone.RootBones.Count; i++)
            {
                var sourceSpringBoneRoot = destinationSpringBone.RootBones[i];

                destinationSpringBone.RootBones[i] = sourceSpringBoneRoot
                    ? (transformMapping.ContainsKey(sourceSpringBoneRoot)
                        ? transformMapping[sourceSpringBoneRoot]
                        : (transformMapping[sourceSpringBoneRoot] = BoneMapper.FindCorrespondingBone(
                            sourceBone: sourceSpringBoneRoot,
                            source: sourceSpringBone.transform.root.gameObject,
                            destination,
                            sourceSkeletonBones
                        )))
                    : null;
            }

            for (var i = 0; i < destinationSpringBone.ColliderGroups.Length; i++)
            {
                var sourceColliderGroup = destinationSpringBone.ColliderGroups[i];

                var destinationColliderGroupTransform = sourceColliderGroup
                    ? (transformMapping.ContainsKey(sourceColliderGroup.transform)
                        ? transformMapping[sourceColliderGroup.transform]
                        : (transformMapping[sourceColliderGroup.transform] = BoneMapper.FindCorrespondingBone(
                            sourceBone: sourceColliderGroup.transform,
                            source: sourceSpringBone.transform.root.gameObject,
                            destination,
                            sourceSkeletonBones
                        )))
                    : null;

                VRMSpringBoneColliderGroup destinationColliderGroup = null;
                if (destinationColliderGroupTransform)
                {
                    CopyVRMSpringBones.CopySpringBoneColliderGroups(
                        sourceBone: sourceColliderGroup.transform,
                        destinationBone: destinationColliderGroupTransform
                    );
                    destinationColliderGroup
                        = destinationColliderGroupTransform.GetComponent<VRMSpringBoneColliderGroup>();
                }
                destinationSpringBone.ColliderGroups[i] = destinationColliderGroup;
            }

            return transformMapping;
        }

        /// <summary>
        /// コピー先にVRMSpringBoneColliderGroupが存在しなければ、コピー元のVRMSpringBoneColliderGroupをすべてコピーします。
        /// </summary>
        /// <param name="sourceBone"></param>
        /// <param name="destinationBone"></param>
        private static void CopySpringBoneColliderGroups(Transform sourceBone, Transform destinationBone)
        {
            if (destinationBone.GetComponent<VRMSpringBoneColliderGroup>())
            {
                return;
            }

            foreach (var sourceColliderGroup in sourceBone.GetComponents<VRMSpringBoneColliderGroup>())
            {
                ComponentUtility.CopyComponent(sourceColliderGroup);
                ComponentUtility.PasteComponentAsNew(destinationBone.gameObject);

                // 正規化されていないモデルに対応するため、オフセットをルートからの相対的な向きに
                var colliderGroup = destinationBone.GetComponents<VRMSpringBoneColliderGroup>().Last();
                foreach (var collider in colliderGroup.Colliders)
                {
                    collider.Offset = TransformUtilities.CalculateOffset(destinationBone, collider.Offset, sourceBone);
                }

            }
        }

        /// <summary>
        /// バーチャルキャスト向けに、どのVRMSpringBoneにも関連付けられていないVRMSpringBoneColliderGroupをコピーします。
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="sourceSkeletonBones"></param>
        private static void CopySpringBoneColliderGroupForVirtualCast(
            GameObject source,
            GameObject destination,
            Dictionary<HumanBodyBones, Transform> sourceSkeletonBones
        )
        {
            var destinationAnimator = destination.GetComponent<Animator>();
            foreach (var humanoidBone in new[] { HumanBodyBones.LeftHand, HumanBodyBones.RightHand })
            {
                CopyVRMSpringBones.CopySpringBoneColliderGroups(
                    sourceBone: sourceSkeletonBones[humanoidBone],
                    destinationBone: destinationAnimator.GetBoneTransform(humanoidBone)
                );
            }
        }
    }
}
