using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UniGLTF;
#if VRC_SDK_VRCSDK3
using VRC.SDK3.Dynamics.PhysBone.Components;
#endif
using static Esperecyan.UniVRMExtensions.Utilities.Gettext;
using static Esperecyan.UniVRMExtensions.SwayingObjects.DynamicBones;
using VRM;

namespace Esperecyan.UniVRMExtensions.SwayingObjects
{
    /// <summary>
    /// ダイアログ。
    /// </summary>
    public class Wizard : ScriptableWizard
    {
        /// <summary>
        /// 変換方向。
        /// </summary>
        private enum Direction
        {
            VRCPhysBonesToVRMSpringBones,
            VRMSpringBonesToVRCPhysBones,
            DynamicBonesToVRMSpringBones,
            VRMSpringBonesToDynamicBones,
        }

        private string version;
        private Direction direction = Direction.VRCPhysBonesToVRMSpringBones;
        private Animator source = null;
        private Animator destination = null;
        private bool sourceEqualsDestination = false;
        private bool removeSourceSwayingObjectsWhenSourceEqualsDestination = true;
        private OverwriteMode overwriteMode = OverwriteMode.Replace;
        private bool ignoreColliders = false;
        private MonoScript callbackFunction = null;
        private Delegate parametersConverter;

        /// <summary>
        /// 指定されたオブジェクトが<see cref="DynamicBone"/>、または<see cref="DynamicBoneCollider"/>を含んでいれば、<c>true</c>を返します。
        /// </summary>
        /// <param name="animator"></param>
        /// <returns></returns>
        private static bool ContainsDynamicBone(Animator animator)
        {
            return animator.GetComponentsInChildren<Component>(includeInactive: true).Cast<Component>()
                .Any(component => component != null // missing objectの回避
                    && new[] { "DynamicBone", "DynamicBoneCollider" }.Contains(component.GetType().FullName));
        }

        /// <summary>
        /// 指定されたオブジェクトが<see cref="VRMSpringBone"/>、または<see cref="VRMSpringBoneColliderGroup"/>を含んでいれば、<c>true</c>を返します。
        /// </summary>
        /// <param name="animator"></param>
        /// <returns></returns>
        private static bool ContainsVRMSpringBone(Animator animator)
        {
            return animator.GetComponentsInChildren<VRMSpringBone>(includeInactive: true).Length > 0
                || animator.GetComponentsInChildren<VRMSpringBoneColliderGroup>(includeInactive: true).Length > 0;
        }

        /// <summary>
        /// 指定されたオブジェクトが<see cref="VRCPhysBone"/>、または<see cref="VRCPhysBoneCollider"/>を含んでいれば、<c>true</c>を返します。
        /// </summary>
        /// <param name="animator"></param>
        /// <returns></returns>
        private static bool ContainsVRCPhysBone(Animator animator)
        {
#if VRC_SDK_VRCSDK3
            return animator.GetComponentsInChildren<VRCPhysBone>(includeInactive: true).Length > 0
                || animator.GetComponentsInChildren<VRCPhysBoneCollider>(includeInactive: true).Length > 0;
#else
            throw new PlatformNotSupportedException("VRChat SDK3-Avatars has not been imported.");
#endif
        }

        /// <summary>
        /// ダイアログを開きます。
        /// </summary>
        internal static async void Open()
        {
            var version = await MenuItems.GetVersion();
            var wizard = ScriptableWizard.DisplayWizard<Wizard>(MenuItems.Name + "-" + version, _("Convert"));
            wizard.version = version;
        }

