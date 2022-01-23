using System;
using UnityEngine;
using UnityEditor;
using VRM;
using Esperecyan.UniVRMExtensions.CopyVRMSettingsComponents;
using SwayingObjectsConverterWizard = Esperecyan.UniVRMExtensions.SwayingObjects.Wizard;

namespace Esperecyan.UniVRMExtensions
{
    /// <summary>
    /// メニューアイテムへの追加とバリデート。
    /// </summary>
    public static class MenuItems
    {
        [Serializable]
        private class Package
        {
            [SerializeField]
#pragma warning disable IDE1006 // 命名スタイル
            internal string version;
#pragma warning restore IDE1006 // 命名スタイル
        }

        /// <summary>
        /// 当エディタ拡張のバージョン。
        /// </summary>
        public static string Version { get; private set; }

        private static readonly string PackageJSONGUID = "b0a763f5e0017914a8effa5e6e1ec9c9";

        /// <summary>
        /// 当エディタ拡張の名称。
        /// </summary>
        internal const string Name = "UniVRMExtensions";

        /// <summary>
        /// 追加するメニューアイテムの、「VRM」メニュー内の位置。
        /// </summary>
        private const int Priority = 1100;

        [MenuItem("VRM0/プレハブバリアントを作ってVRMプレハブ化", false, MenuItems.Priority)]
        private static void Initialize()
        {
            var gameObject = Selection.activeObject as GameObject;
            var animator = gameObject.GetComponent<Animator>();
            if (animator == null || !animator.isHuman)
            {
                EditorUtility.DisplayDialog(
                    MenuItems.Name + "-" + MenuItems.Version,
                    "HumanoidのAnimatorコンポーネントがアタッチされたGameObjectを選択した状態で実行してください。",
                    "OK"
                );
                return;
            }

            if (gameObject.GetComponent<VRMMeta>() != null)
            {
                EditorUtility.DisplayDialog(
                    MenuItems.Name + "-" + MenuItems.Version,
                    "選択中のアバターはVRMプレハブです。",
                    "OK"
                );
                return;
            }

            var path = AssetUtility.CreatePrefabVariant(gameObject);

            VRMInitializer.Initialize(path);

            EditorUtility.DisplayDialog(
                MenuItems.Name + "-" + MenuItems.Version,
                $"「{path}」へVRMプレハブを生成しました。",
                "OK"
            );
        }

        /// <summary>
        /// 選択されているオブジェクトがGameObject、かつサブアセットではなければ <c>true</c> を返します。
        /// </summary>
        /// <returns></returns>
        [MenuItem("VRM0/プレハブバリアントを作ってVRMプレハブ化", true)]
        private static bool ActiveObjectIsGameObject()
        {
            return Selection.activeObject is GameObject gameObject && !AssetDatabase.IsSubAsset(gameObject);
        }

        /// <summary>
        /// 選択されているアバターの変換ダイアログを開きます。
        /// </summary>
        [MenuItem("VRM0/Open CopyVRMSettings Wizard", false, MenuItems.Priority + 1)]
        private static void OpenWizard()
        {
            Wizard.Open();
        }

        /// <summary>
        /// 揺れ物の相互変換ダイアログを開きます。
        /// </summary>
        [MenuItem("VRM0/Open Swaying Objects Converter Wizard", false, MenuItems.Priority + 2)]
        private static void OpenSwayingObjectsConverterWizard()
        {
            SwayingObjectsConverterWizard.Open();
        }

        [InitializeOnLoadMethod]
        private static void LoadVersion()
        {
            var package = AssetDatabase.LoadAssetAtPath<TextAsset>(
                AssetDatabase.GUIDToAssetPath(MenuItems.PackageJSONGUID)
            );
            if (package == null)
            {
                return;
            }
            MenuItems.Version = JsonUtility.FromJson<Package>(package.text).version;
        }
    }
}
