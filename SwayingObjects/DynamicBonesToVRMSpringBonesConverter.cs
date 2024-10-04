#nullable enable
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UniGLTF;
using VRM;
using Esperecyan.UniVRMExtensions.Utilities;
using static Esperecyan.UniVRMExtensions.SwayingObjects.DynamicBones;

namespace Esperecyan.UniVRMExtensions.SwayingObjects
{
    /// <summary>
    /// DynamicBoneをVRMSpringBoneへ変換します。
    /// </summary>
    public class DynamicBonesToVRMSpringBonesConverter
    {
        /// <summary>
        /// 揺れ物のパラメータ変換アルゴリズムの定義を行うコールバック関数。
        /// </summary>
        /// <param name="dynamicBoneParameters"></param>
        /// <param name="boneInfo"></param>
        /// <returns></returns>
        public delegate VRMSpringBoneParameters ParametersConverter(
            DynamicBoneParameters dynamicBoneParameters,
            BoneInfo boneInfo
        );

        /// <summary>
        /// <see cref="ParametersConverter">の既定値。
        /// </summary>
        /// <param name="springBoneParameters"></param>
        /// <param name="boneInfo"></param>
        /// <returns></returns>
        public static VRMSpringBoneParameters DefaultParametersConverter(
            DynamicBoneParameters dynamicBoneParameters,
            BoneInfo boneInfo
        )
        {
            return new VRMSpringBoneParameters()
            {
                StiffnessForce = dynamicBoneParameters.Elasticity / 0.05f,
                DragForce = dynamicBoneParameters.Damping / 0.6f,
                GravityPower = dynamicBoneParameters.Gravity.magnitude,
                GravityDir = dynamicBoneParameters.Gravity.normalized,
            };
        }

        /// <summary>
        /// DynamicBoneをVRMSpringBoneへ変換します。
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
            ParametersConverter? parametersConverter = null
        )
        {
            if (parametersConverter == null)
            {
                parametersConverter = DynamicBonesToVRMSpringBonesConverter.DefaultParametersConverter;
            }

            using (var converter = new Converter(source, destination))
            {
                if (overwriteMode == OverwriteMode.Replace)
                {
                    Utilities.DestroyVRMSpringBones(converter.Destination, converter.DestinationIsAsset);
                }

                if (!ignoreColliders)
                {
                    DynamicBonesToVRMSpringBonesConverter.SetSpringBoneColliderGroups(converter);
                }
                DynamicBonesToVRMSpringBonesConverter.SetSpringBones(converter, parametersConverter);
                DynamicBonesToVRMSpringBonesConverter.SetSpringBoneColliderGroupsForVirtualCast(converter);

                converter.SaveAsset();
            }
        }

        /// <summary>
        /// 変換元の<see cref="DynamicBoneCollider"/>を基に、変換先へ<see cref="VRMSpringBoneColliderGroup"/>を設定します。
        /// </summary>
        /// <param name="converter"></param>
        private static void SetSpringBoneColliderGroups(Converter converter)
        {
            foreach (var sourceColliders in converter.Source.GetComponentsInChildren(DynamicBoneColliderType)
                // インサイドコライダーを除外
                .Where(sourceCollider =>
                {
                    if ((int)((dynamic)sourceCollider).m_Bound != 0)
                    {
                        Debug.LogWarning("Inside colliders cannot be converted" + ": "
                            + sourceCollider.transform.RelativePathFrom(converter.Source.transform), sourceCollider);
                        return false;
                    }
                    return true;
                })
                // ボーンごとにグループ化
                .GroupBy(collider => collider.transform))
            {
                // 変換先の対応するボーンを取得
                var destinationBone = converter.FindCorrespondingBone(
                    sourceColliders.Key,
                    "DynamicBoneCollider → VRMSpringBoneColliderGroup"
                );
                if (destinationBone == null)
                {
                    // 対応するボーンが存在しなければ
                    continue;
                }

                var destinationColliders = sourceColliders.SelectMany(sourceCollider =>
                        DynamicBonesToVRMSpringBonesConverter.ConvertCollider(sourceCollider, destinationBone));

                var destinationColliderGroup = destinationBone.GetComponent<VRMSpringBoneColliderGroup>();
                if (destinationColliderGroup != null)
                {
                    // すでにコライダーが存在すれば
                    if (!converter.DestinationIsAsset)
                    {
                        Undo.RecordObject(destinationColliderGroup, "");
                    }
                    destinationColliderGroup.Colliders
                        = destinationColliderGroup.Colliders.Concat(destinationColliders).ToArray();
                }
                else
                {
                    (converter.DestinationIsAsset
                        ? destinationBone.gameObject.AddComponent<VRMSpringBoneColliderGroup>()
                        : Undo.AddComponent<VRMSpringBoneColliderGroup>(destinationBone.gameObject)).Colliders
                        = destinationColliders.ToArray();
                }
            }
        }

