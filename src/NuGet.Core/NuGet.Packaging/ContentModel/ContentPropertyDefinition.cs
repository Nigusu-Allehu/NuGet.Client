// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Frameworks;
using static NuGet.Client.ManagedCodeConventions;

namespace NuGet.ContentModel
{
    /// <summary>
    /// Defines a property that can be used in Content Model query patterns
    /// <seealso cref="PatternSet" />
    /// </summary>
    public class ContentPropertyDefinition
    {
        private static readonly Func<object, object, bool> EqualsTest = (left, right) => Equals(left, right);

        public ContentPropertyDefinition(string name)
            : this(name, null, null, null, null, false)
        {
        }

        public ContentPropertyDefinition(
            string name,
            Func<string, PatternTable, object> parser)
            : this(name, parser, null, null, null, false)
        {
        }

#pragma warning disable RS0016 // Add public types and members to the declared API
        public ContentPropertyDefinition(
#pragma warning restore RS0016 // Add public types and members to the declared API
            string name,
            ParserType parserType,
            Dictionary<string, NuGetFramework> framework)
            : this(name, parserType, null, null, null, false, framework)
        {
        }

        public ContentPropertyDefinition(
            string name,
            Func<object, object, bool> compatibilityTest)
            : this(name, null, compatibilityTest, null, null, false)
        {
        }

        public ContentPropertyDefinition(
            string name,
            Func<string, PatternTable, object> parser,
            Func<object, object, bool> compatibilityTest)
            : this(name, parser, compatibilityTest, null, null, false)
        {
        }

#pragma warning disable RS0016 // Add public types and members to the declared API
        public ContentPropertyDefinition(
#pragma warning restore RS0016 // Add public types and members to the declared API
            string name,
            ParserType parserType,
            Func<object, object, bool> compatibilityTest,
            Dictionary<string, NuGetFramework> framework)
            : this(name, parserType, compatibilityTest, null, null, false, framework)
        {
        }

        public ContentPropertyDefinition(string name,
            Func<string, PatternTable, object> parser,
            Func<object, object, bool> compatibilityTest,
            Func<object, object, object, int> compareTest)
            : this(name, parser, compatibilityTest, compareTest, null, false)
        {
        }

#pragma warning disable RS0016 // Add public types and members to the declared API
        public ContentPropertyDefinition(string name,
#pragma warning restore RS0016 // Add public types and members to the declared API
           ParserType parserType,
           Func<object, object, bool> compatibilityTest,
           Func<object, object, object, int> compareTest,
           Dictionary<string, NuGetFramework> framework)
           : this(name, parserType, compatibilityTest, compareTest, null, false, framework)
        {
        }

        public ContentPropertyDefinition(
            string name,
            IEnumerable<string> fileExtensions)
            : this(name, null, null, null, fileExtensions, false)
        {
        }

        public ContentPropertyDefinition(
            string name,
            Func<string, PatternTable, object> parser,
            IEnumerable<string> fileExtensions)
            : this(name, parser, null, null, fileExtensions, false)
        {
        }

#pragma warning disable RS0016 // Add public types and members to the declared API
        public ContentPropertyDefinition(
#pragma warning restore RS0016 // Add public types and members to the declared API
            string name,
            ParserType parserType,
            IEnumerable<string> fileExtensions,
            Dictionary<string, NuGetFramework> framework)
            : this(name, parserType, null, null, fileExtensions, false, framework)
        {
        }

        public ContentPropertyDefinition(
            string name,
            IEnumerable<string> fileExtensions,
            bool allowSubfolders)
            : this(name, null, null, null, fileExtensions, allowSubfolders)
        {
        }

        public ContentPropertyDefinition(
            string name,
            Func<string, PatternTable, object> parser,
            IEnumerable<string> fileExtensions,
            bool allowSubfolders)
            : this(name, parser, null, null, fileExtensions, allowSubfolders)
        {
        }

#pragma warning disable RS0016 // Add public types and members to the declared API
        public ContentPropertyDefinition(
#pragma warning restore RS0016 // Add public types and members to the declared API
            string name,
            ParserType parserType,
            IEnumerable<string> fileExtensions,
            bool allowSubfolders,
            Dictionary<string, NuGetFramework> framework)
            : this(name, parserType, null, null, fileExtensions, allowSubfolders, framework)
        {
        }

