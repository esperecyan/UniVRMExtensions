using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
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
        /// <summary>
        /// 当エディタ拡張のバージョン。
        /// </summary>
        public static string Version { get; private set; }

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
            var request = Client.List(offlineMode: true, includeIndirectDependencies: true);
            void Handler()
            {
                if (!request.IsCompleted)
                {
                    return;
                }

                EditorApplication.update -= Handler;

                MenuItems.Version
                    = request.Result.FirstOrDefault(info => info.name == "jp.pokemori.univrm-extensions")?.version;
            }

            EditorApplication.update += Handler;
        }
    }
}
