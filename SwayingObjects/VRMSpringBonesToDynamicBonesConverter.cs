using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VRM;
using Esperecyan.UniVRMExtensions.Utilities;
using static Esperecyan.UniVRMExtensions.SwayingObjects.DynamicBones;

namespace Esperecyan.UniVRMExtensions.SwayingObjects
{
    /// <summary>
    /// VRMSpringBoneをDynamicBoneへ変換します。
    /// </summary>
    public class VRMSpringBonesToDynamicBonesConverter
    {
        /// <summary>
        /// 揺れ物のパラメータ変換アルゴリズムの定義を行うコールバック関数。
        /// </summary>
        /// <param name="vrmSpringBoneParameters"></param>
        /// <param name="boneInfo"></param>
        /// <returns></returns>
        public delegate DynamicBoneParameters ParametersConverter(
            VRMSpringBoneParameters vrmSpringBoneParameters,
            BoneInfo boneInfo
        );

        /// <summary>
        /// <see cref="ParametersConverter">の既定値。
        /// </summary>
        /// <param name="springBoneParameters"></param>
        /// <param name="boneInfo"></param>
        /// <returns></returns>
        public static DynamicBoneParameters DefaultParametersConverter(
            VRMSpringBoneParameters vrmSpringBoneParameters,
            BoneInfo boneInfo
        )
        {
            return new DynamicBoneParameters()
            {
                Elasticity = vrmSpringBoneParameters.StiffnessForce * 0.05f,
                Damping = vrmSpringBoneParameters.DragForce * 0.6f,
                Stiffness = 0,
                Inert = 0,
                Gravity = vrmSpringBoneParameters.GravityPower * vrmSpringBoneParameters.GravityDir,
            };
        }

        /// <summary>
        /// VRMSpringBoneをDynamicBoneへ変換します。
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
                parametersConverter = VRMSpringBonesToDynamicBonesConverter.DefaultParametersConverter;
            }

            using (var converter = new Converter(source, destination))
            {
                if (overwriteMode == OverwriteMode.Replace)
                {
                    Utilities.DestroyDynamicBones(converter.Destination, converter.DestinationIsAsset);
                }

                // 変換元の VRMSpringBoneColliderGroup を基に、変換先へ DynamicBoneCollider を設定
                IDictionary<VRMSpringBoneColliderGroup, List<dynamic>> dynamicBoneColliderGroups = null;
                if (!ignoreColliders)
                {
                    var objectsHavingUsedColliderGroup = converter.Source.GetComponentsInChildren<VRMSpringBone>()
                        .SelectMany(springBone => springBone.ColliderGroups)
                        .Select(colliderGroup => colliderGroup.gameObject);

                    // キーに VRMSpringBoneColliderGroup、値に対応する DynamicBoneCollider のリストを持つジャグ配列。
                    dynamicBoneColliderGroups = converter.Source.GetComponentsInChildren<VRMSpringBoneColliderGroup>()
                        // VRMSpringBone.ColliderGroups から参照されていない VRMSpringBoneColliderGroup を除外
                        .Where(sourceColliderGroup =>
                            objectsHavingUsedColliderGroup.Contains(sourceColliderGroup.gameObject))
                        .ToDictionary(
                            sourceColliderGroup => sourceColliderGroup,
                            sourceColliderGroup => {
                                var sourceBone = sourceColliderGroup.gameObject.transform;
                                var destinationBone = converter.FindCorrespondingBone(
                                    sourceBone,
                                    "VRMSpringBoneColliderGroup → DynamicBoneCollider"
                                );
                                if (destinationBone == null)
                                {
                                    return null;
                                }

                                return sourceColliderGroup.Colliders.Select(collider =>
                                {
                                    dynamic dynamicBoneCollider = converter.DestinationIsAsset
                                        ? destinationBone.gameObject.AddComponent(DynamicBoneColliderType)
                                        : Undo.AddComponent(destinationBone.gameObject, DynamicBoneColliderType);
                                    dynamicBoneCollider.m_Center= TransformUtilities.CalculateOffset(
                                        sourceBone,
                                        collider.Offset,
                                        destinationBone
                                    );
                                    dynamicBoneCollider.m_Radius = TransformUtilities.CalculateDistance(
                                        sourceColliderGroup.transform,
                                        collider.Radius,
                                        destinationBone
                                    );
                                    return dynamicBoneCollider;
                                }).ToList();
                            }
                        );
                }

                // 変換元の VRMSpringBone を基に、変換先へ DynamicBone を設定
                foreach (var vrmSpringBone in converter.Source.GetComponentsInChildren<VRMSpringBone>())
                {
                    var vrmSpringBoneParameters = new VRMSpringBoneParameters()
                    {
                        StiffnessForce = vrmSpringBone.m_stiffnessForce,
                        DragForce = vrmSpringBone.m_dragForce,
                        GravityDir = vrmSpringBone.m_gravityDir,
                        GravityPower = vrmSpringBone.m_gravityPower,
                    };
                    var boneInfo = new BoneInfo(converter.Source.GetComponent<VRMMeta>(), vrmSpringBone.m_comment);

                    foreach (var sourceBone in vrmSpringBone.RootBones)
                    {
                        var destinationBone = converter.FindCorrespondingBone(
                            sourceBone,
                            "VRMSpringBone → DynamicBone"
                        );
                        if (destinationBone == null)
                        {
                            continue;
                        }

                        dynamic dynamicBone = converter.DestinationIsAsset
                            ? converter.Secondary.AddComponent(DynamicBoneType)
                            : Undo.AddComponent(converter.Secondary, DynamicBoneType);
                        dynamicBone.m_Root = destinationBone;
                        dynamicBone.m_Exclusions = new List<Transform>();

                        DynamicBoneParameters dynamicBoneParameters = null;
                        if (parametersConverter != null)
                        {
                            dynamicBoneParameters = parametersConverter(vrmSpringBoneParameters, boneInfo);
                        }
                        if (dynamicBoneParameters != null)
                        {
                            dynamicBone.m_Damping = dynamicBoneParameters.Damping;
                            dynamicBone.m_DampingDistrib = dynamicBoneParameters.DampingDistrib;
                            dynamicBone.m_Elasticity = dynamicBoneParameters.Elasticity;
                            dynamicBone.m_ElasticityDistrib = dynamicBoneParameters.ElasticityDistrib;
                            dynamicBone.m_Stiffness = dynamicBoneParameters.Stiffness;
                            dynamicBone.m_StiffnessDistrib = dynamicBoneParameters.StiffnessDistrib;
                            dynamicBone.m_Inert = dynamicBoneParameters.Inert;
                            dynamicBone.m_InertDistrib = dynamicBoneParameters.InertDistrib;
                            dynamicBone.m_Gravity = dynamicBoneParameters.Gravity;
                        }

                        dynamicBone.m_Radius = TransformUtilities.CalculateDistance(
                            vrmSpringBone.transform,
                            vrmSpringBone.m_hitRadius,
                            converter.Secondary.transform
                        );
                        if (dynamicBoneColliderGroups != null)
                        {
                            dynamic colliders = Activator.CreateInstance(DynamicBoneColliderBaseListType);
                            foreach (var collider in vrmSpringBone.ColliderGroups
                                .SelectMany(colliderGroup => dynamicBoneColliderGroups[colliderGroup]))
                            {
                                colliders.Add(collider);
                            }
                            dynamicBone.m_Colliders = colliders;
                        }
                    }
                }

                converter.SaveAsset();
            }
        }
    }
}
