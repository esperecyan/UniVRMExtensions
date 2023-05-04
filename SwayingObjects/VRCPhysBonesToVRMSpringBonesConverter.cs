using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UniGLTF;
using VRM;
using VRC.Core;
using VRC.SDK3.Avatars.Components;
#if VRC_SDK_VRCSDK3
using VRC.Dynamics;
using VRC.SDK3.Dynamics.PhysBone.Components;
#endif
using Esperecyan.UniVRMExtensions.Utilities;

namespace Esperecyan.UniVRMExtensions.SwayingObjects
{
    /// <summary>
    /// VRCPhysBoneをVRMSpringBoneへ変換します。
    /// </summary>
    public class VRCPhysBonesToVRMSpringBonesConverter : MonoBehaviour
    {
        /// <summary>
        /// 揺れ物のパラメータ変換アルゴリズムの定義を行うコールバック関数。
        /// </summary>
        /// <param name="vrcPhysBoneParameters"></param>
        /// <param name="boneInfo"></param>
        /// <returns></returns>
        public delegate VRMSpringBoneParameters ParametersConverter(
            VRCPhysBoneParameters vrcPhysBoneParameters,
            BoneInfo boneInfo
        );

        /// <summary>
        /// <see cref="ParametersConverter">の既定値。
        /// </summary>
        /// <param name="springBoneParameters"></param>
        /// <param name="boneInfo"></param>
        /// <returns></returns>
        private static VRMSpringBoneParameters DefaultParametersConverter(
            VRCPhysBoneParameters vrcPhysBoneParameters,
            BoneInfo boneInfo
        )
        {
            return new VRMSpringBoneParameters()
            {
                StiffnessForce = vrcPhysBoneParameters.Pull / 0.075f,
                DragForce = vrcPhysBoneParameters.Spring / 2f,
            };
        }

#if !VRC_SDK_VRCSDK3
        private class VRCPhysBoneColliderBase
        {
            internal enum ShapeType
            {
                Plane,
                Capsule,
            }
        }
#endif

        /// <summary>
        /// VRCPhysBoneをVRMSpringBoneへ変換します。
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
                parametersConverter = VRCPhysBonesToVRMSpringBonesConverter.DefaultParametersConverter;
            }

