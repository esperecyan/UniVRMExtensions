using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VRM;
using Esperecyan.UniVRMExtensions.Utilities;

namespace Esperecyan.UniVRMExtensions.CopyVRMSettingsComponents
{

    /// <summary>
    /// ダイアログ。
    /// </summary>
    public class Wizard : ScriptableWizard
    {
        /// <summary>
        /// 設定のコピー元のアバター。
        /// </summary>
        [SerializeField]
        private Animator sourceAvatar = null;

        /// <summary>
        /// 設定のコピー先のアバター。
        /// </summary>
        [SerializeField]
        private Animator destinationAvatar = null;

        [SerializeField]
        private bool metaInformation = true;

        [SerializeField]
        private bool vrmBlendShape = true;

        [SerializeField]
        private bool firstPerson = true;

        [SerializeField]
        private bool lookAt = true;

        [SerializeField]
        private bool vrmSpringBone = true;

        private string version;

        /// <summary>
        /// ダイアログを開きます。
        /// </summary>
        internal static async void Open()
        {
            var version = await MenuItems.GetVersion();
            var wizard
                = ScriptableWizard.DisplayWizard<Wizard>(MenuItems.Name + "-" + version, Gettext._("Copy and Paste"));
            wizard.version = version;
        }

        protected override bool DrawWizardGUI()
        {
            base.DrawWizardGUI();
            this.isValid = true;

            if (!this.metaInformation && !this.vrmBlendShape && !this.firstPerson && !this.vrmSpringBone)
            {
                this.isValid = false;
                return true;
            }

            EditorGUILayout.HelpBox(string.Format(
                this.vrmBlendShape
                    ? Gettext._("“{0}”and its VRMBlendShapes will be overwritten.")
                    : Gettext._("“{0}”will be overwritten."),
                "Destination Avatar"
            ), MessageType.None);

            if (this.sourceAvatar && this.destinationAvatar
                && CopyVRMSettings.GetPrefabAssetPath(this.sourceAvatar.gameObject)
                    == CopyVRMSettings.GetPrefabAssetPath(this.destinationAvatar.gameObject))
            {
                EditorGUILayout.HelpBox(string.Format(
                    Gettext._("“{0}” and “{1}” are instances of same prefab."),
                    "Source Avatar",
                    "Destination Avatar"
                ), MessageType.Error);
                this.isValid = false;
            }

            foreach (var labelAndAnimator in new Dictionary<string, Animator> {
                { "Source Avatar", this.sourceAvatar },
                { "Destination Avatar", this.destinationAvatar },
            })
            {
                if (!labelAndAnimator.Value)
                {
                    this.isValid = false;
                    continue;
                }

                Transform transform = labelAndAnimator.Value.transform;
                if (transform != transform.root)
                {
                    EditorGUILayout.HelpBox(
                        string.Format(Gettext._("“{0}” is not root object."), labelAndAnimator.Key),
                        MessageType.Error
                    );
                    this.isValid = false;
                    continue;
                }

                foreach (var typeAndPropertyName in CopyVRMSettings.RequiredComponentsAndFields)
                {
                    var component = labelAndAnimator.Value.GetComponent(typeAndPropertyName.Key);
                    if (!labelAndAnimator.Value.GetComponent(typeAndPropertyName.Key))
                    {
                        EditorGUILayout.HelpBox(string.Format(
                            Gettext._("“{0}” is not set “{1}” component."),
                            labelAndAnimator.Key,
                            typeAndPropertyName.Key
                        ), MessageType.Error);
                        this.isValid = false;
                        continue;
                    }

                    if (string.IsNullOrEmpty(typeAndPropertyName.Value))
                    {
                        continue;
                    }

                    if (typeAndPropertyName.Key.GetField(
                        name: typeAndPropertyName.Value,
                        bindingAttr: BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public
                    ).GetValue(obj: component) == null)
                    {
                        EditorGUILayout.HelpBox(string.Format(
                            Gettext._("“{0}”’s “{1}” is null."),
                            labelAndAnimator.Key,
                            typeAndPropertyName.Key + "." + typeAndPropertyName.Value
                        ), MessageType.Error);
                        this.isValid = false;
                        continue;
                    }
                }
            }

            return true;
        }

        private void OnWizardCreate()
        {
            var components = new List<Type>();
            if (this.metaInformation)
            {
                components.Add(typeof(VRMMeta));
            }
            if (this.vrmBlendShape)
            {
                components.Add(typeof(VRMBlendShapeProxy));
            }
            if (this.firstPerson)
            {
                components.Add(typeof(VRMFirstPerson));
            }
            if (this.lookAt)
            {
                components.Add(typeof(VRMLookAtHead));
            }
            if (this.vrmSpringBone)
            {
                components.Add(typeof(VRMSpringBone));
            }

            CopyVRMSettings.Copy(
                source: this.sourceAvatar.gameObject,
                destination: this.destinationAvatar.gameObject,
                components
            );

            EditorUtility.DisplayDialog(
                MenuItems.Name + "-" + this.version,
                Gettext._("Settings copying and pasting is completed."),
                Gettext._("OK")
            );
        }
    }
}
