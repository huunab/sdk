// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Razor.Tasks
{
    public class ReadStaticWebAssetsFromManifestFile : Task
    {
        [Required]
        public string ManifestPath { get; set; }

        [Output]
        public ITaskItem[] Assets { get; set; }


        public override bool Execute()
        {
            if (!File.Exists(ManifestPath))
            {
                Log.LogError($"Manifest file at '{ManifestPath}' not found.");
            }

            try
            {
                var manifest = StaticWebAssetsManifest.FromJsonBytes(File.ReadAllBytes(ManifestPath));
                Assets = manifest.Assets.Select(a => a.ToTaskItem()).ToArray();
            }
            catch (Exception ex)
            {
                Log.LogError(ex.Message);
            }

            return !Log.HasLoggedErrors;
        }
    }
}