        /// <summary>
        /// 指定された<see cref="DynamicBoneCollider"/>を基に<see cref="SphereCollider"/>を生成します。
        /// </summary>
        /// <param name="sourceCollider"><see cref="DynamicBoneCollider"/></param>
        /// <param name="destinationBone"></param>
        /// <returns><see cref="DynamicBoneCollider.m_Height"/>が0か直径より小さい場合は1つ、それ以外の場合は3つ。</returns>
        private static IEnumerable<VRMSpringBoneColliderGroup.SphereCollider> ConvertCollider(
            dynamic sourceCollider,
            Transform destinationBone
        )
        {
            var offsets = new List<Vector3>() { sourceCollider.m_Center };
            // カプセルの端から端までの長さ
            if (sourceCollider.m_Height > sourceCollider.m_Radius * 2)
            {
                var distance = (sourceCollider.m_Height - sourceCollider.m_Radius * 2) / 2;
                switch ((int)sourceCollider.m_Direction)
                {
                    case 0: // DynamicBoneColliderBase.Direction.X
                        offsets.Add(offsets[0] + new Vector3(distance, 0, 0));
                        offsets.Add(offsets[0] + new Vector3(-distance, 0, 0));
                        break;
                    case 1: // DynamicBoneColliderBase.Direction.Y
                        offsets.Add(offsets[0] + new Vector3(0, distance, 0));
                        offsets.Add(offsets[0] + new Vector3(0, -distance, 0));
                        break;
                    case 2: // DynamicBoneColliderBase.Direction.Z
                        offsets.Add(offsets[0] + new Vector3(0, 0, distance));
                        offsets.Add(offsets[0] + new Vector3(0, 0, -distance));
                        break;
                }
            }

            return offsets.Select(offset => new VRMSpringBoneColliderGroup.SphereCollider
            {
                Offset = TransformUtilities.CalculateOffset(sourceCollider.transform, offset, destinationBone),
                Radius = TransformUtilities
                    .CalculateDistance(sourceCollider.transform, sourceCollider.m_Radius, destinationBone),
            });
        }

