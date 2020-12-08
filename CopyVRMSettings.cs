using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditorInternal;
using UniGLTF;
using VRM;

namespace Esperecyan.UniVRMExtensions
{
    /// <summary>
    /// セットアップ済みのVRMプレハブから、正規化直後のVRMプレハブへ、UniVRMのコンポーネントの設定をコピーします。
    /// </summary>
    /// <remarks>
    /// • モデルのメタ情報
    /// • VRMBlendShape
    /// • 一人称視点
    /// • 視線制御
    /// • VRMSpringBoneとVRMSpringBoneColliderGroup
    /// 
    /// 動作確認バージョン: UniVRM 0.55.0, Unity 2018.4.20f1, Unity 2019.3.12f1
    /// ライセンス: MIT License (MIT) <https://spdx.org/licenses/MIT.html>
    /// 配布元: <https://gist.github.com/esperecyan/8a6d19738d4828f6df92a53138b7e315>
    /// </remarks>
    public class CopyVRMSettings
    {
        /// <summary>
        /// 変換元のアバターのルートに設定されている必要があるコンポーネントと、そのフィールド名。
        /// </summary>
        public static readonly IDictionary<Type, string> RequiredComponentsAndFields = new Dictionary<Type, string> {
            { typeof(Animator), "" },
            { typeof(VRMMeta), "Meta" },
            { typeof(VRMHumanoidDescription), "Description" },
            { typeof(VRMBlendShapeProxy), "BlendShapeAvatar" },
        };

        /// <summary>
        /// <see cref="CopyVRMSettings.Copy"/> の第3引数 <c>components</c> に指定可能な値。
        /// </summary>
        public static readonly IEnumerable<Type> SupportedComponents = new[] {
            typeof(VRMMeta),
            typeof(VRMBlendShapeProxy),
            typeof(VRMFirstPerson),
            typeof(VRMLookAtHead),
            typeof(VRMSpringBone),
        };

        /// <summary>
        /// 当エディタ拡張のバージョン。
        /// </summary>
        /// <remarks>
        /// 
        /// 2.0.0 (2020-05-03)
        ///     VRMSpringBoneのCenterをコピーできていなかった不具合を修正
        ///         ※バーチャルキャストではCenterを設定するとVRMSpringBoneが正常に動作しなくなるという情報があるため、メジャーバージョンを変更
        ///     その他微修正
        /// 1.3.0 (2020-01-19)
        ///     UniVRM-0.54.0の仕様変更により、コピー先のVRMBlendShapeが壊れるようになっていた問題に対処
        ///     <https://github.com/vrm-c/UniVRM/pull/330>
        ///     モデルのメタ情報、VRMBlendShape、一人称視点、視線制御、VRMSpringBoneを個別にコピーできるようにした
        /// 1.2.0 (2019-08-23)
        ///     1.1.1の更新で、1.1.0の前者の更新を差し戻してしまっていたバグを修正
        ///     Unity 2018.1、2018.2 で動作するようにした
        /// 1.1.1 (2019-08-02)
        ///     VRMSpringBoneとVRMSpringBoneColliderGroupのコピーで、相対パスによる検索が行われずにエラーが発生していたバグを修正
        /// 1.1.0 (2019-07-17)
        ///     オブジェクト名を参照していたために、BlendShapeが正常にコピーできていなかったバグを修正
        ///     Unity 2017.4 に対応
        /// 1.0.2 (2019-06-17)
        ///     VRMSpringBoneとVRMSpringBoneColliderGroupのコピーで、オブジェクト名のみによる検索が行われていなかったのを修正
        ///     その他微修正
        /// 1.0.1 (2019-06-15)
        ///     コピーしたBlendShapeがUnity終了時に失われる問題を修正
        ///     プリセット以外のBlendShapeが追加されない問題に対処
        /// 1.0.0 (2019-06-13)
        ///     公開
        /// </remarks>
        public const string Version = "2.0.0";

