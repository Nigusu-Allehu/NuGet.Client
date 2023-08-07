// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Shared;

namespace NuGet.Packaging
{
    /// <summary>
    /// A group of items/files from a nupkg with the same target framework.
    /// </summary>
    public class FrameworkSpecificGroup : IEquatable<FrameworkSpecificGroup>, IFrameworkSpecific
    {
        private readonly NuGetFramework _targetFramework;
        private readonly string[] _items;
        private readonly List<List<string>> _toknizedItems;

        /// <summary>
        /// Framework specific group
        /// </summary>
        /// <param name="targetFramework">group target framework</param>
        /// <param name="items">group items</param>
        public FrameworkSpecificGroup(NuGetFramework targetFramework, IEnumerable<string> items)
        {
            if (targetFramework == null)
            {
                throw new ArgumentNullException(nameof(targetFramework));
            }

            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            _targetFramework = targetFramework;

            HasEmptyFolder = items.Any(item => item.EndsWith(PackagingCoreConstants.ForwardSlashEmptyFolder,
                StringComparison.Ordinal));

            // Remove empty folder markers here
            _items = items.Where(item => !item.EndsWith(PackagingCoreConstants.ForwardSlashEmptyFolder,
                StringComparison.Ordinal))
                    .ToArray();
        }

#pragma warning disable RS0016 // Add public types and members to the declared API
        public FrameworkSpecificGroup(NuGetFramework targetFramework, IEnumerable<List<string>> items)
#pragma warning restore RS0016 // Add public types and members to the declared API
        {
            if (targetFramework == null)
            {
                throw new ArgumentNullException(nameof(targetFramework));
            }

            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            _targetFramework = targetFramework;

            HasEmptyFolder = items.Any(item => IsEmptyFolder(item));

            // Remove empty folder markers here
            _toknizedItems = _toknizedItems.Where(item => !IsEmptyFolder(item)).ToList();
        }

        private bool IsEmptyFolder(List<string> pathTokens)
        {
            // Check if the last token is the "empty folder" marker. Adjust this as needed.
            return pathTokens.Count > 0 && pathTokens[pathTokens.Count - 1] == PackagingCoreConstants.ForwardSlashEmptyFolder;
        }


        /// <summary>
        /// Group target framework
        /// </summary>
        public NuGetFramework TargetFramework
        {
            get { return _targetFramework; }
        }

        /// <summary>
        /// Item relative paths
        /// </summary>
        public IEnumerable<string> Items
        {
            get { return _items; }
        }

        public bool HasEmptyFolder { get; }

        public bool Equals(FrameworkSpecificGroup other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (ReferenceEquals(other, null))
            {
                return false;
            }

            return GetHashCode() == other.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var other = obj as FrameworkSpecificGroup;

            if (other != null)
            {
                return Equals(other);
            }

            return false;
        }

        public override int GetHashCode()
        {
            if (ReferenceEquals(this, null))
            {
                return 0;
            }

            var combiner = new HashCodeCombiner();

            combiner.AddObject(TargetFramework);

            if (Items != null)
            {
#if NETFRAMEWORK || NETSTANDARD

                foreach (var hash in Items.Select(e => e.GetHashCode()).OrderBy(e => e))
#else
                foreach (var hash in Items.Select(e => e.GetHashCode(StringComparison.Ordinal)).OrderBy(e => e))
#endif
                {
                    combiner.AddObject(hash);
                }
            }

            return combiner.CombinedHash;
        }
    }
}
