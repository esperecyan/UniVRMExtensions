using System.IO;
using UnityEngine;
using UnityEditor;
using UniGLTF;
using VRM;

namespace Esperecyan.UniVRMExtensions.CopyVRMSettingsComponents
{
    internal class CopyMeta
    {
        /// <summary>
        /// モデル情報をコピーします。
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        internal static void Copy(GameObject source, GameObject destination)
        {
            var sourceMeta = source.GetComponent<VRMMeta>().Meta;
            var destinationMeta = destination.GetComponent<VRMMeta>().Meta;
            if (sourceMeta == destinationMeta)
            {
                return;
            }

            CopyMeta.CopyInformation(source: source, destination: destination);
            CopyMeta.CopyLicense(sourceMeta: sourceMeta, destinationMeta: destinationMeta);
            CopyMeta.CopyRedistributionAndModificationsLicense(
                sourceMeta: sourceMeta,
                destinationMeta: destinationMeta
            );

            return;
        }

        /// <summary>
        /// 情報をコピーします。
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        private static void CopyInformation(GameObject source, GameObject destination)
        {
            var sourceMeta = source.GetComponent<VRMMeta>().Meta;
            var destinationMeta = destination.GetComponent<VRMMeta>().Meta;

            destinationMeta.Title = sourceMeta.Title;
            destinationMeta.Version = sourceMeta.Version;
            destinationMeta.Author = sourceMeta.Author;
            destinationMeta.ContactInformation = sourceMeta.ContactInformation;
            destinationMeta.Reference = sourceMeta.Reference;

            destinationMeta.Thumbnail = sourceMeta.Thumbnail;
            if (!destinationMeta.Thumbnail)
            {
                return;
            }

            var sourceThumbnailPath = AssetDatabase.GetAssetPath(destinationMeta.Thumbnail);
            if (UnityPath.FromUnityPath(sourceThumbnailPath).Parent.Value
                != UnityPath.FromAsset(source).GetAssetFolder(suffix: ".Textures").Value)
            {
                return;
            }

            var destinationPrefabPath = CopyVRMSettings.GetPrefabAssetPath(destination);
            if (string.IsNullOrEmpty(destinationPrefabPath))
            {
                return;
            }

            var destinationThumbnailPath = UnityPath.FromUnityPath(destinationPrefabPath)
                .GetAssetFolder(suffix: ".Textures")
                .Child(Path.GetFileName(sourceThumbnailPath)).GenerateUniqueAssetPath().Value;
            AssetDatabase.CopyAsset(sourceThumbnailPath, destinationThumbnailPath);
            destinationMeta.Thumbnail = AssetDatabase.LoadAssetAtPath<Texture2D>(destinationThumbnailPath);
        }

        /// <summary>
        /// 使用許諾・ライセンス情報をコピーします。
        /// </summary>
        /// <param name="sourceMeta"></param>
        /// <param name="destinationMeta"></param>
        private static void CopyLicense(VRMMetaObject sourceMeta, VRMMetaObject destinationMeta)
        {
            destinationMeta.AllowedUser = sourceMeta.AllowedUser;
            destinationMeta.ViolentUssage = sourceMeta.ViolentUssage;
            destinationMeta.SexualUssage = sourceMeta.SexualUssage;
            destinationMeta.CommercialUssage = sourceMeta.CommercialUssage;
            destinationMeta.OtherPermissionUrl = sourceMeta.OtherPermissionUrl;
        }

        /// <summary>
        /// 再配布・改変に関する許諾範囲をコピーします。
        /// </summary>
        /// <param name="sourceMeta"></param>
        /// <param name="destinationMeta"></param>
        private static void CopyRedistributionAndModificationsLicense(
            VRMMetaObject sourceMeta,
            VRMMetaObject destinationMeta
        )
        {
            destinationMeta.LicenseType = sourceMeta.LicenseType;
            destinationMeta.OtherLicenseUrl = sourceMeta.OtherLicenseUrl;
        }
    }
}
