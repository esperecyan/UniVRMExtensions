using System.Linq;
using UnityEngine;
using UnityEditor;
using VRM;

namespace Esperecyan.UniVRMExtensions
{
    /// <summary>
    /// Hmanoidモデルを直接VRMプレハブにします。
    /// </summary>
    /// <remarks>
    /// 
    /// 
    /// 動作確認バージョン: UniVRM 0.62.0, Unity 2020.1.16f1
    /// ライセンス: MIT License (MIT) <https://spdx.org/licenses/MIT.html>
    /// 配布元: <https://gist.github.com/esperecyan/>
    /// </remarks>
    public class VRMInitializer
    {
        /// <summary>
        /// 当エディタ拡張の名称。
        /// </summary>
        internal const string Name = "InitializeVRM.cs";

        /// <summary>
        /// 当エディタ拡張のバージョン。
        /// </summary>
        internal const string Version = "1.0.0";

        /// <summary>
        /// プレハブアセットを上書きしてVRMプレハブにします。
        /// </summary>
        /// <param name="prefabPath">「Assets/」で始まるプレハブアセットのパス。</param>
        public static void Initialize(string prefabPath)
        {
            var prefab = PrefabUtility.LoadPrefabContents(prefabPath);

            var animator = prefab.GetComponent<Animator>();

            var metaObject = ScriptableObject.CreateInstance<VRMMetaObject>();
            metaObject.name = "Meta";
            var meta = prefab.AddComponent<VRMMeta>();
            meta.Meta = AssetUtility.Save(prefabPath, metaObject);

            var humanoidDescription = prefab.AddComponent<VRMHumanoidDescription>();
            humanoidDescription.Avatar = animator.avatar;

            var blendShapeProxy = prefab.AddComponent<VRMBlendShapeProxy>();
            var blendShapeAvatar = ScriptableObject.CreateInstance<BlendShapeAvatar>();
            blendShapeAvatar.name = "BlendShape";
            blendShapeProxy.BlendShapeAvatar = AssetUtility.Save(prefabPath, blendShapeAvatar);
            blendShapeProxy.BlendShapeAvatar.CreateDefaultPreset();
            blendShapeAvatar.Clips
                = blendShapeAvatar.Clips.Select(clip => AssetUtility.Save(prefabPath, clip)).ToList();

            var firstPerson = prefab.AddComponent<VRMFirstPerson>();
            firstPerson.SetDefault();
            firstPerson.TraverseRenderers();

            prefab.AddComponent<VRMLookAtHead>();

            var lookAtBoneApplyer = prefab.AddComponent<VRMLookAtBoneApplyer>();
            lookAtBoneApplyer.LeftEye = OffsetOnTransform.Create(animator.GetBoneTransform(HumanBodyBones.LeftEye));
            lookAtBoneApplyer.RightEye = OffsetOnTransform.Create(animator.GetBoneTransform(HumanBodyBones.RightEye));

            var secondary = prefab.transform.Find("secondary");
            if (secondary == null)
            {
                secondary = new GameObject("secondary").transform;
                secondary.SetParent(prefab.transform, false);
            }
            if (secondary.GetComponent<VRMSpringBone>() == null)
            {
                secondary.gameObject.AddComponent<VRMSpringBone>();
            }

            PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefab);
            AssetDatabase.SaveAssets();
        }
    }
}
