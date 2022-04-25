using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VRM;
#if VRC_SDK_VRCSDK3
using VRC.Dynamics;
using VRC.SDK3.Dynamics.PhysBone.Components;
#endif
using Esperecyan.UniVRMExtensions.Utilities;

namespace Esperecyan.UniVRMExtensions.SwayingObjects
{
    /// <summary>
    /// VRMSpringBoneをVRCPhysBoneへ変換します。
    /// </summary>
    public class VRMSpringBonesToVRCPhysBonesConverter
    {
        /// <summary>
        /// 揺れ物のパラメータ変換アルゴリズムの定義を行うコールバック関数。
        /// </summary>
        /// <param name="vrmSpringBoneParameters"></param>
        /// <param name="boneInfo"></param>
        /// <returns></returns>
        public delegate VRCPhysBoneParameters ParametersConverter(
            VRMSpringBoneParameters vrmSpringBoneParameters,
            BoneInfo boneInfo
        );

        /// <summary>
        /// <see cref="ParametersConverter">の既定値。
        /// </summary>
        /// <param name="springBoneParameters"></param>
        /// <param name="boneInfo"></param>
        /// <returns></returns>
        private static VRCPhysBoneParameters DefaultParametersConverter(
            VRMSpringBoneParameters vrmSpringBoneParameters,
            BoneInfo boneInfo
        )
        {
            return new VRCPhysBoneParameters()
            {
                Pull = vrmSpringBoneParameters.StiffnessForce * 0.075f,
                Spring = vrmSpringBoneParameters.DragForce * 0.2f,
                Immobile = 0,
            };
        }

