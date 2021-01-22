using UnityEngine;
using UnityEditor;
using VRM;
using Esperecyan.UniVRMExtensions.CopyVRMSettingsComponents;

namespace Esperecyan.UniVRMExtensions
{
    /// <summary>
    /// メニューアイテムへの追加とバリデート。
    /// </summary>
    internal static class MenuItems
    {
        /// <summary>
        /// 当エディタ拡張の名称。
        /// </summary>
        internal const string Name = "UniVRMExtensions";

        /// <summary>
        /// 当エディタ拡張のバージョン。
        /// </summary>
        internal const string Version = "1.6.0";

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
    }
}