            using (var converter = new Converter(source, destination))
            {
                if (overwriteMode == OverwriteMode.Replace)
                {
                    Utilities.DestroyVRMSpringBones(converter.Destination, converter.DestinationIsAsset);
                }

                if (!ignoreColliders)
                {
                    VRCPhysBonesToVRMSpringBonesConverter.SetSpringBoneColliderGroups(converter);
                }
                VRCPhysBonesToVRMSpringBonesConverter.SetSpringBoneColliderGroupsForVirtualCast(converter);
                VRCPhysBonesToVRMSpringBonesConverter.SetSpringBones(converter, parametersConverter, ignoreColliders);
                VRCPhysBonesToVRMSpringBonesConverter.RemoveVRCComponents(converter);

                converter.SaveAsset();
            }
        }

        /// <summary>
        /// 変換元の<see cref="VRCPhysBoneCollider"/>を基に、変換先へ<see cref="VRMSpringBoneColliderGroup"/>を設定します。
        /// </summary>
        /// <param name="converter"></param>
        private static void SetSpringBoneColliderGroups(Converter converter)
        {
            foreach (var sourceColliders in converter.Source.GetComponentsInChildren<
#if VRC_SDK_VRCSDK3
                VRCPhysBoneCollider
#else
                dynamic
#endif
            >()
                // ボーンごとにグループ化
                .GroupBy(collider => collider.transform))
            {
                // 変換先の対応するボーンを取得
                var destinationBone = converter.FindCorrespondingBone(
                    sourceColliders.Key,
                    "VRCPhysBoneCollider → VRMSpringBoneColliderGroup"
                );
                if (destinationBone == null)
                {
                    // 対応するボーンが存在しなければ
                    continue;
                }

                var destinationColliders = sourceColliders.SelectMany(sourceCollider =>
#if !VRC_SDK_VRCSDK3
                    (IEnumerable<dynamic>)
#endif
                    VRCPhysBonesToVRMSpringBonesConverter.ConvertCollider(sourceCollider, destinationBone));

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
        /// 指定された<see cref="VRCPhysBoneCollider"/>を基に<see cref="SphereCollider"/>を生成します。
        /// </summary>
        /// <param name="sourceCollider"></param>
        /// <param name="destinationBone"></param>
        /// <returns><see cref="VRCPhysBoneCollider.height"/>が0か直径より小さい場合は1つ、それ以外の場合は3つ。</returns>
        private static IEnumerable<VRMSpringBoneColliderGroup.SphereCollider> ConvertCollider(
#if VRC_SDK_VRCSDK3
            VRCPhysBoneCollider
#else
            dynamic
#endif
                sourceCollider,
            Transform destinationBone
        )
        {
            if (sourceCollider.shapeType == VRCPhysBoneColliderBase.ShapeType.Plane)
            {
                Debug.LogWarning("Plane colliders cannot be converted" + ": "
                    + sourceCollider.transform.RelativePathFrom(sourceCollider.transform), sourceCollider);
            }

            var offsets = new List<Vector3>() { sourceCollider.position };
            if (sourceCollider.shapeType == VRCPhysBoneColliderBase.ShapeType.Capsule
                // カプセルの端から端までの長さ
                && sourceCollider.height > sourceCollider.radius * 2)
            {
                var distance = (sourceCollider.height - sourceCollider.radius * 2) / 2;
                offsets.Add(offsets[0] + sourceCollider.rotation * new Vector3(0, 0, distance));
                offsets.Add(offsets[0] + sourceCollider.rotation * new Vector3(0, 0, -distance));
            }

            return offsets.Select(offset => new VRMSpringBoneColliderGroup.SphereCollider
            {
                Offset = TransformUtilities.CalculateOffset(sourceCollider.transform, offset, destinationBone),
                Radius = TransformUtilities
                    .CalculateDistance(sourceCollider.transform, sourceCollider.radius, destinationBone),
            });
        }

        /// <summary>
        /// 変換元の<see cref="VRCPhysBone"/>を基に、変換先へ<see cref="VRMSpringBone"/>を設定します。
        /// </summary>
        /// <param name="converter"></param>
        /// <param name="parametersConverter"></param>
        /// <param name="ignoreColliders"><c>true</c> なら、揺れ物の設定に依らず、揺れ物のコライダー一覧へ手のコライダーを追加しません。</param>
        private static void SetSpringBones(
            Converter converter,
            ParametersConverter parametersConverter,
            bool ignoreColliders
        )
        {
            var destinationAnimator = converter.Destination.GetComponent<Animator>();
            var destinationHandColliderGroups = new[] { HumanBodyBones.LeftHand, HumanBodyBones.RightHand }
                .Select(humanBoneId =>
                    destinationAnimator.GetBoneTransform(humanBoneId).GetComponent<VRMSpringBoneColliderGroup>());

            foreach (var vrcPhysBones in converter.Source.GetComponentsInChildren<
#if VRC_SDK_VRCSDK3
                VRCPhysBone
#else
                dynamic
#endif
                >()
                .Select(vrcPhysBone =>
                {
                    var parameters = parametersConverter(new VRCPhysBoneParameters()
                    {
                        Pull = vrcPhysBone.pull,
                        PullCurve = vrcPhysBone.pullCurve,
                        Spring = vrcPhysBone.spring,
                        SpringCurve = vrcPhysBone.springCurve,
                        Stiffness = vrcPhysBone.stiffness,
                        StiffnessCurve = vrcPhysBone.stiffnessCurve,
#if VRC_SDK_VRCSDK3
                        ImmobileType = vrcPhysBone.immobileType,
#endif
                        Immobile = vrcPhysBone.immobile,
                        ImmobileCurve = vrcPhysBone.immobileCurve,
                        GrabMovement = vrcPhysBone.grabMovement,
                        MaxStretch = vrcPhysBone.maxStretch,
                        MaxStretchCurve = vrcPhysBone.maxStretchCurve,
                    }, new BoneInfo(converter.Destination.GetComponent<VRMMeta>(), vrcPhysBone.parameter));

                    var destinationColliderGroups = new List<VRMSpringBoneColliderGroup>();
                    if (vrcPhysBone.colliders != null)
                    {
                        foreach (var sourceCollider in vrcPhysBone.colliders)
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

                    if (
                        !ignoreColliders
#if VRC_SDK_VRCSDK3
                            && (vrcPhysBone.allowCollision != VRCPhysBoneBase.AdvancedBool.False
                                || vrcPhysBone.allowGrabbing != VRCPhysBoneBase.AdvancedBool.False)
#endif
                    )
                    {
                        // コライダーの変換が有効、かつデフォルトのコライダーとの干渉を許可か掴むのを許可していれば
                        foreach (var colliderGroup in destinationHandColliderGroups)
                        {
                            if (destinationColliderGroups.Contains(colliderGroup))
                            {
                                // すでに手のコライダーが含まれていれば
                                continue;
                            }

                            // 手のコライダーを揺れ物へ追加
                            destinationColliderGroups.Add(colliderGroup);
                        }
                    }

                    return (vrcPhysBone, parameters, destinationColliderGroups, compare: string.Join("\n", new[]
                    {
                        parameters.StiffnessForce,
                        vrcPhysBone.gravity,
                        parameters.DragForce,
                        TransformUtilities.CalculateDistance(vrcPhysBone.transform, vrcPhysBone.radius),
                }.Select(parameter => parameter.ToString("F2"))
                    .Concat(destinationColliderGroups.Select(colliderGroup =>
                        colliderGroup.transform.RelativePathFrom(converter.Destination.transform)
                            + vrcPhysBone.parameter))
                    ));
                })
                .GroupBy(vrcPhysBones => vrcPhysBones.compare)) // 同一パラメータでグループ化
            {
                var vrcPhysBone = vrcPhysBones.First();

                var vrmSpringBone = converter.DestinationIsAsset
                    ? converter.Secondary.AddComponent<VRMSpringBone>()
                    : Undo.AddComponent<VRMSpringBone>(converter.Secondary);
                vrmSpringBone.m_comment = vrcPhysBone.vrcPhysBone.parameter;
                vrmSpringBone.m_stiffnessForce = vrcPhysBone.parameters.StiffnessForce;
                vrmSpringBone.m_gravityPower = vrcPhysBone.vrcPhysBone.gravity / 0.05f;
                vrmSpringBone.m_dragForce = vrcPhysBone.parameters.DragForce;
                vrmSpringBone.m_center = converter.Secondary.transform; // Centerにsecondaryを設定
                vrmSpringBone.RootBones = vrcPhysBones
                    .Select(db => (/* SDK3未インポート用 */Transform)
                        (db.vrcPhysBone.rootTransform != null ? db.vrcPhysBone.rootTransform : db.vrcPhysBone.transform))
                        // VRCPhysBoneコンポーネントのRoot Transformが設定されていない場合、
                        // コンポーネントが設定されたオブジェクトがRoot Transformとして扱われる
                        // <https://docs.vrchat.com/docs/physbones#transforms>
                    .Where(sourceBone => sourceBone.IsChildOf(converter.Source.transform))
                    .Distinct()
                    // 変換先の対応するボーンを取得
                    .Select(sourceBone => converter.FindCorrespondingBone(sourceBone, "VRCPhysBone → VRMSpringBone"))
                    .ToList();
                vrmSpringBone.m_hitRadius = TransformUtilities.CalculateDistance(
                    vrcPhysBone.vrcPhysBone.transform,
                    vrcPhysBone.vrcPhysBone.radius,
                    converter.Secondary.transform
                );
                vrmSpringBone.ColliderGroups = vrcPhysBone.destinationColliderGroups.ToArray();
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

        /// <summary>
        ///
        /// </summary>
        /// <param name="converter"></param>
        private static void RemoveVRCComponents(Converter converter)
        {
            var descriptor = converter.Destination.GetComponent<VRCAvatarDescriptor>();
            var pipeline = converter.Destination.GetComponent<PipelineManager>();
            if (descriptor != null) DestroyImmediate(descriptor);
            if (pipeline != null) DestroyImmediate(pipeline);
        }
    }
}