        /// <summary>
        /// VRMの設定をコピーします。
        /// </summary>
        /// <param name="source">ヒエラルキーのルート、もしくはプレハブのルートであるコピー元のアバター。</param>
        /// <param name="destination">ヒエラルキーのルート、もしくはプレハブのルートであるコピー先のアバター。</param>
        /// <param name="">コピーするコンポーネント。既定値は <see cref="CopyVRMSettings.SupportedComponents">。</param>
        public static void Copy(GameObject source, GameObject destination, IEnumerable<Type> components = null)
        {
            if (components == null)
            {
                components = CopyVRMSettings.SupportedComponents;
            }

            GameObject destinationPrefab = null;
            if (!SceneManager.GetActiveScene().GetRootGameObjects().Contains(destination.gameObject))
            {
                destinationPrefab = destination;
                destination = PrefabUtility.InstantiatePrefab(destination) as GameObject;
            }

            if (components.Contains(typeof(VRMMeta)))
            {
                CopyMeta.Copy(source: source, destination: destination);
            }
            if (components.Contains(typeof(VRMBlendShapeProxy)))
            {
                CopyVRMBlendShapes.Copy(source: source, destination: destination);
            }
            var sourceSkeletonBones = BoneMapper.GetAllSkeletonBones(avatar: source);
            if (components.Contains(typeof(VRMFirstPerson)))
            {
                CopyFirstPerson.Copy(source: source, destination: destination, sourceSkeletonBones: sourceSkeletonBones);
            }
            if (components.Contains(typeof(VRMLookAtHead)))
            {
                CopyEyeControl.Copy(source: source, destination: destination, sourceSkeletonBones: sourceSkeletonBones);
            }
            if (components.Contains(typeof(VRMSpringBone)))
            {
                CopyVRMSpringBones.Copy(source: source, destination: destination, sourceSkeletonBones: sourceSkeletonBones);
            }

            if (destinationPrefab)
            {
#if UNITY_2018_3_OR_NEWER
                PrefabUtility.ApplyPrefabInstance(destination, InteractionMode.AutomatedAction);
#else
                PrefabUtility.ReplacePrefab(destination, destinationPrefab, ReplacePrefabOptions.ConnectToPrefab);
#endif
                UnityEngine.Object.DestroyImmediate(destination);
            }
        }

        /// <summary>
        /// 当エディタ拡張の名称。
        /// </summary>
        internal const string Name = "CopyVRMSettings.cs";

