﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
#if NET472_OR_GREATER
using System.Collections.Generic;
#endif
using System.Diagnostics;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Razor.Tasks
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class StaticWebAsset
    {
        public string Identity { get; set; }

        public string SourceType { get; set; }

        public string SourceId { get; set; }

        public string ContentRoot { get; set; }

        public string BasePath { get; set; }

        public string RelativePath { get; set; }

        public string AssetKind { get; set; }

        public string AssetMode { get; set; }

        public string CopyToOutputDirectory { get; set; }

        public string CopyToPublishDirectory { get; set; }

        public static StaticWebAsset FromTaskItem(ITaskItem item) => FromTaskItemCore(item, 2);

        public static StaticWebAsset FromV1TaskItem(ITaskItem item) => FromTaskItemCore(item, 1);

        private static StaticWebAsset FromTaskItemCore(ITaskItem item, int version)
        {
            var result = new StaticWebAsset
            {
                // Register the identity as the full path since assets might have come
                // from packages and other sources and the identity (which is typically
                // just the relative path from the project) is not enough to locate them.
                Identity = item.GetMetadata("FullPath"),
                SourceType = item.GetMetadata(nameof(SourceType)),
                SourceId = item.GetMetadata(nameof(SourceId)),
                ContentRoot = item.GetMetadata(nameof(ContentRoot)),
                BasePath = item.GetMetadata(nameof(BasePath)),
                RelativePath = item.GetMetadata(nameof(RelativePath)),
                AssetKind = item.GetMetadata(nameof(AssetKind)),
                AssetMode = item.GetMetadata(nameof(AssetMode)),
                CopyToOutputDirectory = item.GetMetadata(nameof(CopyToOutputDirectory)),
                CopyToPublishDirectory = item.GetMetadata(nameof(CopyToPublishDirectory)),
            };

            if (version == 1)
            {
                if (string.IsNullOrEmpty(result.AssetKind))
                {
                    result.AssetKind = AssetKinds.All;
                }
                if (string.IsNullOrEmpty(result.AssetMode))
                {
                    result.AssetMode = AssetModes.All;
                }
            }

            result.Validate();
            result.Normalize();

            return result;
        }

        internal string ComputeTargetPath(string pathPrefix)
        {
            var prefix = pathPrefix != null ? Normalize(pathPrefix) : "";
            // These have been normalized already, so only contain forward slashes
            return Path.Combine(prefix, SourceType == SourceTypes.Discovered || SourceType == SourceTypes.Computed ? "" : BasePath, RelativePath)
                .Replace('/', Path.DirectorySeparatorChar)
                .TrimStart(Path.DirectorySeparatorChar);
        }

        public ITaskItem ToTaskItem()
        {
            var result = new TaskItem(Identity);
            result.SetMetadata(nameof(SourceType), SourceType);
            result.SetMetadata(nameof(SourceId), SourceId);
            result.SetMetadata(nameof(ContentRoot), ContentRoot);
            result.SetMetadata(nameof(BasePath), BasePath);
            result.SetMetadata(nameof(RelativePath), RelativePath);
            result.SetMetadata(nameof(AssetKind), AssetKind);
            result.SetMetadata(nameof(AssetMode), AssetMode);
            result.SetMetadata(nameof(CopyToOutputDirectory), CopyToOutputDirectory);
            result.SetMetadata(nameof(CopyToPublishDirectory), CopyToPublishDirectory);

            return result;
        }

        public void Validate()
        {
            switch (SourceType)
            {
                case SourceTypes.Discovered:
                case SourceTypes.Computed:
                case SourceTypes.Project:
                case SourceTypes.Package:
                    break;
                default:
                    throw new InvalidOperationException($"Unknown source type '{SourceType}' for '{Identity}'.");
            };

            if (SourceId == null)
            {
                throw new InvalidOperationException($"The '{nameof(SourceId)}' for the asset must be defined for '{Identity}'.");
            }

            if (ContentRoot == null)
            {
                throw new InvalidOperationException($"The '{nameof(ContentRoot)}' for the asset must be defined for '{Identity}'.");
            }

            if (BasePath == null)
            {
                throw new InvalidOperationException($"The '{nameof(BasePath)}' for the asset must be defined for '{Identity}'.");
            }

            if (RelativePath == null)
            {
                throw new InvalidOperationException($"The '{nameof(RelativePath)}' for the asset must be defined for '{Identity}'.");
            }

            switch (AssetKind)
            {
                case AssetKinds.All:
                case AssetKinds.Build:
                case AssetKinds.Publish:
                    break;
                default:
                    throw new InvalidOperationException($"Unknown Asset kind '{AssetKind}' for '{Identity}'.");
            };

            switch (AssetMode)
            {
                case AssetModes.All:
                case AssetModes.CurrentProject:
                case AssetModes.Reference:
                    break;
                default:
                    throw new InvalidOperationException($"Unknown Asset mode '{AssetMode}' for '{Identity}'.");
            };
        }

        public void Normalize()
        {
            BasePath = Normalize(BasePath);
            RelativePath = Normalize(RelativePath);
        }

        public static string Normalize(string path)
        {
            return path.Replace('\\', '/').Trim('/');
        }

        public override bool Equals(object obj)
        {
            return obj is StaticWebAsset asset &&
                   Identity == asset.Identity &&
                   SourceType == asset.SourceType &&
                   SourceId == asset.SourceId &&
                   ContentRoot == asset.ContentRoot &&
                   BasePath == asset.BasePath &&
                   RelativePath == asset.RelativePath &&
                   AssetKind == asset.AssetKind &&
                   AssetMode == asset.AssetMode &&
                   CopyToOutputDirectory == asset.CopyToOutputDirectory &&
                   CopyToPublishDirectory == asset.CopyToPublishDirectory;
        }

        public static class AssetModes
        {
            public const string CurrentProject = nameof(CurrentProject);
            public const string Reference = nameof(Reference);
            public const string All = nameof(All);
        }

        public static class AssetKinds
        {
            public const string Build = nameof(Build);
            public const string Publish = nameof(Publish);
            public const string All = nameof(All);
        }

        public static class SourceTypes
        {
            public const string Discovered = nameof(Discovered);
            public const string Computed = nameof(Computed);
            public const string Project = nameof(Project);
            public const string Package = nameof(Package);
        }

        private string GetDebuggerDisplay()
        {
            return ToString();
        }

        public override string ToString() =>
            $"Identity: {Identity}, " +
            $"SourceType: {SourceType}, " +
            $"SourceId: {SourceId}, " +
            $"ContentRoot: {ContentRoot}, " +
            $"BasePath: {BasePath}, " +
            $"RelativePath: {RelativePath}, " +
            $"AssetKind: {AssetKind}, " +
            $"AssetMode: {AssetMode}, " +
            $"AssetKind: {CopyToOutputDirectory}, " +
            $"AssetKind: {CopyToPublishDirectory}";

        public override int GetHashCode()
        {
#if NET6_0_OR_GREATER
            HashCode hash = new HashCode();
            hash.Add(Identity);
            hash.Add(SourceType);
            hash.Add(SourceId);
            hash.Add(ContentRoot);
            hash.Add(BasePath);
            hash.Add(RelativePath);
            hash.Add(AssetKind);
            hash.Add(AssetMode);
            hash.Add(CopyToOutputDirectory);
            hash.Add(CopyToPublishDirectory);
            return hash.ToHashCode();
#else
            int hashCode = 1447485498;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Identity);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SourceType);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SourceId);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ContentRoot);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(BasePath);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(RelativePath);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(AssetKind);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(AssetMode);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(CopyToOutputDirectory);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(CopyToPublishDirectory);
            return hashCode;
#endif
        }
    }
}