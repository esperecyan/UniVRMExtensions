using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Esperecyan.UniVRMExtensions.Utilities;

namespace Esperecyan.UniVRMExtensions
{
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

                    { "The Dynamic Bone asset has not been imported.", "Dynamic Boneアセットがインポートされていません。" },
                    { "Conversion source", "変換元" },
                    { "VRChat SDK3-Avatars has not been imported.", "VRChat SDK3-Avatars がインポートされていません。" },
                    { "{0} does not exist in Conversion source.", "{0} は変換元に存在しません。" },
                    { "{0} already exists in Conversion destination. This components will be removed.",
                        "{0} はすでに変換先に存在します。同コンポーネントは削除されます。" },
                    { "Conversion destination", "変換先" },
                    { "The same object can be specified as the source and destination.",
                        "変換元と変換先には同じオブジェクトを指定することができます。" },
                    { "Remove swaying objects from Conversion source", "変換元の揺れ物を削除" },
                    { "Overwrite mode", "上書きモード" },
                    { "Remove all swaying objects from the destination before conversion", "変換前に、変換先の揺れ物をすべて削除" },
                    { "Append", "追加" },
                    { "Ignore colliders", "コライダーを変換しない" },
                    { "Parameters conversion algorithm", "パラメータ変換アルゴリズム" },
                    { "There is no “Convert” method with matching parameters.", "引数が合致する「Convert」メソッドが存在しません。" },
                    { "Specify a C# script file that contains a static method named “Converter” as shown below.",
                        "以下のような「Converter」という名前のstaticメソッドを含むC#スクリプトファイルを指定します。" },
                    { "The destination contains missing scripts.", "変換先にmissing scriptが含まれています。" },
                    { "Convert", "変換" },
                    { "No corresponding bone found in the conversion destination", "変換先に対応するボーンが見つかりません" },
                    { "Inside colliders cannot be converted", "インサイドコライダーは変換できません" },
                    { "The conversion is completed.", "変換が完了しました。" },
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
}
