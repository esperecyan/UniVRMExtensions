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
            BlendShapeAvatar sourceBlendShapeAvatar = source.GetComponent<VRMBlendShapeProxy>().BlendShapeAvatar;
            BlendShapeAvatar destinationBlendShapeAvatar
                = destination.GetComponent<VRMBlendShapeProxy>().BlendShapeAvatar;
            if (sourceBlendShapeAvatar == destinationBlendShapeAvatar)
            {
                return;
            }

            foreach (BlendShapeClip sourceClip in sourceBlendShapeAvatar.Clips)
            {
                if (!sourceClip)
                {
                    continue;
                }

                CopyVRMBlendShapes.CopyBlendShapeClip(sourceClip: sourceClip, source: source, destination: destination);
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
            BlendShapeAvatar destinationBlendShapeAvatar
                = destination.GetComponent<VRMBlendShapeProxy>().BlendShapeAvatar;

            BlendShapeClip destinationClip = sourceClip.Preset != BlendShapePreset.Unknown
                ? destinationBlendShapeAvatar.GetClip(preset: sourceClip.Preset)
                : destinationBlendShapeAvatar.GetClip(name: sourceClip.BlendShapeName);
            if (sourceClip == destinationClip)
            {
                return;
            }

            if (!destinationClip)
            {
                destinationClip = BlendShapeAvatar.CreateBlendShapeClip(
                    path: UnityPath.FromAsset(destinationBlendShapeAvatar)
                        .Parent.Child(Path.GetFileName(AssetDatabase.GetAssetPath(sourceClip))).Value
                );
                destinationBlendShapeAvatar.Clips.Add(destinationClip);
                EditorUtility.SetDirty(destinationBlendShapeAvatar);
            }

            destinationClip.Values = sourceClip.Values.Select(binding =>
                CopyVRMBlendShapes.CopyBlendShapeBinding(binding: binding, source: source, destination: destination)
            ).ToArray();

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
            Mesh sourceMesh = CopyVRMBlendShapes.GetMesh(binding: binding, avatar: source);
            if (!sourceMesh)
            {
                return binding;
            }

            string shapeKeyName = sourceMesh.GetBlendShapeName(binding.Index);

            Mesh destinationMesh = CopyVRMBlendShapes.GetMesh(relativePath: binding.RelativePath, avatar: destination);
            if (destinationMesh)
            {
                int index = destinationMesh.GetBlendShapeIndex(shapeKeyName);
                if (index != -1)
                {
                    binding.Index = index;
                    return binding;
                }
            }

            return CopyVRMBlendShapes.FindShapeKey(binding: binding, shapeKeyName: shapeKeyName, avatar: destination);
        }

        /// <summary>
        /// 指定したパスからメッシュを取得します。
        /// </summary>
        /// <param name="binding"></param>
        /// <param name="avatar"></param>
        /// <returns></returns>
        private static Mesh GetMesh(string relativePath, GameObject avatar)
        {
            Transform transform = avatar.transform.Find(relativePath);
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
            Mesh mesh = CopyVRMBlendShapes.GetMesh(relativePath: binding.RelativePath, avatar: avatar);
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

                int index = mesh.GetBlendShapeIndex(shapeKeyName);
                if (index == -1)
                {
                    continue;
                }

                binding.RelativePath = renderer.transform.RelativePathFrom(root: avatar.transform);
                binding.Index = index;
                return binding;
            }

            foreach (var renderer in renderers)
            {
                Mesh mesh = renderer.sharedMesh;
                if (!mesh)
                {
                    continue;
                }

                for (var i = 0; i < mesh.blendShapeCount; i++)
                {
                    string name = mesh.GetBlendShapeName(i);
                    if (!name.EndsWith(shapeKeyName) && !shapeKeyName.EndsWith(name))
                    {
                        continue;
                    }

                    binding.RelativePath = renderer.transform.RelativePathFrom(root: avatar.transform);
                    binding.Index = i;
                    return binding;
                }
            }

            return binding;
        }
    }
}
