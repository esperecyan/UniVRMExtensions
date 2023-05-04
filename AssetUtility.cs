using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEditor;
using UniGLTF;
using VRM;

namespace Esperecyan.UniVRMExtensions
{
    /// <summary>
    /// アセットの保存。
    /// </summary>
    internal static class AssetUtility
    {    
        /// <summary>
        /// アセットの種類ごとの、複製先のフォルダ名の末尾に追加する文字列。
        /// </summary>
        private static readonly IDictionary<Type, string> TypeSuffixPairs = new Dictionary<Type, string>
        {
            { typeof(VRMMetaObject   ), ".MetaObject"  },
            { typeof(BlendShapeAvatar), ".BlendShapes" },
            { typeof(BlendShapeClip  ), ".BlendShapes" },
        };

        /// <summary>
        /// プレハブアセットを生成します。
        /// </summary>
        /// <param name="gameObject">ヒエラルキー上のオブジェクト、またはプレハブアセット。</param>
        /// <returns></returns>
        internal static string CreatePrefabVariant(GameObject gameObject)
        {
            var path = AssetDatabase.GetAssetPath(gameObject);
            var activeObjectIsPrefabAsset = !string.IsNullOrEmpty(path);
            if (activeObjectIsPrefabAsset)
            {
                gameObject = PrefabUtility.InstantiatePrefab(gameObject) as GameObject;
                if (Path.GetExtension(path) != ".prefab")
                {
                    path = $"{Path.GetDirectoryName(path)}/{Path.GetFileNameWithoutExtension(path)}.prefab";
                }
            }
            else
            {
                path = PrefabUtility.GetNearestPrefabInstanceRoot(gameObject) == gameObject
                    ? PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject)
                    : $"Assets/Fara/Prefab/VRM/{gameObject.name}.prefab";
            }
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            PrefabUtility.SaveAsPrefabAsset(gameObject, path);
            if (activeObjectIsPrefabAsset)
            {
                Object.DestroyImmediate(gameObject);
            }

            AssetDatabase.SaveAssets();

            return path;
        }

        /// <summary>
        /// インスタンスをプレハブが置かれているディレクトリの直下のフォルダへ保存します。
        /// </summary>
        /// <remarks>
        /// 複製先にすでにアセットが存在していれば上書きし、保存先のアセットのGUIDが変わらないようにします。
        /// </remarks>
        /// <param name="prefabPath">「Assets/」から始まるパス。</param>
        /// <param name="instance">保存するインスタンス。</param>
        /// <returns></returns>
        internal static T Save<T>(string prefabPath, T instance) where T : Object
        {
            var destinationPath = AssetUtility.DetermineAssetPath(prefabPath, instance);

            var destination = AssetDatabase.LoadMainAssetAtPath(destinationPath);
            if (destination)
            {
                EditorUtility.CopySerialized(instance, destination);
            }
            else
            {
                AssetDatabase.CreateAsset(instance, destinationPath);
            }

            var asset = AssetDatabase.LoadAssetAtPath<T>(destinationPath);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        /// <summary>
        /// インスタンスの種類に応じて、保存先を決定します。
        /// </summary>
        /// <param name="prefabPath">「Assets/」から始まるパス。</param>
        /// <param name="instance">保存するインスタンス。</param>
        /// <returns>「Assets/」から始まるパス。</returns>
        private static string DetermineAssetPath(string prefabPath, Object instance)
        {
            var destinationFolderUnityPath = UnityPath.FromUnityPath(prefabPath).GetAssetFolder(
                AssetUtility.TypeSuffixPairs
                    .First(typeSuffixPair => typeSuffixPair.Key.IsInstanceOfType(instance)).Value
            );

            destinationFolderUnityPath.EnsureFolder();

            return destinationFolderUnityPath.Child(instance.name + ".asset").Value;
        }
    }
}