        /// <summary>
        /// VRMSpringBoneをVRCPhysBoneへ変換します。
        /// </summary>
        /// <param name="source">変換元のアバター。</param>
        /// <param name="destination">変換先のアバター。変換元と同一のオブジェクトを指定可能。</param>
        /// <param name="overwriteMode">変換先にすでに揺れ物が存在している場合の設定。</param>
        /// <param name="ignoreColliders">コライダーを変換しないなら <c>true</c> を指定。</param>
        /// <param name="parametersConverter">揺れ物のパラメータ変換アルゴリズム。</param>
        public static void Convert(
            Animator source,
            Animator destination,
            OverwriteMode overwriteMode = OverwriteMode.Replace,
            bool ignoreColliders = false,
            ParametersConverter parametersConverter = null
        )
        {
            if (parametersConverter == null)
            {
                parametersConverter = VRMSpringBonesToVRCPhysBonesConverter.DefaultParametersConverter;
            }

            using (var converter = new Converter(source, destination))
            {
                if (overwriteMode == OverwriteMode.Replace)
                {
                    Utilities.DestroyVRCPhysBones(converter.Destination, converter.DestinationIsAsset);
                }

                // 変換元の VRMSpringBoneColliderGroup を基に、変換先へ VRCPhysBoneCollider を設定
#if VRC_SDK_VRCSDK3
                IDictionary<VRMSpringBoneColliderGroup, List<VRCPhysBoneCollider>>
#else
                dynamic
#endif
                    vrcPhysBoneColliderGroups = null;
                if (!ignoreColliders)
                {
                    var objectsHavingUsedColliderGroup = converter.Source.GetComponentsInChildren<VRMSpringBone>()
                        .SelectMany(springBone => springBone.ColliderGroups)
                        .Select(colliderGroup => colliderGroup.gameObject);

                    // キーに VRMSpringBoneColliderGroup、値に対応する VRCPhysBoneCollider のリストを持つジャグ配列。
                    vrcPhysBoneColliderGroups = converter.Source.GetComponentsInChildren<VRMSpringBoneColliderGroup>()
                        // VRMSpringBone.ColliderGroups から参照されていない VRMSpringBoneColliderGroup を除外
                        .Where(sourceColliderGroup =>
                            objectsHavingUsedColliderGroup.Contains(sourceColliderGroup.gameObject))
                        .ToDictionary(
                            sourceColliderGroup => sourceColliderGroup,
                            sourceColliderGroup => {
                                var sourceBone = sourceColliderGroup.gameObject.transform;
                                var destinationBone = converter.FindCorrespondingBone(
                                    sourceBone,
                                    "VRMSpringBoneColliderGroup → VRCPhysBoneCollider"
                                );
                                if (destinationBone == null)
                                {
                                    return null;
                                }

                                return sourceColliderGroup.Colliders.Select(collider =>
                                {
                                    var vrcPhysBoneCollider
#if VRC_SDK_VRCSDK3
                                        = converter.DestinationIsAsset
                                            ? destinationBone.gameObject.AddComponent<VRCPhysBoneCollider>()
                                            : Undo.AddComponent<VRCPhysBoneCollider>(destinationBone.gameObject);
#else
                                        = (dynamic)null;
#endif
                                    vrcPhysBoneCollider.position = TransformUtilities.CalculateOffset(
                                        sourceBone,
                                        collider.Offset,
                                        destinationBone
                                    );
                                    vrcPhysBoneCollider.radius = TransformUtilities.CalculateDistance(
                                        sourceColliderGroup.transform,
                                        collider.Radius,
                                        destinationBone
                                    );
                                    return vrcPhysBoneCollider;
                                }).ToList();
                            }
                        );
                }

                // 変換元の VRMSpringBone を基に、変換先へ VRCPhysBone を設定
                foreach (var vrmSpringBone in converter.Source.GetComponentsInChildren<VRMSpringBone>())
                {
                    var vrmSpringBoneParameters = new VRMSpringBoneParameters()
                    {
                        StiffnessForce = vrmSpringBone.m_stiffnessForce,
                        DragForce = vrmSpringBone.m_dragForce,
                    };
                    var boneInfo = new BoneInfo(converter.Source.GetComponent<VRMMeta>());

                    foreach (var sourceBone in vrmSpringBone.RootBones)
                    {
                        var destinationBone = converter.FindCorrespondingBone(
                            sourceBone,
                            "VRMSpringBone → VRCPhysBone"
                        );
                        if (destinationBone == null)
                        {
                            continue;
                        }

                        var vrcPhysBone
#if VRC_SDK_VRCSDK3
                            = converter.DestinationIsAsset
                                ? converter.Secondary.AddComponent<VRCPhysBone>()
                                : Undo.AddComponent<VRCPhysBone>(converter.Secondary);
#else
                            = (dynamic)null;
#endif
                        vrcPhysBone.parameter = vrmSpringBone.m_comment;
                        vrcPhysBone.rootTransform = destinationBone;

                        VRCPhysBoneParameters vrcPhaysBoneParameters = null;
                        if (parametersConverter != null)
                        {
                            vrcPhaysBoneParameters = parametersConverter(vrmSpringBoneParameters, boneInfo);
                        }
                        if (vrcPhaysBoneParameters != null)
                        {
                            vrcPhysBone.pull = vrcPhaysBoneParameters.Pull;
                            vrcPhysBone.pullCurve = vrcPhaysBoneParameters.PullCurve;
                            vrcPhysBone.spring = vrcPhaysBoneParameters.Spring;
                            vrcPhysBone.springCurve = vrcPhaysBoneParameters.SpringCurve;
                            vrcPhysBone.immobile = vrcPhaysBoneParameters.Immobile;
                            vrcPhysBone.immobileCurve = vrcPhaysBoneParameters.ImmobileCurve;
                            vrcPhysBone.grabMovement = vrcPhaysBoneParameters.GrabMovement;
                            vrcPhysBone.maxStretch = vrcPhaysBoneParameters.MaxStretch;
                            vrcPhysBone.maxStretchCurve = vrcPhaysBoneParameters.MaxStretchCurve;
                        }

                        vrcPhysBone.gravity = vrmSpringBone.m_gravityPower * 0.05f;
                        vrcPhysBone.radius = TransformUtilities.CalculateDistance(
                            vrmSpringBone.transform,
                            vrmSpringBone.m_hitRadius,
                            converter.Secondary.transform
                        );
                        vrcPhysBone.allowPosing = false;
                        if (vrcPhysBoneColliderGroups != null)
                        {
                            vrcPhysBone.colliders = vrmSpringBone.ColliderGroups
#if VRC_SDK_VRCSDK3
                                .SelectMany(colliderGroup => vrcPhysBoneColliderGroups[colliderGroup])
                                .Select(collider => (VRCPhysBoneColliderBase)collider)
#endif
                                .ToList();
                        }
                    }
                }

                converter.SaveAsset();
            }
        }
    }
}
