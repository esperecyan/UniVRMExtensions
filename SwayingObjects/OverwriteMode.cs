#nullable enable

namespace Esperecyan.UniVRMExtensions.SwayingObjects
{
    /// <summary>
    /// 変換先にすでに揺れ物が存在している場合の設定。
    /// </summary>
    public enum OverwriteMode
    {
        /// <summary>
        /// 変換前に、変換先の揺れ物をすべて削除。
        /// </summary>
        Replace,
        /// <summary>
        /// 追加。
        /// </summary>
        Append,
    }
}