        protected override bool DrawWizardGUI()
        {
            base.DrawWizardGUI();

            this.isValid = true;

            this.direction = (Direction)EditorGUILayout.Popup(
                "",
                (int)this.direction,
                Enum.GetValues(typeof(Direction)).Cast<Direction>().Select(mode => {
                    switch (mode)
                    {
                        case Direction.VRCPhysBonesToVRMSpringBones:
                            return "VRC Phys Bone → VRM Spring Bone";
                        case Direction.VRMSpringBonesToVRCPhysBones:
                            return "VRM Spring Bone → VRC Phys Bone";
                        case Direction.DynamicBonesToVRMSpringBones:
                            return "Dynamic Bone → VRM Spring Bone";
                        case Direction.VRMSpringBonesToDynamicBones:
                            return "VRM Spring Bone → Dynamic Bone";
                        default:
                            return null;
                    }
                }).ToArray()
            );

            switch (this.direction)
            {
                case Direction.VRCPhysBonesToVRMSpringBones:
                case Direction.VRMSpringBonesToVRCPhysBones:
#if !VRC_SDK_VRCSDK3
                    EditorGUILayout.HelpBox(_("VRChat SDK3-Avatars has not been imported."), MessageType.Error);
                    this.isValid = false;
                    return true;
#else
                    break;
#endif
                case Direction.DynamicBonesToVRMSpringBones:
                case Direction.VRMSpringBonesToDynamicBones:
                    if (DynamicBoneType == null)
                    {
                        EditorGUILayout.HelpBox(_("The Dynamic Bone asset has not been imported."), MessageType.Error);
                        this.isValid = false;
                        return true;
                    }
                    break;
            }

            this.source = (Animator)EditorGUILayout
                .ObjectField(_("Conversion source"), this.source, typeof(Animator), allowSceneObjects: true);
            if (!this.ReportRootObjectValidation(this.destination, _("Conversion source")))
            {
                this.isValid = false;
            }
            if (this.source != null)
            {
                string unexistedComponentName = null;
                switch (this.direction)
                {
                    case Direction.VRCPhysBonesToVRMSpringBones:
                        if (!Wizard.ContainsVRCPhysBone(this.source))
                        {
                            unexistedComponentName = "VRCPhysBone/VRCPhysBoneCollider";
                        }
                        break;
                    case Direction.DynamicBonesToVRMSpringBones:
                        if (!Wizard.ContainsDynamicBone(this.source))
                        {
                            unexistedComponentName = "DynamicBone/DynamicBoneCollider";
                        }
                        break;
                    case Direction.VRMSpringBonesToVRCPhysBones:
                    case Direction.VRMSpringBonesToDynamicBones:
                        if (!Wizard.ContainsVRMSpringBone(this.source))
                        {
                            unexistedComponentName = "VRMSpringBone/VRMSpringBoneColliderGroup";
                        }
                        break;
                }
                if (unexistedComponentName != null)
                {
                    EditorGUILayout.HelpBox(
                        string.Format(_("{0} does not exist in Conversion source."), unexistedComponentName),
                        MessageType.Error
                    );
                    this.isValid = false;
                }
            }

            this.destination = (Animator)EditorGUILayout
                .ObjectField(_("Conversion destination"), this.destination, typeof(Animator), allowSceneObjects: true);
            if (!this.ReportRootObjectValidation(this.destination, _("Conversion destination")))
            {
                this.isValid = false;
            }
            if (this.destination != null
                && GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(this.destination.gameObject) > 0)
            {
                EditorGUILayout.HelpBox(_("The destination contains missing scripts."), MessageType.Error);
                this.isValid = false;
            }
            if (this.destination != null && this.overwriteMode == OverwriteMode.Replace)
            {
                string existedComponentName = null;
                switch (this.direction)
                {
                    case Direction.VRCPhysBonesToVRMSpringBones:
                    case Direction.DynamicBonesToVRMSpringBones:
                        if (Wizard.ContainsVRMSpringBone(this.destination))
                        {
                            existedComponentName = "VRMSpringBone/VRMSpringBoneColliderGroup";
                        }
                        break;
                    case Direction.VRMSpringBonesToVRCPhysBones:
                        if (Wizard.ContainsVRCPhysBone(this.destination))
                        {
                            existedComponentName = "VRCPhysBone/VRCPhysBoneCollider";
                        }
                        break;
                    case Direction.VRMSpringBonesToDynamicBones:
                        if (Wizard.ContainsDynamicBone(this.destination))
                        {
                            existedComponentName = "DynamicBone/DynamicBoneCollider";
                        }
                        break;
                }
                if (existedComponentName != null)
                {
                    EditorGUILayout.HelpBox(string.Format(
                        _("{0} already exists in Conversion destination. This components will be removed."),
                        existedComponentName
                    ), MessageType.Warning);
                }
            }

            this.sourceEqualsDestination = this.source != null && this.destination != null
                && this.source.gameObject == this.destination.gameObject;
            if (this.sourceEqualsDestination)
            {
                using (new EditorGUILayout.HorizontalScope(new GUIStyle()
                {
                    margin = new RectOffset(left: (int)EditorGUIUtility.labelWidth, 0, 0, 0),
                }))
                {
                    this.removeSourceSwayingObjectsWhenSourceEqualsDestination = EditorGUILayout.ToggleLeft(
                        _("Remove swaying objects from Conversion source"),
                        this.removeSourceSwayingObjectsWhenSourceEqualsDestination
                    );
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    _("The same object can be specified as the source and destination."),
                    MessageType.None
                );
            }

            this.overwriteMode = (OverwriteMode)EditorGUILayout.Popup(
                _("Overwrite mode"),
                (int)this.overwriteMode,
                Enum.GetValues(typeof(OverwriteMode)).Cast<OverwriteMode>().Select(mode => {
                    switch (mode)
                    {
                        case OverwriteMode.Replace:
                            return _("Remove all swaying objects from the destination before conversion");
                        case OverwriteMode.Append:
                            return _("Append");
                        default:
                            return null;
                    }
                }).ToArray()
            );

            this.ignoreColliders = EditorGUILayout.Toggle(_("Ignore colliders"), this.ignoreColliders);

            var previousWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = EditorGUIUtility.currentViewWidth;
            EditorGUILayout.PrefixLabel(_("Parameters conversion algorithm"));
            EditorGUIUtility.labelWidth = previousWidth;
            using (new EditorGUILayout.HorizontalScope(new GUIStyle()
            {
                margin = new RectOffset(left: (int)EditorGUIUtility.labelWidth, 0, 0, 0),
            }))
            {
                this.callbackFunction = (MonoScript)EditorGUILayout.ObjectField(
                    this.callbackFunction,
                    typeof(MonoScript),
                    allowSceneObjects: false
                );
            }
            if (this.callbackFunction != null)
            {
                switch (this.direction)
                {
                    case Direction.VRCPhysBonesToVRMSpringBones:
                        this.parametersConverter = Delegate.CreateDelegate(
                            type: typeof(VRCPhysBonesToVRMSpringBonesConverter.ParametersConverter),
                            target: this.callbackFunction,
                            method: "Converter",
                            ignoreCase: false,
                            throwOnBindFailure: false
                        );
                        break;
                    case Direction.VRMSpringBonesToVRCPhysBones:
                        this.parametersConverter = Delegate.CreateDelegate(
                            type: typeof(VRMSpringBonesToVRCPhysBonesConverter.ParametersConverter),
                            target: this.callbackFunction,
                            method: "Converter",
                            ignoreCase: false,
                            throwOnBindFailure: false
                        );
                        break;
                    case Direction.DynamicBonesToVRMSpringBones:
                        this.parametersConverter = Delegate.CreateDelegate(
                            type: typeof(DynamicBonesToVRMSpringBonesConverter.ParametersConverter),
                            target: this.callbackFunction,
                            method: "Converter",
                            ignoreCase: false,
                            throwOnBindFailure: false
                        );
                        break;
                    case Direction.VRMSpringBonesToDynamicBones:
                        this.parametersConverter = Delegate.CreateDelegate(
                            type: typeof(VRMSpringBonesToDynamicBonesConverter.ParametersConverter),
                            target: this.callbackFunction,
                            method: "Converter",
                            ignoreCase: false,
                            throwOnBindFailure: false
                        );
                        break;
                }

                if (this.parametersConverter == null)
                {
                    EditorGUILayout.HelpBox(
                        _("There is no “Convert” static method with matching parameters."),
                        MessageType.Error
                    );
                    this.isValid = false;
                }
            }
            var code = "";
            switch (this.direction)
            {
                case Direction.VRCPhysBonesToVRMSpringBones:
                    code = @"
    public static VRMSpringBoneParameters Converter(
        VRCPhysBoneParameters vrcPhysBoneParameters,
        BoneInfo boneInfo
    )
    {
        return new VRMSpringBoneParameters()
        {
            StiffnessForce = vrcPhysBoneParameters.Pull / 0.075f,
            DragForce = vrcPhysBoneParameters.Spring / 2f,
        };
    }";
                    break;
                case Direction.VRMSpringBonesToVRCPhysBones:
                    code = @"
    public static VRCPhysBoneParameters Converter(
        VRMSpringBoneParameters vrmSpringBoneParameters,
        BoneInfo boneInfo
    )
    {
            return new VRCPhysBoneParameters()
            {
                Pull = vrmSpringBoneParameters.StiffnessForce * 0.075f,
                Spring = vrmSpringBoneParameters.DragForce * 0.2f,
                Immobile = 0,
            };
        }
    }";
                    break;
                case Direction.DynamicBonesToVRMSpringBones:
                    code = @"
    public static VRMSpringBoneParameters Converter(
        DynamicBoneParameters dynamicBoneParameters,
        BoneInfo boneInfo
    )
    {
        return new VRMSpringBoneParameters()
        {
            StiffnessForce = dynamicBoneParameters.Elasticity / 0.05f,
            DragForce = dynamicBoneParameters.Damping / 0.6f,
        };
    }";
                    break;
                case Direction.VRMSpringBonesToDynamicBones:
                    code = @"
    public static DynamicBoneParameters Converter(
        VRMSpringBoneParameters vrmSpringBoneParameters,
        BoneInfo boneInfo
    )
    {
        return new DynamicBoneParameters()
        {
            Elasticity = vrmSpringBoneParameters.StiffnessForce * 0.05f,
            Damping = vrmSpringBoneParameters.DragForce * 0.6f,
            Stiffness = 0,
            Inert = 0,
        };
    }";
                    break;
            }
            EditorGUILayout.HelpBox(
                _("Specify a C# script file that contains a static method named “Converter” as shown below."),
                MessageType.None
            );
            EditorGUILayout.HelpBox(@"using UnityEngine;
using Esperecyan.UniVRMExtensions.SwayingObjects;
public class Example : MonoBehaviour
{" + code + "\n}", MessageType.None);

            return true;
        }