        public ContentPropertyDefinition(
            string name,
            Func<string, PatternTable, object> parser,
            Func<object, object, bool> compatibilityTest,
            Func<object, object, object, int> compareTest,
            IEnumerable<string> fileExtensions,
            bool allowSubfolders)
        {
            Name = name;
            Parser = parser;
            CompatibilityTest = compatibilityTest ?? EqualsTest;
            CompareTest = compareTest;
            FileExtensions = fileExtensions?.ToList();
            FileExtensionAllowSubFolders = allowSubfolders;
        }

#pragma warning disable RS0016 // Add public types and members to the declared API
        public ContentPropertyDefinition(
#pragma warning restore RS0016 // Add public types and members to the declared API
            string name,
            ParserType parserType,
            Func<object, object, bool> compatibilityTest,
            Func<object, object, object, int> compareTest,
            IEnumerable<string> fileExtensions,
            bool allowSubfolders,
            Dictionary<string, NuGetFramework> framework)
        {
            Name = name;
            _parserType = parserType;
            CompatibilityTest = compatibilityTest ?? EqualsTest;
            CompareTest = compareTest;
            FileExtensions = fileExtensions?.ToList();
            FileExtensionAllowSubFolders = allowSubfolders;
            _framework = framework;
        }

        public string Name { get; }

        public List<string> FileExtensions { get; }

        public bool FileExtensionAllowSubFolders { get; }

        public Func<string, PatternTable, object> Parser { get; }

        private Dictionary<string, NuGetFramework> _framework;
        private ParserType _parserType;

        public virtual bool TryLookup(string name, PatternTable table, out object value)
        {
            if (name == null)
            {
                value = null;
                return false;
            }

            if (FileExtensions?.Count > 0)
            {
                if (FileExtensionAllowSubFolders || !ContainsSlash(name))
                {
                    foreach (var fileExtension in FileExtensions)
                    {
                        if (name.EndsWith(fileExtension, StringComparison.OrdinalIgnoreCase))
                        {
                            value = name;
                            return true;
                        }
                    }
                }
            }

            if (Parser != null)
            {
                value = Parser.Invoke(name, table);
                if (value != null)
                {
                    return true;
                }
            }

            value = null;
            return false;
        }

#pragma warning disable RS0016 // Add public types and members to the declared API
        public virtual bool TryLookup2(string name, PatternTable table, out object value)
#pragma warning restore RS0016 // Add public types and members to the declared API
        {
            //string name = path.Substring(startIndex, length);
            if (name == null)
            {
                value = null;
                return false;
            }

            if (FileExtensions?.Count > 0)
            {
                if (FileExtensionAllowSubFolders || !ContainsSlash(name))
                {
                    foreach (var fileExtension in FileExtensions)
                    {
                        if (name.EndsWith(fileExtension, StringComparison.OrdinalIgnoreCase))
                        {
                            value = name;
                            return true;
                        }
                    }
                }
            }

            if (_parserType != 0)
            {
                value = Client.ManagedCodeConventions.parseAll(_parserType, _framework, name, table);
                if (value != null)
                {
                    return true;
                }
            }

            value = null;
            return false;
        }

        private static bool ContainsSlash(string name)
        {
            var containsSlash = false;
            foreach (var ch in name)
            {
                if (ch == '/' || ch == '\\')
                {
                    containsSlash = true;
                    break;
                }
            }

            return containsSlash;
        }

        public Func<object, object, bool> CompatibilityTest { get; }

        /// <summary>
        /// Find the nearest compatible candidate.
        /// </summary>
        public Func<object, object, object, int> CompareTest { get; }

        public virtual bool IsCriteriaSatisfied(object critieriaValue, object candidateValue)
        {
            return CompatibilityTest.Invoke(critieriaValue, candidateValue);
        }

        public virtual int Compare(object criteriaValue, object candidateValue1, object candidateValue2)
        {
            var betterCoverageFromValue1 = IsCriteriaSatisfied(candidateValue1, candidateValue2);
            var betterCoverageFromValue2 = IsCriteriaSatisfied(candidateValue2, candidateValue1);
            if (betterCoverageFromValue1 && !betterCoverageFromValue2)
            {
                return -1;
            }
            if (betterCoverageFromValue2 && !betterCoverageFromValue1)
            {
                return 1;
            }

            if (CompareTest != null)
            {
                // In the case of a tie call the external compare test to determine the nearest candidate.
                return CompareTest.Invoke(criteriaValue, candidateValue1, candidateValue2);
            }

            // No tie breaker was provided.
            return 0;
        }
    }
}
