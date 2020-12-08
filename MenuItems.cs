using UnityEngine;
using UnityEditor;
using VRM;

namespace Esperecyan.UniVRMExtensions
{
    /// <summary>
    /// メニューアイテムへの追加とバリデート。
    /// </summary>
    internal static class MenuItems
    {
        /// <summary>
        /// 追加するメニューアイテムの、「VRM」メニュー内の位置。
        /// </summary>
        private const int Priority = 1100;

        [MenuItem("VRM/プレハブバリアントを作ってVRMプレハブ化", false, MenuItems.Priority)]
        private static void Initialize()
        {
            var gameObject = Selection.activeObject as GameObject;
            var animator = gameObject.GetComponent<Animator>();
            if (animator == null || !animator.isHuman)
            {
                EditorUtility.DisplayDialog(
                    VRMInitializer.Name + "-" + VRMInitializer.Version,
                    "HumanoidのAnimatorコンポーネントがアタッチされたGameObjectを選択した状態で実行してください。",
                    "OK"
                );
                return;
            }

            if (gameObject.GetComponent<VRMMeta>() != null)
            {
                EditorUtility.DisplayDialog(
                    VRMInitializer.Name + "-" + VRMInitializer.Version,
                    "選択中のアバターはVRMプレハブです。",
                    "OK"
                );
                return;
            }

            var path = AssetUtility.CreatePrefabVariant(gameObject);

            VRMInitializer.Initialize(path);

            EditorUtility.DisplayDialog(
                VRMInitializer.Name + "-" + VRMInitializer.Version,
                $"「{path}」へVRMプレハブを生成しました。",
                "OK"
            );
        }

        /// <summary>
        /// 選択されているオブジェクトがGameObject、かつサブアセットではなければ <c>true</c> を返します。
        /// </summary>
        /// <returns></returns>
        [MenuItem("VRM/プレハブバリアントを作ってVRMプレハブ化", true)]
        private static bool ActiveObjectIsGameObject()
        {
            return Selection.activeObject is GameObject gameObject && !AssetDatabase.IsSubAsset(gameObject);
        }
    }
}
