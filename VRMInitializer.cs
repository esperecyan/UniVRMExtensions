#nullable enable
using System.Linq;
using UnityEngine;
using UnityEditor;
using VRM;

namespace Esperecyan.UniVRMExtensions
{
    /// <summary>
    /// Hmanoidモデルを直接VRMプレハブにします。
    /// </summary>
    public class VRMInitializer
    {
        /// <summary>
        /// プレハブアセットを上書きしてVRMプレハブにします。
        /// </summary>
        /// <param name="prefabPath">「Assets/」で始まるプレハブアセットのパス。</param>
        /// <param name="prefabInstance"><see cref="PrefabUtility.LoadPrefabContents"/>で開いたプレハブインスタンス。
        ///     指定されていなければ、「prefabPath」を<see cref="PrefabUtility.LoadPrefabContents"/>で開き、処理後、
        ///     <see cref="PrefabUtility.SaveAsPrefabAsset"/>、<see cref="PrefabUtility.UnloadPrefabContents"/>、
        ///     <see cref="PrefabUtility.SaveAssets"/>を実行します。</param>
        public static void Initialize(string prefabPath, GameObject? prefabInstance = null)
        {
            var prefab = prefabInstance != null ? prefabInstance : PrefabUtility.LoadPrefabContents(prefabPath);

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

            if (prefabInstance == null)
            {
                PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
                PrefabUtility.UnloadPrefabContents(prefab);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
