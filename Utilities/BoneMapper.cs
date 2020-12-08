using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UniGLTF;

namespace Esperecyan.UniVRMExtensions.Utilities
{
    /// <summary>
    /// HumanoidボーンとTransformの対応関係。
    /// </summary>
    internal class BoneMapper
    {
        /// <summary>
        /// すべてのスケルトンボーンを取得します。
        /// </summary>
        /// <param name="avatar"></param>
        /// <returns></returns>
        internal static Dictionary<HumanBodyBones, Transform> GetAllSkeletonBones(GameObject avatar)
        {
            var animator = avatar.GetComponent<Animator>();
            return Enum.GetValues(typeof(HumanBodyBones)).Cast<HumanBodyBones>()
                .Select(bone => (
                    bone,
                    transform: bone != HumanBodyBones.LastBone ? animator.GetBoneTransform(bone) : null
                ))
                .Where(boneTransformPair => boneTransformPair.transform != null)
                .ToDictionary(
                    boneTransformPair => boneTransformPair.bone,
                    boneTransformPair => boneTransformPair.transform
                );
        }

        /// <summary>
        /// コピー元のアバターの指定ボーンと対応する、コピー先のアバターのボーンを返します。
        /// </summary>
        /// <param name="sourceBoneRelativePath"></param>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="sourceSkeletonBones"></param>
        /// <returns>見つからなかった場合は <c>null</c> を返します。</returns>
        internal static Transform FindCorrespondingBone(
            Transform sourceBone,
            GameObject source,
            GameObject destination,
            Dictionary<HumanBodyBones, Transform> sourceSkeletonBones
        )
        {
            if (!sourceBone.IsChildOf(source.transform))
            {
                return null;
            }

            var sourceBoneRelativePath = sourceBone.RelativePathFrom(root: source.transform);
            var destinationBone = destination.transform.Find(sourceBoneRelativePath);
            if (destinationBone)
            {
                return destinationBone;
            }

            if (!sourceBone.IsChildOf(source.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Hips)))
            {
                return null;
            }

            var humanoidAndSkeletonBone = BoneMapper.ClosestSkeletonBone(sourceBone, sourceSkeletonBones);
            var destinationAniamtor = destination.GetComponent<Animator>();
            var destinationSkeletonBone = destinationAniamtor.GetBoneTransform(humanoidAndSkeletonBone.Key);
            if (!destinationSkeletonBone)
            {
                return null;
            }

            destinationBone = destinationSkeletonBone.Find(sourceBone.RelativePathFrom(humanoidAndSkeletonBone.Value));
            if (destinationBone)
            {
                return destinationBone;
            }

            return destinationSkeletonBone.GetComponentsInChildren<Transform>()
                .FirstOrDefault(bone => bone.name == sourceBone.name);
        }

        /// <summary>
        /// 祖先方向へたどり、指定されたボーンを含む直近のスケルトンボーンを取得します。
        /// </summary>
        /// <param name="bone"></param>
        /// <param name="avatar"></param>
        /// <param name="skeletonBones"></param>
        /// <returns></returns>
        private static KeyValuePair<HumanBodyBones, Transform> ClosestSkeletonBone(
            Transform bone,
            Dictionary<HumanBodyBones, Transform> skeletonBones
        )
        {
            foreach (Transform parent in bone.Ancestors())
            {
                if (!skeletonBones.ContainsValue(parent))
                {
                    continue;
                }

                return skeletonBones.FirstOrDefault(humanoidAndSkeletonBone => humanoidAndSkeletonBone.Value == parent);
            }

            throw new ArgumentException();
        }
    }
}