        /// <summary>
        /// 変換元の<see cref="DynamicBone"/>を基に、変換先へ<see cref="VRMSpringBone"/>を設定します。
        /// </summary>
        /// <param name="converter"></param>
        /// <param name="parametersConverter"></param>
        private static void SetSpringBones(Converter converter, ParametersConverter parametersConverter)
        {
            foreach (var dynamicBones in converter.Source.GetComponentsInChildren(DynamicBoneType)
                .Select((dynamic dynamicBone) =>
                {
                    var parameters = parametersConverter(new DynamicBoneParameters()
                    {
                        Damping = dynamicBone.m_Damping,
                        DampingDistrib = dynamicBone.m_DampingDistrib,
                        Elasticity = dynamicBone.m_Elasticity,
                        ElasticityDistrib = dynamicBone.m_ElasticityDistrib,
                        Stiffness = dynamicBone.m_Stiffness,
                        StiffnessDistrib = dynamicBone.m_StiffnessDistrib,
                        Inert = dynamicBone.m_Inert,
                        InertDistrib = dynamicBone.m_InertDistrib,
                        Gravity = dynamicBone.m_Gravity,
                    }, new BoneInfo(converter.Destination.GetComponent<VRMMeta>(), comment: ""));

                    var destinationColliderGroups = new List<VRMSpringBoneColliderGroup>();
                    if (dynamicBone.m_Colliders != null)
                    {
                        foreach (var sourceCollider in dynamicBone.m_Colliders)
                        {
                            if (sourceCollider == null)
                            {
                                // コライダーが削除された、などで消失状態の場合
                                continue;
                            }
                            if (!sourceCollider.transform.IsChildOf(converter.Source.transform))
                            {
                                // ルート外の参照を除外
                                continue;
                            }

                            // 変換先の対応するボーンを取得
                            var destinationBone = converter.FindCorrespondingBone(
                                sourceCollider.transform,
                                target: null
                            );
                            if (destinationBone == null)
                            {
                                // 対応するボーンが存在しなければ
                                continue;
                            }

                            var destinationColliderGroup
                                = destinationBone.GetComponent<VRMSpringBoneColliderGroup>();
                            if (destinationColliderGroup == null
                                || destinationColliderGroups.Contains(destinationColliderGroup))
                            {
                                continue;
                            }

                            destinationColliderGroups.Add(destinationColliderGroup);
                        }
                    }

                    return (dynamicBone, parameters, destinationColliderGroups, compare: string.Join("\n", new[]
                    {
                        parameters.StiffnessForce,
                        parameters.GravityPower,
                        parameters.GravityDir.x,
                        parameters.GravityDir.y,
                        parameters.GravityDir.z,
                        parameters.DragForce,
                        TransformUtilities.CalculateDistance(dynamicBone.transform, dynamicBone.m_Radius),
                }.Select(parameter => parameter.ToString("F2"))
                    .Concat(destinationColliderGroups.Select(colliderGroup =>
                        colliderGroup.transform.RelativePathFrom(converter.Destination.transform)))
                    ));
                })
                .GroupBy(dynamicBones => dynamicBones.compare)) // 同一パラメータでグループ化
            {
                var dynamicBone = dynamicBones.First();

                var vrmSpringBone = converter.DestinationIsAsset
                    ? converter.Secondary.AddComponent<VRMSpringBone>()
                    : Undo.AddComponent<VRMSpringBone>(converter.Secondary);
                vrmSpringBone.m_stiffnessForce = dynamicBone.parameters.StiffnessForce;
                vrmSpringBone.m_gravityPower = dynamicBone.parameters.GravityPower;
                vrmSpringBone.m_gravityDir = dynamicBone.parameters.GravityDir;
                vrmSpringBone.m_dragForce = dynamicBone.parameters.DragForce;
                vrmSpringBone.RootBones = dynamicBones.Select(db => (Transform)db.dynamicBone.m_Root)
                    .Where(sourceBone => sourceBone != null && sourceBone.IsChildOf(converter.Source.transform))
                    .Distinct()
                    // 変換先の対応するボーンを取得
                    .Select(sourceBone => converter.FindCorrespondingBone(sourceBone, "DynamicBone → VRMSpringBone"))
                    .ToList();
                vrmSpringBone.m_hitRadius = TransformUtilities.CalculateDistance(
                    dynamicBone.dynamicBone.transform,
                    dynamicBone.dynamicBone.m_Radius,
                    converter.Secondary.transform
                );
                vrmSpringBone.ColliderGroups = dynamicBone.destinationColliderGroups.ToArray();
            }
        }

        /// <summary>
        /// <see cref="HumanBodyBones.LeftHand"/>、<see cref="HumanBodyBones.RightHand"/>に<see cref="VRMSpringBoneColliderGroup"/>が存在しなければ設定します。
        /// </summary>
        /// <param name="converter"></param>
        private static void SetSpringBoneColliderGroupsForVirtualCast(Converter converter)
        {
            var animator = converter.Destination.GetComponent<Animator>();
            foreach (var bone in new[] { HumanBodyBones.LeftHand, HumanBodyBones.RightHand })
            {
                var hand = animator.GetBoneTransform(bone).gameObject;
                if (hand.GetComponent<VRMSpringBoneColliderGroup>() == null)
                {
                    if (converter.DestinationIsAsset)
                    {
                        hand.AddComponent<VRMSpringBoneColliderGroup>();
                    }
                    else
                    {
                        Undo.AddComponent<VRMSpringBoneColliderGroup>(hand);
                    }
                }
            }
        }
    }
}
