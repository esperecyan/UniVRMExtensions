using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UniGLTF;
using VRM;

namespace Esperecyan.UniVRMExtensions.CopyVRMSettingsComponents
{
    internal class CopyVRMBlendShapes
    {
        /// <summary>
        /// VRMBlendShapeをコピーします。
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        internal static void Copy(GameObject source, GameObject destination)
        {
            var sourceBlendShapeAvatar = source.GetComponent<VRMBlendShapeProxy>().BlendShapeAvatar;
            var destinationBlendShapeAvatar = destination.GetComponent<VRMBlendShapeProxy>().BlendShapeAvatar;
            if (sourceBlendShapeAvatar == destinationBlendShapeAvatar)
            {
                return;
            }

            foreach (var sourceClip in sourceBlendShapeAvatar.Clips)
            {
                if (!sourceClip)
                {
                    continue;
                }

                CopyVRMBlendShapes.CopyBlendShapeClip(sourceClip, source, destination);
            }
        }

        /// <summary>
        /// コピー元のアバターのBlendShapeClipを基に、コピー先のアバターのBlendShapeClipを書き替えます。
        /// </summary>
        /// <param name="sourceClip"></param>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        private static void CopyBlendShapeClip(BlendShapeClip sourceClip, GameObject source, GameObject destination)
        {
            var destinationBlendShapeAvatar = destination.GetComponent<VRMBlendShapeProxy>().BlendShapeAvatar;

            var destinationClip = sourceClip.Preset != BlendShapePreset.Unknown
                ? destinationBlendShapeAvatar.GetClip(sourceClip.Preset)
                : destinationBlendShapeAvatar.GetClip(sourceClip.BlendShapeName);
            if (sourceClip == destinationClip)
            {
                return;
            }

            if (!destinationClip)
            {
                destinationClip = BlendShapeAvatar.CreateBlendShapeClip(
                    UnityPath.FromAsset(destinationBlendShapeAvatar)
                        .Parent.Child(Path.GetFileName(AssetDatabase.GetAssetPath(sourceClip))).Value
                );
                destinationClip.BlendShapeName = sourceClip.BlendShapeName;
                destinationBlendShapeAvatar.Clips.Add(destinationClip);
                EditorUtility.SetDirty(destinationBlendShapeAvatar);
            }

            destinationClip.Values = sourceClip.Values
                .Select(binding => CopyVRMBlendShapes.CopyBlendShapeBinding(binding, source, destination)).ToArray();

            destinationClip.MaterialValues = sourceClip.MaterialValues.ToArray();

            EditorUtility.SetDirty(destinationClip);
        }

        /// <summary>
        /// コピー元のアバターのBlendShapeBindingを基に、コピー先のアバターのBlendShapeBindingを生成します。
        /// </summary>
        /// <param name="sourceBinding"></param>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        private static BlendShapeBinding CopyBlendShapeBinding(
            BlendShapeBinding binding,
            GameObject source,
            GameObject destination
        )
        {
            var sourceMesh = CopyVRMBlendShapes.GetMesh(binding, source);
            if (!sourceMesh)
            {
                return binding;
            }

            var shapeKeyName = sourceMesh.GetBlendShapeName(binding.Index);

            var destinationMesh = CopyVRMBlendShapes.GetMesh(binding.RelativePath, destination);
            if (destinationMesh)
            {
                var index = destinationMesh.GetBlendShapeIndex(shapeKeyName);
                if (index != -1)
                {
                    binding.Index = index;
                    return binding;
                }
            }

            return CopyVRMBlendShapes.FindShapeKey(binding, shapeKeyName, destination);
        }

        /// <summary>
        /// 指定したパスからメッシュを取得します。
        /// </summary>
        /// <param name="binding"></param>
        /// <param name="avatar"></param>
        /// <returns></returns>
        private static Mesh GetMesh(string relativePath, GameObject avatar)
        {
            var transform = avatar.transform.Find(relativePath);
            if (!transform)
            {
                return null;
            }

            var renderer = transform.GetComponent<SkinnedMeshRenderer>();
            if (!renderer)
            {
                return null;
            }

            return renderer.sharedMesh;
        }

        /// <summary>
        /// BlendShapeBindingに対応するメッシュを返します。
        /// </summary>
        /// <param name="binding"></param>
        /// <param name="avatar"></param>
        /// <returns></returns>
        private static Mesh GetMesh(BlendShapeBinding binding, GameObject avatar)
        {
            var mesh = CopyVRMBlendShapes.GetMesh(binding.RelativePath, avatar);
            if (!mesh || binding.Index > mesh.blendShapeCount)
            {
                return null;
            }

            return mesh;
        }

        /// <summary>
        /// 指定されたシェイプキー名を持つメッシュを探し、見つからなければ後方一致するものを探し、BlendShapeBindingを書き替えて返します。
        /// </summary>
        /// <param name="binding"></param>
        /// <param name="shapeKeyName"></param>
        /// <param name="avatar"></param>
        /// <returns>見つからなかった場合は <c>binding</c> をそのまま返します。</returns>
        private static BlendShapeBinding FindShapeKey(BlendShapeBinding binding, string shapeKeyName, GameObject avatar)
        {
            var renderers = avatar.GetComponentsInChildren<SkinnedMeshRenderer>();

            foreach (var renderer in renderers)
            {
                Mesh mesh = renderer.sharedMesh;
                if (!mesh)
                {
                    continue;
                }

                var index = mesh.GetBlendShapeIndex(shapeKeyName);
                if (index == -1)
                {
                    continue;
                }

                binding.RelativePath = renderer.transform.RelativePathFrom(avatar.transform);
                binding.Index = index;
                return binding;
            }

            foreach (var renderer in renderers)
            {
                var mesh = renderer.sharedMesh;
                if (!mesh)
                {
                    continue;
                }

                for (var i = 0; i < mesh.blendShapeCount; i++)
                {
                    var name = mesh.GetBlendShapeName(i);
                    if (!name.EndsWith(shapeKeyName) && !shapeKeyName.EndsWith(name))
                    {
                        continue;
                    }

                    binding.RelativePath = renderer.transform.RelativePathFrom(avatar.transform);
                    binding.Index = i;
                    return binding;
                }
            }

            return binding;
        }
    }
}