        /// <summary>
        /// 指定されたオブジェクトがルートオブジェクトであるかの確認と、エラー表示を行います。
        /// </summary>
        /// <param name="animator"></param>
        /// <param name="label"></param>
        /// <returns>バリデーションエラーがなければ <c>true</c>。</returns>
        private bool ReportRootObjectValidation(Animator animator, string label)
        {
            if (animator == null)
            {
                return false;
            }

            var transform = animator.transform;
            if (transform != transform.root)
            {
                EditorGUILayout.HelpBox(string.Format(_("“{0}” is not root object."), label), MessageType.Error);
                return false;
            }

            return true;
        }

        private async void OnWizardCreate()
        {
            switch (this.direction)
            {
                case Direction.VRCPhysBonesToVRMSpringBones:
                    VRCPhysBonesToVRMSpringBonesConverter.Convert(
                        this.source,
                        this.destination,
                        this.overwriteMode,
                        this.ignoreColliders,
                        (VRCPhysBonesToVRMSpringBonesConverter.ParametersConverter)this.parametersConverter
                    );
                    break;
                case Direction.VRMSpringBonesToVRCPhysBones:
                    VRMSpringBonesToVRCPhysBonesConverter.Convert(
                        this.source,
                        this.destination,
                        this.overwriteMode,
                        this.ignoreColliders,
                        (VRMSpringBonesToVRCPhysBonesConverter.ParametersConverter)this.parametersConverter
                    );
                    break;
                case Direction.DynamicBonesToVRMSpringBones:
                    DynamicBonesToVRMSpringBonesConverter.Convert(
                        this.source,
                        this.destination,
                        this.overwriteMode,
                        this.ignoreColliders,
                        (DynamicBonesToVRMSpringBonesConverter.ParametersConverter)this.parametersConverter
                    );
                    break;
                case Direction.VRMSpringBonesToDynamicBones:
                    VRMSpringBonesToDynamicBonesConverter.Convert(
                        this.source,
                        this.destination,
                        this.overwriteMode,
                        this.ignoreColliders,
                        (VRMSpringBonesToDynamicBonesConverter.ParametersConverter)this.parametersConverter
                    );
                    break;
            }

            if (this.sourceEqualsDestination && this.removeSourceSwayingObjectsWhenSourceEqualsDestination)
            {

                var source = this.source.gameObject;
                var sourceIsAsset = PrefabUtility.IsPartOfPrefabAsset(source);
                int? undoGroupIndex = null;
                try
                {
                    if (sourceIsAsset)
                    {
                        source = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(source));
                    }
                    else
                    {
                        Undo.IncrementCurrentGroup();
                        Undo.SetCurrentGroupName($"Remove Swaying objects from “{source.name}”");
                    }
                    undoGroupIndex = Undo.GetCurrentGroup();

                    switch (this.direction)
                    {
                        case Direction.VRCPhysBonesToVRMSpringBones:
                            Utilities.DestroyVRCPhysBones(source, sourceIsAsset);
                            break;
                        case Direction.DynamicBonesToVRMSpringBones:
                            Utilities.DestroyDynamicBones(source, sourceIsAsset);
                            break;
                        case Direction.VRMSpringBonesToVRCPhysBones:
                        case Direction.VRMSpringBonesToDynamicBones:
                            Utilities.DestroyVRMSpringBones(source, sourceIsAsset);
                            break;
                    }

                    if (sourceIsAsset)
                    {
                        PrefabUtility.SaveAsPrefabAsset(
                            source,
                            PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(this.source)
                        );
                    }
                }
                finally
                {
                    if (sourceIsAsset)
                    {
                        PrefabUtility.UnloadPrefabContents(source);
                    }
                    else
                    {
                        Undo.CollapseUndoOperations(undoGroupIndex.Value);
                    }
                }
            }

            EditorUtility.DisplayDialog(
                MenuItems.Name + "-" + await MenuItems.GetVersion(),
                _("The conversion is completed."),
                _("OK")
            );
        }
    }
}
