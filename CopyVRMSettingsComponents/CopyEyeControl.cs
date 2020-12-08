using System.Collections.Generic;
using UnityEngine;
using UnityEditorInternal;
using UniGLTF;
using VRM;
using Esperecyan.UniVRMExtensions.Utilities;

namespace Esperecyan.UniVRMExtensions.CopyVRMSettingsComponents
{
    internal class CopyEyeControl
    {
        /// <summary>
        /// 視線制御の設定をコピーします。
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
            if (!source.GetComponent<VRMFirstPerson>())
            {
                return;
            }

            var sourceLookAtHead = source.GetComponent<VRMLookAtHead>();
            if (!sourceLookAtHead)
            {
                var destinationLookAtHead = destination.GetComponent<VRMLookAtHead>();
                if (destinationLookAtHead)
                {
                    Object.DestroyImmediate(destinationLookAtHead);
                }
                return;
            }

            var sourceBoneApplyer = source.GetComponent<VRMLookAtBoneApplyer>();
            if (sourceBoneApplyer)
            {
                ComponentUtility.CopyComponent(sourceBoneApplyer);
                var destinationBoneApplyer = destination.GetOrAddComponent<VRMLookAtBoneApplyer>();
                ComponentUtility.PasteComponentValues(destinationBoneApplyer);

                if (destinationBoneApplyer.LeftEye.Transform)
                {
                    destinationBoneApplyer.LeftEye.Transform = BoneMapper.FindCorrespondingBone(
                        sourceBone: destinationBoneApplyer.LeftEye.Transform,
                        source: source,
                        destination: destination,
                        sourceSkeletonBones: sourceSkeletonBones
                    );
                }
                if (destinationBoneApplyer.RightEye.Transform)
                {
                    destinationBoneApplyer.RightEye.Transform = BoneMapper.FindCorrespondingBone(
                        sourceBone: destinationBoneApplyer.RightEye.Transform,
                        source: source,
                        destination: destination,
                        sourceSkeletonBones: sourceSkeletonBones
                    );
                }

                var blendShapeApplyer = destination.GetComponent<VRMLookAtBlendShapeApplyer>();
                if (blendShapeApplyer)
                {
                    Object.DestroyImmediate(blendShapeApplyer);
                }
                return;
            }

            var sourceBlendShapeApplyer = source.GetComponent<VRMLookAtBlendShapeApplyer>();
            if (sourceBlendShapeApplyer)
            {
                ComponentUtility.CopyComponent(sourceBlendShapeApplyer);
                var destinationBlendShapeApplyer = destination.GetOrAddComponent<VRMLookAtBlendShapeApplyer>();
                ComponentUtility.PasteComponentValues(destinationBlendShapeApplyer);
            }
        }
    }
}
