using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UniGLTF;
using static Esperecyan.UniVRMExtensions.Utilities.Gettext;
using static Esperecyan.UniVRMExtensions.SwayingObjects.DynamicBones;

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
            DynamicBonesToVRMSpringBones,
            VRMSpringBonesToDynamicBones,
        }

        private Direction direction = Direction.DynamicBonesToVRMSpringBones;
        private Animator source = null;
        private Animator destination = null;
        private OverwriteMode overwriteMode = OverwriteMode.Replace;
        private bool ignoreColliders = false;
        private MonoScript callbackFunction = null;
        private VRMSpringBonesToDynamicBonesConverter.ParametersConverter
            vrmSpringBoneToDynamicBoneParametersConverter;
        private DynamicBonesToVRMSpringBonesConverter.ParametersConverter
            dynamicBoneToVRMSpringBoneToParametersConverter;

        /// <summary>
        /// ダイアログを開きます。
        /// </summary>
        internal static void Open()
        {
            ScriptableWizard.DisplayWizard<Wizard>(MenuItems.Name + "-" + MenuItems.Version, _("Convert"));
        }

        protected override bool DrawWizardGUI()
        {
            base.DrawWizardGUI();

            if (DynamicBoneType == null)
            {
                EditorGUILayout.HelpBox(_("The Dynamic Bone asset has not been imported."), MessageType.Error);
                this.isValid = false;
                return true;
            }

            this.isValid = true;

            this.direction = (Direction)EditorGUILayout.Popup(
                "",
                (int)this.direction,
                Enum.GetValues(typeof(Direction)).Cast<Direction>().Select(mode => {
                    switch (mode)
                    {
                        case Direction.DynamicBonesToVRMSpringBones:
                            return "Dynamic Bone → VRM Spring Bone";
                        case Direction.VRMSpringBonesToDynamicBones:
                            return "VRM Spring Bone → Dynamic Bone";
                        default:
                            return null;
                    }
                }).ToArray()
            );

            this.source = (Animator)EditorGUILayout
                .ObjectField(_("Conversion source"), this.source, typeof(Animator), allowSceneObjects: true);

            this.destination = (Animator)EditorGUILayout
                .ObjectField(_("Conversion destination"), this.destination, typeof(Animator), allowSceneObjects: true);

            EditorGUILayout.HelpBox(
                _("The same object can be specified as the source and destination."),
                MessageType.None
            );

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

            this.callbackFunction = (MonoScript)EditorGUILayout.ObjectField(
                _("Parameters conversion algorithm"),
                this.callbackFunction,
                typeof(MonoScript),
                allowSceneObjects: false
            );
            if (this.callbackFunction != null)
            {
                switch (this.direction)
                {
                    case Direction.DynamicBonesToVRMSpringBones:
                        this.dynamicBoneToVRMSpringBoneToParametersConverter
                            = (DynamicBonesToVRMSpringBonesConverter.ParametersConverter)Delegate.CreateDelegate(
                                type: typeof(DynamicBonesToVRMSpringBonesConverter.ParametersConverter),
                                target: this.callbackFunction,
                                method: "Converter",
                                ignoreCase: false,
                                throwOnBindFailure: false
                            );
                        if (this.dynamicBoneToVRMSpringBoneToParametersConverter == null)
                        {
                            EditorGUILayout.HelpBox(
                                _("There is no “Convert” static method with matching parameters."),
                                MessageType.Error
                            );
                            this.isValid = false;
                        }
                        break;
                    case Direction.VRMSpringBonesToDynamicBones:
                        this.vrmSpringBoneToDynamicBoneParametersConverter
                            = (VRMSpringBonesToDynamicBonesConverter.ParametersConverter)Delegate.CreateDelegate(
                                type: typeof(VRMSpringBonesToDynamicBonesConverter.ParametersConverter),
                                target: this.callbackFunction,
                                method: "Converter",
                                ignoreCase: false,
                                throwOnBindFailure: false
                            );
                        if (this.vrmSpringBoneToDynamicBoneParametersConverter == null)
                        {
                            EditorGUILayout.HelpBox(
                                _("There is no “Convert” method with matching parameters."),
                                MessageType.Error
                            );
                            this.isValid = false;
                        }
                        break;
                }
            }
            var code = "";
            switch (this.direction)
            {
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

            foreach (var (label, animator) in new Dictionary<string, Animator> {
                { _("Source"), this.source },
                { _("Destination"), this.destination },
            })
            {
                if (animator == null)
                {
                    this.isValid = false;
                    continue;
                }

                var transform = animator.transform;
                if (transform != transform.root)
                {
                    EditorGUILayout.HelpBox(string.Format(_("“{0}” is not root object."), label), MessageType.Error);
                    this.isValid = false;
                    continue;
                }
            }

            if (this.destination != null
                && GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(this.destination.gameObject) > 0)
            {
                EditorGUILayout.HelpBox(_("The destination contains missing scripts."), MessageType.Error);
                this.isValid = false;
            }

            return true;
        }

        private void OnWizardCreate()
        {
            switch (this.direction)
            {
                case Direction.DynamicBonesToVRMSpringBones:
                    DynamicBonesToVRMSpringBonesConverter.Convert(
                        this.source,
                        this.destination,
                        this.overwriteMode,
                        this.ignoreColliders,
                        this.callbackFunction != null ? this.dynamicBoneToVRMSpringBoneToParametersConverter : null
                    );
                    break;
                case Direction.VRMSpringBonesToDynamicBones:
                    VRMSpringBonesToDynamicBonesConverter.Convert(
                        this.source,
                        this.destination,
                        this.overwriteMode,
                        this.ignoreColliders,
                        this.callbackFunction != null ? this.vrmSpringBoneToDynamicBoneParametersConverter : null
                    );
                    break;
            }

            EditorUtility.DisplayDialog(
                MenuItems.Name + "-" + MenuItems.Version,
                _("The conversion is completed."),
                _("OK")
            );
        }
    }
}
