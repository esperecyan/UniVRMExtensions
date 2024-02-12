#nullable enable
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using VRM;
using Esperecyan.UniVRMExtensions.CopyVRMSettingsComponents;
using Esperecyan.UniVRMExtensions.Utilities;

namespace Esperecyan.UniVRMExtensions
{
    /// <summary>
    /// セットアップ済みのVRMプレハブから、正規化直後のVRMプレハブへ、UniVRMのコンポーネントの設定をコピーします。
    /// </summary>
    /// <remarks>
    /// • モデルのメタ情報
    /// • VRMBlendShape
    /// • 一人称視点
    /// • 視線制御
    /// • VRMSpringBoneとVRMSpringBoneColliderGroup
    /// </remarks>
    public class CopyVRMSettings
    {
        /// <summary>
        /// 変換元のアバターのルートに設定されている必要があるコンポーネントと、そのフィールド名。
        /// </summary>
        public static readonly IDictionary<Type, string> RequiredComponentsAndFields = new Dictionary<Type, string> {
            { typeof(Animator), "" },
            { typeof(VRMMeta), "Meta" },
            { typeof(VRMBlendShapeProxy), "BlendShapeAvatar" },
        };

        /// <summary>
        /// <see cref="CopyVRMSettings.Copy"/> の第3引数 <c>components</c> に指定可能な値。
        /// </summary>
        public static readonly IEnumerable<Type> SupportedComponents = new[] {
            typeof(VRMMeta),
            typeof(VRMBlendShapeProxy),
            typeof(VRMFirstPerson),
            typeof(VRMLookAtHead),
            typeof(VRMSpringBone),
        };

        /// <summary>
        /// VRMの設定をコピーします。
        /// </summary>
        /// <param name="source">ヒエラルキーのルート、もしくはプレハブのルートであるコピー元のアバター。</param>
        /// <param name="destination">ヒエラルキーのルート、もしくはプレハブのルートであるコピー先のアバター。</param>
        /// <param name="">コピーするコンポーネント。既定値は <see cref="CopyVRMSettings.SupportedComponents">。</param>
        public static void Copy(GameObject source, GameObject destination, IEnumerable<Type>? components = null)
        {
            if (components == null)
            {
                components = CopyVRMSettings.SupportedComponents;
            }

            GameObject? destinationPrefab = null;
            if (!SceneManager.GetActiveScene().GetRootGameObjects().Contains(destination))
            {
                destinationPrefab = destination;
                destination = (GameObject)PrefabUtility.InstantiatePrefab(destination);
            }

            if (components.Contains(typeof(VRMMeta)))
            {
                CopyMeta.Copy(source: source, destination: destination);
            }
            if (components.Contains(typeof(VRMBlendShapeProxy)))
            {
                CopyVRMBlendShapes.Copy(source: source, destination: destination);
            }

            var sourceSkeletonBones = BoneMapper.GetAllSkeletonBones(avatar: source);
            if (components.Contains(typeof(VRMFirstPerson)))
            {
                CopyFirstPerson.Copy(source: source, destination: destination, sourceSkeletonBones: sourceSkeletonBones);
            }
            if (components.Contains(typeof(VRMLookAtHead)))
            {
                CopyEyeControl.Copy(source: source, destination: destination, sourceSkeletonBones: sourceSkeletonBones);
            }
            if (components.Contains(typeof(VRMSpringBone)))
            {
                CopyVRMSpringBones.Copy(source: source, destination: destination, sourceSkeletonBones: sourceSkeletonBones);
            }

            if (destinationPrefab != null)
            {
                PrefabUtility.ApplyPrefabInstance(destination, InteractionMode.AutomatedAction);
                UnityEngine.Object.DestroyImmediate(destination);
            }
        }

        /// <summary>
        /// 当エディタ拡張の名称。
        /// </summary>
        internal const string Name = "CopyVRMSettings.cs";

        /// <summary>
        /// プレハブのパスを返します。
        /// </summary>
        /// <param name="prefab">プレハブ、またはプレハブのインスタンス。</param>
        /// <returns>「Assets/」から始まるパス。プレハブのインスタンスでなかった場合は空文字列。</returns>
        internal static string GetPrefabAssetPath(GameObject prefab)
        {
            return PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefab);
        }
    }
}
