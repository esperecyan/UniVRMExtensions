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
    /// 
    /// 動作確認バージョン: UniVRM 0.55.0, Unity 2018.4.20f1, Unity 2019.3.12f1
    /// ライセンス: MIT License (MIT) <https://spdx.org/licenses/MIT.html>
    /// 配布元: <https://gist.github.com/esperecyan/8a6d19738d4828f6df92a53138b7e315>
    /// </remarks>
    public class CopyVRMSettings
    {
        /// <summary>
        /// 変換元のアバターのルートに設定されている必要があるコンポーネントと、そのフィールド名。
        /// </summary>
        public static readonly IDictionary<Type, string> RequiredComponentsAndFields = new Dictionary<Type, string> {
            { typeof(Animator), "" },
            { typeof(VRMMeta), "Meta" },
            { typeof(VRMHumanoidDescription), "Description" },
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
        /// 当エディタ拡張のバージョン。
        /// </summary>
        /// <remarks>
        /// 
        /// 2.0.0 (2020-05-03)
        ///     VRMSpringBoneのCenterをコピーできていなかった不具合を修正
        ///         ※バーチャルキャストではCenterを設定するとVRMSpringBoneが正常に動作しなくなるという情報があるため、メジャーバージョンを変更
        ///     その他微修正
        /// 1.3.0 (2020-01-19)
        ///     UniVRM-0.54.0の仕様変更により、コピー先のVRMBlendShapeが壊れるようになっていた問題に対処
        ///     <https://github.com/vrm-c/UniVRM/pull/330>
        ///     モデルのメタ情報、VRMBlendShape、一人称視点、視線制御、VRMSpringBoneを個別にコピーできるようにした
        /// 1.2.0 (2019-08-23)
        ///     1.1.1の更新で、1.1.0の前者の更新を差し戻してしまっていたバグを修正
        ///     Unity 2018.1、2018.2 で動作するようにした
        /// 1.1.1 (2019-08-02)
        ///     VRMSpringBoneとVRMSpringBoneColliderGroupのコピーで、相対パスによる検索が行われずにエラーが発生していたバグを修正
        /// 1.1.0 (2019-07-17)
        ///     オブジェクト名を参照していたために、BlendShapeが正常にコピーできていなかったバグを修正
        ///     Unity 2017.4 に対応
        /// 1.0.2 (2019-06-17)
        ///     VRMSpringBoneとVRMSpringBoneColliderGroupのコピーで、オブジェクト名のみによる検索が行われていなかったのを修正
        ///     その他微修正
        /// 1.0.1 (2019-06-15)
        ///     コピーしたBlendShapeがUnity終了時に失われる問題を修正
        ///     プリセット以外のBlendShapeが追加されない問題に対処
        /// 1.0.0 (2019-06-13)
        ///     公開
        /// </remarks>
        public const string Version = "2.0.0";

        /// <summary>
        /// VRMの設定をコピーします。
        /// </summary>
        /// <param name="source">ヒエラルキーのルート、もしくはプレハブのルートであるコピー元のアバター。</param>
        /// <param name="destination">ヒエラルキーのルート、もしくはプレハブのルートであるコピー先のアバター。</param>
        /// <param name="">コピーするコンポーネント。既定値は <see cref="CopyVRMSettings.SupportedComponents">。</param>
        public static void Copy(GameObject source, GameObject destination, IEnumerable<Type> components = null)
        {
            if (components == null)
            {
                components = CopyVRMSettings.SupportedComponents;
            }

            GameObject destinationPrefab = null;
            if (!SceneManager.GetActiveScene().GetRootGameObjects().Contains(destination.gameObject))
            {
                destinationPrefab = destination;
                destination = PrefabUtility.InstantiatePrefab(destination) as GameObject;
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

            if (destinationPrefab)
            {
#if UNITY_2018_3_OR_NEWER
                PrefabUtility.ApplyPrefabInstance(destination, InteractionMode.AutomatedAction);
#else
                PrefabUtility.ReplacePrefab(destination, destinationPrefab, ReplacePrefabOptions.ConnectToPrefab);
#endif
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
#if UNITY_2018_3_OR_NEWER
            return PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefab);
#else
            GameObject root = PrefabUtility.FindPrefabRoot(prefab);
            if (!root)
            {
                return "";
            }
            return AssetDatabase.GetAssetPath(root);
#endif
        }
    }
}