        /// <summary>
        /// プレハブのパスを返します。
        /// </summary>
        /// <param name="prefab">プレハブ、またはプレハブのインスタンス。</param>
        /// <returns>「Assets/」から始まるパス。プレハブのインスタンスでなかった場合は空文字列。</returns>
        internal static string GetPrefabAssetPath(GameObject prefab)
        {
#if UNITY_2018_3_OR_NEWER
            return PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefab);
#else
            GameObject root = PrefabUtility.FindPrefabRoot(prefab);
            if (!root)
            {
                return "";
            }
            return AssetDatabase.GetAssetPath(root);
#endif
        }
    }

    /// <summary>
    /// L10N。
    /// </summary>
    internal class Locales
    {
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            Gettext.SetLocalizedTexts(localizedTexts: new Dictionary<string, IDictionary<string, string>> {
                { "ja", new Dictionary<string, string> {
                    { "“{0}” is not root object.", "「{0}」はルートオブジェクトではありません。"},
                    { "“{0}”and its VRMBlendShapes will be overwritten.", "「{0}」、およびそのVRMBlendShapeは上書きされます。" },
                    { "“{0}”will be overwritten.", "「{0}」は上書きされます。" },
                    { "“{0}” is not set “{1}” component.", "「{0}」に「{1}」コンポーネントが設定されていません。" },
                    { "“{0}”’s “{1}” is null.", "「{0}」の「{1}」が null です。"},
                    { "“{0}” and “{1}” are instances of same prefab.", "「{0」と「{1}」は同一のプレハブのインスタンスです。" },
                    { "Copy and Paste", "コピー&ペースト" },
                    { "Settings copying and pasting is completed.", "設定のコピー&ペーストが完了しました。" },
                    { "OK", "OK" },
                }}
            });

            Gettext.SetLocale(clientLang: Locales.ConvertToLangtagFromSystemLanguage(systemLanguage: Application.systemLanguage));
        }

        /// <summary>
        /// <see cref="SystemLanguage"/>に対応するIETF言語タグを返します。
        /// </summary>
        /// <param name="systemLanguage"></param>
        /// <returns><see cref="SystemLanguage.Unknown"/>の場合は「und」、未知の<see cref="SystemLanguage"/>の場合は空文字列を返します。</returns>
        private static string ConvertToLangtagFromSystemLanguage(SystemLanguage systemLanguage)
        {
            switch (systemLanguage)
            {
                case SystemLanguage.Afrikaans:
                    return "af";
                case SystemLanguage.Arabic:
                    return "ar";
                case SystemLanguage.Basque:
                    return "eu";
                case SystemLanguage.Belarusian:
                    return "be";
                case SystemLanguage.Bulgarian:
                    return "bg";
                case SystemLanguage.Catalan:
                    return "ca";
                case SystemLanguage.Chinese:
                    return "zh";
                case SystemLanguage.Czech:
                    return "cs";
                case SystemLanguage.Danish:
                    return "da";
                case SystemLanguage.Dutch:
                    return "nl";
                case SystemLanguage.English:
                    return "en";
                case SystemLanguage.Estonian:
                    return "et";
                case SystemLanguage.Faroese:
                    return "fo";
                case SystemLanguage.Finnish:
                    return "fi";
                case SystemLanguage.French:
                    return "fr";
                case SystemLanguage.German:
                    return "de";
                case SystemLanguage.Greek:
                    return "el";
                case SystemLanguage.Hebrew:
                    return "he";
                case SystemLanguage.Hungarian:
                    return "hu";
                case SystemLanguage.Icelandic:
                    return "is";
                case SystemLanguage.Indonesian:
                    return "in";
                case SystemLanguage.Italian:
                    return "it";
                case SystemLanguage.Japanese:
                    return "ja";
                case SystemLanguage.Korean:
                    return "ko";
                case SystemLanguage.Latvian:
                    return "lv";
                case SystemLanguage.Lithuanian:
                    return "lt";
                case SystemLanguage.Norwegian:
                    return "no";
                case SystemLanguage.Polish:
                    return "pl";
                case SystemLanguage.Portuguese:
                    return "pt";
                case SystemLanguage.Romanian:
                    return "ro";
                case SystemLanguage.Russian:
                    return "ru";
                case SystemLanguage.SerboCroatian:
                    return "sh";
                case SystemLanguage.Slovak:
                    return "sk";
                case SystemLanguage.Slovenian:
                    return "sl";
                case SystemLanguage.Spanish:
                    return "es";
                case SystemLanguage.Swedish:
                    return "sv";
                case SystemLanguage.Thai:
                    return "th";
                case SystemLanguage.Turkish:
                    return "tr";
                case SystemLanguage.Ukrainian:
                    return "uk";
                case SystemLanguage.Vietnamese:
                    return "vi";
                case SystemLanguage.ChineseSimplified:
                    return "zh-Hans";
                case SystemLanguage.ChineseTraditional:
                    return "zh-Hant";
                case SystemLanguage.Unknown:
                    return "und";
            }

            return "";
        }
    }

    /// <summary>
    /// i18n。
    /// </summary>
    internal class Gettext
    {
        /// <summary>
        /// 翻訳対象文字列 (msgid) の言語。IETF言語タグの「language」サブタグ。
        /// </summary>
        private static readonly string OriginalLocale = "en";

        /// <summary>
        /// クライアントの言語の翻訳リソースが存在しないとき、どの言語に翻訳するか。IETF言語タグの「language」サブタグ。
        /// </summary>
        private static readonly string DefaultLocale = "en";

        /// <summary>
        /// クライアントの言語。<see cref="Gettext.SetLocale"/>から変更されます。
        /// </summary>
        private static string langtag = "en";

        /// <summary>
        /// クライアントの言語のlanguage部分。<see cref="Gettext.SetLocale"/>から変更されます。
        /// </summary>
        private static string language = "en";

        /// <summary>
        /// 翻訳リソース。<see cref="Gettext.SetLocalizedTexts"/>から変更されます。
        /// </summary>
        private static IDictionary<string, IDictionary<string, string>> multilingualLocalizedTexts = new Dictionary<string, IDictionary<string, string>> { };

        /// <summary>
        /// 翻訳リソースを追加します。
        /// </summary>
        /// <param name="localizedTexts"></param>
        internal static void SetLocalizedTexts(IDictionary<string, IDictionary<string, string>> localizedTexts)
        {
            Gettext.multilingualLocalizedTexts = localizedTexts;
        }

        /// <summary>
        /// クライアントの言語を設定します。
        /// </summary>
        /// <param name="clientLang">IETF言語タグ (「language」と「language-REGION」にのみ対応)。</param>
        internal static void SetLocale(string clientLang)
        {
            string[] splitedClientLang = clientLang.Split(separator: '-');
            Gettext.language = splitedClientLang[0].ToLower();
            Gettext.langtag = string.Join(
                separator: "-",
                value: splitedClientLang,
                startIndex: 0,
                count: Math.Min(2, splitedClientLang.Length)
            );
            if (Gettext.language == "ja")
            {
                // ja-JPをjaと同一視
                Gettext.langtag = Gettext.language;
            }
        }

        /// <summary>
        /// テキストをクライアントの言語に変換します。
        /// </summary>
        /// <param name="message">翻訳前。</param>
        /// <returns>翻訳語。</returns>
        internal static string _(string message)
        {
            if (Gettext.langtag == Gettext.OriginalLocale)
            {
                // クライアントの言語が翻訳元の言語なら、そのまま返す
                return message;
            }

            foreach (string langtag in new[] {
                // クライアントの言語の翻訳リソースが存在すれば、それを返す
                Gettext.langtag,
                // 地域下位タグを取り除いた言語タグの翻訳リソースが存在すれば、それを返す
                Gettext.language,
                // 既定言語の翻訳リソースが存在すれば、それを返す
                Gettext.DefaultLocale,
            })
            {
                if (Gettext.multilingualLocalizedTexts.ContainsKey(langtag)
                    && Gettext.multilingualLocalizedTexts[Gettext.langtag].ContainsKey(message)
                    && Gettext.multilingualLocalizedTexts[Gettext.langtag][message] != "")
                {
                    return Gettext.multilingualLocalizedTexts[Gettext.langtag][message];
                }
            }

            return message;
        }
    }

    /// <summary>
    /// ダイアログ。
    /// </summary>
    public class Wizard : ScriptableWizard
    {
        /// <summary>
        /// 追加するメニューアイテムの、「VRM」メニュー内の位置。
        /// </summary>
        public const int Priority = 1101;

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

        /// <summary>
        /// 選択されているアバターの変換ダイアログを開きます。
        /// </summary>
        [MenuItem("VRM/" + CopyVRMSettings.Name + "-" + CopyVRMSettings.Version, false, Wizard.Priority)]
        private static void OpenWizard()
        {
            Wizard.Open();
        }

        /// <summary>
        /// ダイアログを開きます。
        /// </summary>
        internal static void Open()
        {
            var wizard = DisplayWizard<Wizard>(
                CopyVRMSettings.Name + " " + CopyVRMSettings.Version,
                Gettext._("Copy and Paste")
            );
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
                components: components
            );

            EditorUtility.DisplayDialog(
                CopyVRMSettings.Name + "-" + CopyVRMSettings.Version,
                Gettext._("Settings copying and pasting is completed."),
                Gettext._("OK")
            );
        }
    }

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
            VRMMetaObject sourceMeta = source.GetComponent<VRMMeta>().Meta;
            VRMMetaObject destinationMeta = destination.GetComponent<VRMMeta>().Meta;
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
            VRMMetaObject sourceMeta = source.GetComponent<VRMMeta>().Meta;
            VRMMetaObject destinationMeta = destination.GetComponent<VRMMeta>().Meta;

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

            string sourceThumbnailPath = AssetDatabase.GetAssetPath(destinationMeta.Thumbnail);
            if (UnityPath.FromUnityPath(sourceThumbnailPath).Parent.Value
                != UnityPath.FromAsset(source).GetAssetFolder(suffix: ".Textures").Value)
            {
                return;
            }

            string destinationPrefabPath = CopyVRMSettings.GetPrefabAssetPath(destination);
            if (string.IsNullOrEmpty(destinationPrefabPath))
            {
                return;
            }

            string destinationThumbnailPath = UnityPath.FromUnityPath(destinationPrefabPath)
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

    internal class CopyFirstPerson
    {
        /// <summary>
        /// 一人称表示の設定をコピーします。
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="sourceSkeletonBones"></param>
        internal static void Copy(
            GameObject source,
            GameObject destination,
            Dictionary<HumanBodyBones, Transform> sourceSkeletonBones
        ) {
            var sourceFirstPerson = source.GetComponent<VRMFirstPerson>();
            var destinationFirstPerson = destination.GetComponent<VRMFirstPerson>();
            if (!sourceFirstPerson)
            {
                if (destinationFirstPerson)
                {
                    UnityEngine.Object.DestroyImmediate(destinationFirstPerson);
                }
                return;
            }

            if (sourceFirstPerson.FirstPersonBone)
            {
                destinationFirstPerson.FirstPersonBone = BoneMapper.FindCorrespondingBone(
                    sourceBone: sourceFirstPerson.FirstPersonBone,
                    source: source,
                    destination: destination,
                    sourceSkeletonBones: sourceSkeletonBones
                );
            }

            destinationFirstPerson.FirstPersonOffset = sourceFirstPerson.FirstPersonOffset;

            foreach (VRMFirstPerson.RendererFirstPersonFlags sourceFlags in sourceFirstPerson.Renderers)
            {
                if (sourceFlags.FirstPersonFlag == FirstPersonFlag.Auto)
                {
                    continue;
                }

                Mesh sourceMesh = sourceFlags.SharedMesh;
                if (!sourceMesh)
                {
                    continue;
                }

                string sourceMeshName = sourceMesh.name;

                int index = destinationFirstPerson.Renderers.FindIndex(match: flags => {
                    Mesh destinationMesh = flags.SharedMesh;
                    return destinationMesh && destinationMesh.name == sourceMeshName;
                });
                if (index == -1)
                {
                    continue;
                }

                VRMFirstPerson.RendererFirstPersonFlags destinationFlags = destinationFirstPerson.Renderers[index];
                destinationFlags.FirstPersonFlag = sourceFlags.FirstPersonFlag;
                destinationFirstPerson.Renderers[index] = destinationFlags;
            }
        }
    }
    internal class CopyEyeControl
    {
        /// <summary>
        /// 視線制御の設定をコピーします。
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="sourceSkeletonBones"></param>
        internal static void Copy(
            GameObject source,
            GameObject destination,
            Dictionary<HumanBodyBones, Transform> sourceSkeletonBones
        ) {
            if (!source.GetComponent<VRMFirstPerson>())
            {
                return;
            }

            var sourceLookAtHead = source.GetComponent<VRMLookAtHead>();
            if (!sourceLookAtHead)
            {
                var destinationLookAtHead = destination.GetComponent<VRMLookAtHead>();
                if (destinationLookAtHead)
                {
                    UnityEngine.Object.DestroyImmediate(destinationLookAtHead);
                }
                return;
            }

            var sourceBoneApplyer = source.GetComponent<VRMLookAtBoneApplyer>();
            if (sourceBoneApplyer)
            {
                ComponentUtility.CopyComponent(sourceBoneApplyer);
                var destinationBoneApplyer = destination.GetOrAddComponent<VRMLookAtBoneApplyer>();
                ComponentUtility.PasteComponentValues(destinationBoneApplyer);

                if (destinationBoneApplyer.LeftEye.Transform)
                {
                    destinationBoneApplyer.LeftEye.Transform = BoneMapper.FindCorrespondingBone(
                        sourceBone: destinationBoneApplyer.LeftEye.Transform,
                        source: source,
                        destination: destination,
                        sourceSkeletonBones: sourceSkeletonBones
                    );
                }
                if (destinationBoneApplyer.RightEye.Transform)
                {
                    destinationBoneApplyer.RightEye.Transform = BoneMapper.FindCorrespondingBone(
                        sourceBone: destinationBoneApplyer.RightEye.Transform,
                        source: source,
                        destination: destination,
                        sourceSkeletonBones: sourceSkeletonBones
                    );
                }

                var blendShapeApplyer = destination.GetComponent<VRMLookAtBlendShapeApplyer>();
                if (blendShapeApplyer)
                {
                    UnityEngine.Object.DestroyImmediate(blendShapeApplyer);
                }
                return;
            }

            var sourceBlendShapeApplyer = source.GetComponent<VRMLookAtBlendShapeApplyer>();
            if (sourceBlendShapeApplyer)
            {
                ComponentUtility.CopyComponent(sourceBlendShapeApplyer);
                var destinationBlendShapeApplyer = destination.GetOrAddComponent<VRMLookAtBlendShapeApplyer>();
                ComponentUtility.PasteComponentValues(destinationBlendShapeApplyer);
            }
        }
    }

    internal class CopyVRMSpringBones
    {
        /// <summary>
        /// VRMSpringBone、およびVRMSpringBoneColliderGroupをコピーします。
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="sourceSkeletonBones"></param>
        internal static void Copy(
            GameObject source,
            GameObject destination,
            Dictionary<HumanBodyBones, Transform> sourceSkeletonBones
        )
        {
            foreach (Component component in new[] { typeof(VRMSpringBone), typeof(VRMSpringBoneColliderGroup) }
                .SelectMany(type => destination.GetComponentsInChildren(type)))
            {
                UnityEngine.Object.DestroyImmediate(component);
            }

            IDictionary<Transform, Transform> transformMapping = new Dictionary<Transform, Transform>();

            foreach (var sourceSpringBone in source.GetComponentsInChildren<VRMSpringBone>())
            {
                if (sourceSpringBone.RootBones.Count == 0)
                {
                    continue;
                }

                transformMapping = CopyVRMSpringBones.CopySpringBone(
                    sourceSpringBone: sourceSpringBone,
                    destination: destination,
                    sourceSkeletonBones: sourceSkeletonBones,
                    transformMapping: transformMapping
                );
            }

            CopyVRMSpringBones.CopySpringBoneColliderGroupForVirtualCast(source: source, destination: destination);
        }

        /// <summary>
        /// VRMSpringBone、およびVRMSpringBoneColliderGroupをコピーします。
        /// </summary>
        /// <param name="sourceSpringBone"></param>
        /// <param name="destination"></param>
        /// <param name="sourceSkeletonBones"></param>
        /// <param name="transformMapping"></param>
        /// <returns>更新された <c>transformMapping</c> を返します。</returns>
        private static IDictionary<Transform, Transform> CopySpringBone(
            VRMSpringBone sourceSpringBone,
            GameObject destination,
            Dictionary<HumanBodyBones, Transform> sourceSkeletonBones,
            IDictionary<Transform, Transform> transformMapping
        ) {
            GameObject destinationSecondary = destination.transform.Find("secondary").gameObject;

            ComponentUtility.CopyComponent(sourceSpringBone);
            ComponentUtility.PasteComponentAsNew(destinationSecondary);
            VRMSpringBone destinationSpringBone = destinationSecondary.GetComponents<VRMSpringBone>().Last();

            if (destinationSpringBone.m_center)
            {
                destinationSpringBone.m_center = transformMapping.ContainsKey(destinationSpringBone.m_center)
                    ? transformMapping[destinationSpringBone.m_center]
                    : (transformMapping[destinationSpringBone.m_center] = BoneMapper.FindCorrespondingBone(
                        sourceBone: destinationSpringBone.m_center,
                        source: sourceSpringBone.transform.root.gameObject,
                        destination: destination,
                        sourceSkeletonBones: sourceSkeletonBones
                    ));
            }

            for (var i = 0; i < destinationSpringBone.RootBones.Count; i++)
            {
                Transform sourceSpringBoneRoot = destinationSpringBone.RootBones[i];

                destinationSpringBone.RootBones[i] = sourceSpringBoneRoot
                    ? (transformMapping.ContainsKey(sourceSpringBoneRoot)
                        ? transformMapping[sourceSpringBoneRoot]
                        : (transformMapping[sourceSpringBoneRoot] = BoneMapper.FindCorrespondingBone(
                            sourceBone: sourceSpringBoneRoot,
                            source: sourceSpringBone.transform.root.gameObject,
                            destination: destination,
                            sourceSkeletonBones: sourceSkeletonBones
                        )))
                    : null;
            }

            for (var i = 0; i < destinationSpringBone.ColliderGroups.Length; i++)
            {
                VRMSpringBoneColliderGroup sourceColliderGroup = destinationSpringBone.ColliderGroups[i];

                Transform destinationColliderGroupTransform = sourceColliderGroup
                    ? (transformMapping.ContainsKey(sourceColliderGroup.transform)
                        ? transformMapping[sourceColliderGroup.transform]
                        : (transformMapping[sourceColliderGroup.transform] = BoneMapper.FindCorrespondingBone(
                            sourceBone: sourceColliderGroup.transform,
                            source: sourceSpringBone.transform.root.gameObject,
                            destination: destination,
                            sourceSkeletonBones: sourceSkeletonBones
                        )))
                    : null;

                VRMSpringBoneColliderGroup destinationColliderGroup = null;
                if (destinationColliderGroupTransform)
                {
                    CopyVRMSpringBones.CopySpringBoneColliderGroups(
                        sourceBone: sourceColliderGroup.transform,
                        destinationBone: destinationColliderGroupTransform
                    );
                    destinationColliderGroup
                        = destinationColliderGroupTransform.GetComponent<VRMSpringBoneColliderGroup>();
                }
                destinationSpringBone.ColliderGroups[i] = destinationColliderGroup;
            }

            return transformMapping;
        }

        /// <summary>
        /// コピー先にVRMSpringBoneColliderGroupが存在しなければ、コピー元のVRMSpringBoneColliderGroupをすべてコピーします。
        /// </summary>
        /// <param name="sourceBone"></param>
        /// <param name="destinationBone"></param>
        private static void CopySpringBoneColliderGroups(Transform sourceBone, Transform destinationBone)
        {
            if (destinationBone.GetComponent<VRMSpringBoneColliderGroup>())
            {
                return;
            }

            foreach (var colliderGroup in sourceBone.GetComponents<VRMSpringBoneColliderGroup>())
            {
                ComponentUtility.CopyComponent(colliderGroup);
                ComponentUtility.PasteComponentAsNew(destinationBone.gameObject);
            }
        }

        /// <summary>
        /// バーチャルキャスト向けに、どのVRMSpringBoneにも関連付けられていないVRMSpringBoneColliderGroupをコピーします。
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        private static void CopySpringBoneColliderGroupForVirtualCast(GameObject source, GameObject destination)
        {
            var sourceAnimator = source.GetComponent<Animator>();
            var destinationAnimator = destination.GetComponent<Animator>();
            foreach (var humanoidBone in new[] { HumanBodyBones.LeftHand, HumanBodyBones.RightHand })
            {
                CopyVRMSpringBones.CopySpringBoneColliderGroups(
                    sourceBone: sourceAnimator.GetBoneTransform(humanoidBone),
                    destinationBone: destinationAnimator.GetBoneTransform(humanoidBone)
                );
            }
        }
    }

    internal class BoneMapper
    {

        /// <summary>
        /// すべてのスケルトンボーンを取得します。
        /// </summary>
        /// <param name="avatar"></param>
        /// <returns></returns>
        internal static Dictionary<HumanBodyBones, Transform> GetAllSkeletonBones(GameObject avatar)
        {
            var animator = avatar.GetComponent<Animator>();
            return avatar.GetComponent<VRMHumanoidDescription>().Description.human
                .Select(boneLimit => boneLimit.humanBone)
                .ToDictionary(
                    keySelector: humanoidBone => humanoidBone,
                    elementSelector: humanoidBone => animator.GetBoneTransform(humanoidBone)
                );
        }

        /// <summary>
        /// コピー元のアバターの指定ボーンと対応する、コピー先のアバターのボーンを返します。
        /// </summary>
        /// <param name="sourceBoneRelativePath"></param>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="sourceSkeletonBones"></param>
        /// <returns>見つからなかった場合は <c>null</c> を返します。</returns>
        internal static Transform FindCorrespondingBone(
            Transform sourceBone,
            GameObject source,
            GameObject destination,
            Dictionary<HumanBodyBones, Transform> sourceSkeletonBones
        )
        {
            if (!sourceBone.IsChildOf(source.transform))
            {
                return null;
            }

            string sourceBoneRelativePath = sourceBone.RelativePathFrom(root: source.transform);
            Transform destinationBone = destination.transform.Find(sourceBoneRelativePath);
            if (destinationBone)
            {
                return destinationBone;
            }

            if (!sourceBone.IsChildOf(source.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Hips)))
            {
                return null;
            }

            var humanoidAndSkeletonBone
                = BoneMapper.ClosestSkeletonBone(bone: sourceBone, skeletonBones: sourceSkeletonBones);
            Animator destinationAniamtor = destination.GetComponent<Animator>();
            Transform destinationSkeletonBone = destinationAniamtor.GetBoneTransform(humanoidAndSkeletonBone.Key);
            if (!destinationSkeletonBone)
            {
                return null;
            }

            destinationBone
                = destinationSkeletonBone.Find(sourceBone.RelativePathFrom(root: humanoidAndSkeletonBone.Value));
            if (destinationBone)
            {
                return destinationBone;
            }

            return destinationSkeletonBone.GetComponentsInChildren<Transform>()
                .FirstOrDefault(bone => bone.name == sourceBone.name);
        }

        /// <summary>
        /// 祖先方向へたどり、指定されたボーンを含む直近のスケルトンボーンを取得します。
        /// </summary>
        /// <param name="bone"></param>
        /// <param name="avatar"></param>
        /// <param name="skeletonBones"></param>
        /// <returns></returns>
        private static KeyValuePair<HumanBodyBones, Transform> ClosestSkeletonBone(
            Transform bone,
            Dictionary<HumanBodyBones, Transform> skeletonBones
        )
        {
            foreach (Transform parent in bone.Ancestors())
            {
                if (!skeletonBones.ContainsValue(parent))
                {
                    continue;
                }

                return skeletonBones
                    .FirstOrDefault(predicate: humanoidAndSkeletonBone => humanoidAndSkeletonBone.Value == parent);
            }

            throw new ArgumentException();
        }
    }
}
