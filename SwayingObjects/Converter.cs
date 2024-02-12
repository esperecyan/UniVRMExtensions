#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UniGLTF;
using Esperecyan.UniVRMExtensions.Utilities;
using static Esperecyan.UniVRMExtensions.Utilities.Gettext;

namespace Esperecyan.UniVRMExtensions.SwayingObjects
{
    /// <summary>
    /// 共通処理。
    /// </summary>
    internal class Converter : IDisposable
    {
        internal readonly GameObject Source;
        internal readonly GameObject Destination;
        internal readonly bool DestinationIsAsset;
        internal readonly GameObject Secondary;

        private readonly Dictionary<HumanBodyBones, Transform> sourceSkeletonBones;
        private readonly string? destinationAssetPath;
        private readonly int undoGroupIndex;

        private bool disposed = false;

        internal Converter(Animator source, Animator destination)
        {
            this.Source = source.gameObject;

            // コピー元のHumanoidボーンを取得
            this.sourceSkeletonBones = BoneMapper.GetAllSkeletonBones(this.Source);

            this.Destination = destination.gameObject;
            this.DestinationIsAsset = PrefabUtility.IsPartOfPrefabAsset(this.Destination);
            if (this.DestinationIsAsset)
            {
                this.Destination = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(this.Destination));
                this.destinationAssetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(destination);
            }
            else
            {
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName($"Convert Swaying objects from “{source.name}” to “{destination.name}”");
            }
            this.undoGroupIndex = Undo.GetCurrentGroup();

            var secondary = this.Destination.transform.Find("secondary");
            if (secondary == null)
            {
                // secondaryがなければ追加
                this.Secondary = new GameObject("secondary");
                this.Secondary.transform.SetParent(this.Destination.transform);
                this.Secondary.transform.localPosition = Vector3.zero;
                if (!this.DestinationIsAsset)
                {
                    Undo.RegisterCreatedObjectUndo(this.Secondary, "");
                }
            }
            else
            {
                this.Secondary = secondary.gameObject;
            }
        }

        /// <summary>
        /// 指定したコピー元のボーンがLeftHand、もしくはRightHandなら、<c>true</c> を返します。
        /// </summary>
        /// <param name="sourceBone"></param>
        /// <returns></returns>
        internal bool IsHandBone(Transform sourceBone)
        {
            foreach (var humanoidBodyBone in new[] { HumanBodyBones.LeftHand, HumanBodyBones.RightHand })
            {
                if (this.sourceSkeletonBones.ContainsKey(humanoidBodyBone)
                    && this.sourceSkeletonBones[humanoidBodyBone] == sourceBone)
                {
                    return true;
                }
            }
            return false;
        }

        internal Transform? FindCorrespondingBone(Transform sourceBone, string? target)
        {
            var destinationBone = BoneMapper.FindCorrespondingBone(
                sourceBone,
                this.Source,
                this.Destination,
                this.sourceSkeletonBones
            );

            if (destinationBone == null && target != null)
            {
                Debug.LogWarning(
                    _("No corresponding bone found in the conversion destination")
                        + ": " + target
                        + ": " + sourceBone.RelativePathFrom(this.Source.transform),
                    sourceBone
                );
            }

            return destinationBone;
        }

        internal void SaveAsset()
        {
            if (!this.DestinationIsAsset)
            {
                return;
            }

            PrefabUtility.SaveAsPrefabAsset(this.Destination, this.destinationAssetPath);
        }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }
            this.disposed = true;

            if (this.DestinationIsAsset)
            {
                PrefabUtility.UnloadPrefabContents(this.Destination);
            }
            else
            {
                Undo.CollapseUndoOperations(this.undoGroupIndex);
            }
        }
    }
}
