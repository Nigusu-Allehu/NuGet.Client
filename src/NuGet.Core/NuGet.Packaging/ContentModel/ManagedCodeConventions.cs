// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NuGet.ContentModel;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.RuntimeModel;

namespace NuGet.Client
{
    /// <summary>
    /// Defines all the package conventions used by Managed Code packages
    /// </summary>
    public class ManagedCodeConventions
    {
#pragma warning disable RS0016 // Add public types and members to the declared API
        public enum ParserType

        {
            Local_parser = 1,
            AllowEmptyFolderParser = 2,
            CodeLanguage_Parser = 3,
            All_strings = 4,
            TargetFrameworkName_Parser = 5

        }

#pragma warning disable IDE1006 // Naming Styles
        public static object parseAll(ParserType parserType,
#pragma warning restore IDE1006 // Naming Styles
            Dictionary<string, NuGetFramework> framework,
            string path,
            int startIndex,
            int length,
            PatternTable table)
        {
            switch (parserType)
            {
                case ParserType.Local_parser:
                    NuGetFileLogger.DefaultInstance.Write("Local_parser");
                    return Locale_Parser(path.AsSpan().Slice(startIndex, length).ToString(), table);
                case ParserType.AllowEmptyFolderParser:
                    NuGetFileLogger.DefaultInstance.Write("AllowEmptyFolderParser");
                    return AllowEmptyFolderParser(path.AsSpan().Slice(startIndex, length).ToString(), table);
                case ParserType.CodeLanguage_Parser:
                    NuGetFileLogger.DefaultInstance.Write("CodeLanguage_Parser");
                    return CodeLanguage_Parser(path.AsSpan().Slice(startIndex, length).ToString(), table);
                case ParserType.All_strings:
                    NuGetFileLogger.DefaultInstance.Write("All_strings");
                    return path.AsSpan().Slice(startIndex, length).ToString();
                case ParserType.TargetFrameworkName_Parser:
                    NuGetFileLogger.DefaultInstance.Write("TargetFrameworkName_Parser");
                    return TargetFrameworkName_Parser(path.AsSpan().Slice(startIndex, length), table, framework);
                default: return null;
            }
        }

#pragma warning restore RS0016 // Add public types and members to the declared API
        private static readonly ContentPropertyDefinition LocaleProperty = new ContentPropertyDefinition(PropertyNames.Locale,
            parserType: ParserType.Local_parser, framework: null);

        private static readonly ContentPropertyDefinition AnyProperty = new ContentPropertyDefinition(
            PropertyNames.AnyValue,
            parserType: ParserType.All_strings, null); // Identity parser, all strings are valid for any
        private static readonly ContentPropertyDefinition AssemblyProperty = new ContentPropertyDefinition(PropertyNames.ManagedAssembly,
            parserType: ParserType.AllowEmptyFolderParser,
            fileExtensions: new[] { ".dll", ".winmd", ".exe" }, null);
        private static readonly ContentPropertyDefinition MSBuildProperty = new ContentPropertyDefinition(PropertyNames.MSBuild,
            parserType: ParserType.AllowEmptyFolderParser,
            fileExtensions: new[] { ".targets", ".props" }, null);
        private static readonly ContentPropertyDefinition SatelliteAssemblyProperty = new ContentPropertyDefinition(PropertyNames.SatelliteAssembly,
            parserType: ParserType.AllowEmptyFolderParser,
            fileExtensions: new[] { ".resources.dll" }, null);

        private static readonly ContentPropertyDefinition CodeLanguageProperty = new ContentPropertyDefinition(
            PropertyNames.CodeLanguage,
            parserType: ParserType.CodeLanguage_Parser, null);

        private static readonly Dictionary<string, object> NetTFMTable = new Dictionary<string, object>
        {
            { "tfm", new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.Net, FrameworkConstants.EmptyVersion) },
            { "tfm_raw", "net0" }
        };

        private static readonly Dictionary<string, object> DefaultTfmAny = new Dictionary<string, object>
        {
            { PropertyNames.TargetFrameworkMoniker, AnyFramework.Instance },
            { PropertyNames.TargetFrameworkMoniker + "_raw", "any" }
        };

        private static readonly PatternTable DotnetAnyTable = new PatternTable(new[]
        {
            new PatternTableEntry(
                PropertyNames.TargetFrameworkMoniker,
                "any",
                FrameworkConstants.CommonFrameworks.DotNet)
        });

        private static readonly PatternTable AnyTable = new PatternTable(new[]
        {
            new PatternTableEntry(
                PropertyNames.TargetFrameworkMoniker,
                "any",
                AnyFramework.Instance )
        });

        private static readonly FrameworkReducer FrameworkReducer = new();

        private RuntimeGraph _runtimeGraph;

        private Dictionary<string, NuGetFramework> _frameworkCache
            = new Dictionary<string, NuGetFramework>(StringComparer.Ordinal);

        public ManagedCodeCriteria Criteria { get; }
        public IReadOnlyDictionary<string, ContentPropertyDefinition> Properties { get; }
        public ManagedCodePatterns Patterns { get; }

        public ManagedCodeConventions(RuntimeGraph runtimeGraph)
        {
            _runtimeGraph = runtimeGraph;

            var props = new Dictionary<string, ContentPropertyDefinition>();
            props[AnyProperty.Name] = AnyProperty;
            props[AssemblyProperty.Name] = AssemblyProperty;
            props[LocaleProperty.Name] = LocaleProperty;
            props[MSBuildProperty.Name] = MSBuildProperty;
            props[SatelliteAssemblyProperty.Name] = SatelliteAssemblyProperty;
            props[CodeLanguageProperty.Name] = CodeLanguageProperty;

            props[PropertyNames.RuntimeIdentifier] = new ContentPropertyDefinition(
                PropertyNames.RuntimeIdentifier,
                parserType: ParserType.All_strings, // Identity parser, all strings are valid runtime ids :)
                compatibilityTest: RuntimeIdentifier_CompatibilityTest, _frameworkCache);

            props[PropertyNames.TargetFrameworkMoniker] = new ContentPropertyDefinition(
                PropertyNames.TargetFrameworkMoniker,
                parserType: ParserType.TargetFrameworkName_Parser,
                compatibilityTest: TargetFrameworkName_CompatibilityTest,
                compareTest: TargetFrameworkName_NearestCompareTest, _frameworkCache);

            Properties = new ReadOnlyDictionary<string, ContentPropertyDefinition>(props);

            Criteria = new ManagedCodeCriteria(this);
            Patterns = new ManagedCodePatterns(this);
        }

        private bool RuntimeIdentifier_CompatibilityTest(object criteria, object available)
        {
            if (_runtimeGraph == null)
            {
                return Equals(criteria, available);
            }
            else
            {
                var criteriaRid = criteria as string;
                var availableRid = available as string;

                if (criteriaRid != null
                    && availableRid != null)
                {
                    return _runtimeGraph.AreCompatible(criteriaRid, availableRid);
                }
                return false;
            }
        }

        private static object CodeLanguage_Parser(string name, PatternTable table)
        {
            if (table != null)
            {
                object val;
                if (table.TryLookup(PropertyNames.CodeLanguage, name, out val))
                {
                    return val;
                }
            }

            // Code language values must be alpha numeric.
            return name.All(c => char.IsLetterOrDigit(c)) ? name : null;
        }

        private static object Locale_Parser(string name, PatternTable table)
        {
            if (table != null)
            {
                object val;
                if (table.TryLookup(PropertyNames.Locale, name, out val))
                {
                    return val;
                }
            }

            if (name.Length == 2)
            {
                return name;
            }
            else if (name.Length >= 4 && name[2] == '-')
            {
                return name;
            }

            return null;
        }

        private static object TargetFrameworkName_Parser(
            ReadOnlySpan<char> name,
            PatternTable table,
            Dictionary<string, NuGetFramework> frameworkCache)
        {
            NuGetFileLogger.DefaultInstance.Write($"Parsing TFM: {name.ToString()}");
            object obj = null;

            // Check for replacements
            if (table != null)
            {
                if (table.TryLookup(PropertyNames.TargetFrameworkMoniker, name.ToString(), out obj))
                {
                    return obj;
                }
            }

            // Check the cache for an exact match
            if (!(name == null || name.IsEmpty))
            {
                NuGetFramework cachedResult;
                if (!frameworkCache.TryGetValue(name.ToString(), out cachedResult))
                {
                    // Parse and add the framework to the cache
                    cachedResult = TargetFrameworkName_ParserCore(name.ToString());
                    frameworkCache.Add(name.ToString(), cachedResult);
                }

                return cachedResult;
            }

            // Let the framework parser handle null/empty and create the error message.
            return TargetFrameworkName_ParserCore(name.ToString());
        }

        private static NuGetFramework TargetFrameworkName_ParserCore(string name)
        {
            var result = NuGetFramework.ParseFolder(name);

            if (!result.IsUnsupported)
            {
                return result;
            }

            // Everything should be in the folder format, but fallback to
            // full parsing for legacy support.
            result = NuGetFramework.ParseFrameworkName(name, DefaultFrameworkNameProvider.Instance);

            if (!result.IsUnsupported)
            {
                return result;
            }

            // For unknown frameworks return the name as is.
            return new NuGetFramework(name, FrameworkConstants.EmptyVersion);
        }

        private static object AllowEmptyFolderParser(string s, PatternTable table)
        {
            // Accept "_._" as a pseudo-assembly
            return PackagingCoreConstants.EmptyFolder.Equals(s, StringComparison.Ordinal) ? s : null;
        }

        private static bool TargetFrameworkName_CompatibilityTest(object criteria, object available)
        {
            var criteriaFrameworkName = criteria as NuGetFramework;
            var availableFrameworkName = available as NuGetFramework;

            if (criteriaFrameworkName != null
                && availableFrameworkName != null)
            {
                // We only consider 'any' matches when the criteria explicitly asks for them
                if (criteriaFrameworkName.IsAny
                    && availableFrameworkName.IsAny)
                {
                    return true;
                }
                else if (Object.Equals(AnyFramework.AnyFramework, availableFrameworkName))
                {
                    // If the convention does not contain a TxM it will use AnyFramework, this is
                    // always compatible with other frameworks.
                    return true;
                }
                else if (criteriaFrameworkName.IsAny
                         || availableFrameworkName.IsAny)
                {
                    // Otherwise, ignore 'any' framework values
                    return false;
                }

                return NuGetFrameworkUtility.IsCompatibleWithFallbackCheck(criteriaFrameworkName, availableFrameworkName);
            }

            return false;
        }

        private static int TargetFrameworkName_NearestCompareTest(object projectFramework, object criteria, object available)
        {
            var projectFrameworkName = projectFramework as NuGetFramework;
            var criteriaFrameworkName = criteria as NuGetFramework;
            var availableFrameworkName = available as NuGetFramework;

            if (criteriaFrameworkName != null
                && availableFrameworkName != null
                && projectFrameworkName != null)
            {
                // If the frameworks are the same this can be skipped
                if (!criteriaFrameworkName.Equals(availableFrameworkName))
                {
                    var frameworks = new NuGetFramework[] { criteriaFrameworkName, availableFrameworkName };

                    // Find the nearest compatible framework to the project framework.
                    var nearest = FrameworkReducer.GetNearest(projectFrameworkName, frameworks);

                    if (criteriaFrameworkName.Equals(nearest))
                    {
                        return -1;
                    }

                    if (availableFrameworkName.Equals(nearest))
                    {
                        return 1;
                    }
                }
            }

            return 0;
        }

        private static Version NormalizeVersion(Version version)
        {
            return new Version(version.Major,
                version.Minor,
                Math.Max(version.Build, 0),
                Math.Max(version.Revision, 0));
        }

        public class ManagedCodeCriteria
        {
            private ManagedCodeConventions _conventions;

            internal ManagedCodeCriteria(ManagedCodeConventions conventions)
            {
                _conventions = conventions;
            }

            public SelectionCriteria ForFrameworkAndRuntime(NuGetFramework framework, string runtimeIdentifier)
            {
                if (framework is FallbackFramework)
                {
                    // Fallback frameworks are not handled by content model
                    throw new NotSupportedException("FallbackFramework is not supported.");
                }

                // Both criteria must specify a RID

                var builder = new SelectionCriteriaBuilder(_conventions.Properties);
                if (!string.IsNullOrEmpty(runtimeIdentifier))
                {
                    builder = builder
                        // Take runtime-specific matches first!
                        .Add[PropertyNames.TargetFrameworkMoniker, framework][PropertyNames.RuntimeIdentifier, runtimeIdentifier];
                }

                // Then try runtime-agnostic
                builder = builder
                    .Add[PropertyNames.TargetFrameworkMoniker, framework][PropertyNames.RuntimeIdentifier, value: null];

                return builder.Criteria;
            }

            public SelectionCriteria ForFramework(NuGetFramework framework)
            {
                return ForFrameworkAndRuntime(framework, runtimeIdentifier: null);
            }

            public SelectionCriteria ForRuntime(string runtimeIdentifier)
            {
                var builder = new SelectionCriteriaBuilder(_conventions.Properties);
                builder = builder
                    .Add[PropertyNames.RuntimeIdentifier, runtimeIdentifier];
                return builder.Criteria;
            }
        }

        public class ManagedCodePatterns
        {
            /// <summary>
            /// Pattern used to locate all files targetted at a specific runtime and/or framework
            /// </summary>
            public PatternSet AnyTargettedFile { get; }

            /// <summary>
            /// Pattern used to locate all files designed for loading as managed code assemblies at run-time
            /// </summary>
            public PatternSet RuntimeAssemblies { get; }

            /// <summary>
            /// Pattern used to locate ref assemblies for compile.
            /// </summary>
            public PatternSet CompileRefAssemblies { get; }

            /// <summary>
            /// Pattern used to locate lib assemblies for compile.
            /// </summary>
            public PatternSet CompileLibAssemblies { get; }

            /// <summary>
            /// Pattern used to locate all files designed for loading as native code libraries at run-time
            /// </summary>
            public PatternSet NativeLibraries { get; }

            /// <summary>
            /// Pattern used to locate all files designed for loading as managed code resource assemblies at run-time
            /// </summary>
            public PatternSet ResourceAssemblies { get; }

            /// <summary>
            /// Pattern used to identify MSBuild targets and props files
            /// </summary>
            public PatternSet MSBuildFiles { get; }

            /// <summary>
            /// Pattern used to identify MSBuild global targets and props files
            /// </summary>
            public PatternSet MSBuildMultiTargetingFiles { get; }

            /// <summary>
            /// Pattern used to identify content files
            /// </summary>
            public PatternSet ContentFiles { get; }

            /// <summary>
            /// Pattern used to identify Tools assets for global tools
            /// </summary>
            public PatternSet ToolsAssemblies { get; }

            /// <summary>
            /// Pattern used to locate embed interop types assemblies
            /// </summary>
            public PatternSet EmbedAssemblies { get; }

            /// <summary>
            /// Pattern used to identify MSBuild transitive targets and props files
            /// </summary>
            public PatternSet MSBuildTransitiveFiles { get; }

            internal ManagedCodePatterns(ManagedCodeConventions conventions)
            {
                AnyTargettedFile = new PatternSet(
                    conventions.Properties,
                    groupPatterns: new PatternDefinition[]
                    {
                        new PatternDefinition("{any}/{tfm}/{any?}", table: DotnetAnyTable),
                        new PatternDefinition("runtimes/{rid}/{any}/{tfm}/{any?}", table: DotnetAnyTable),
                    },
                    pathPatterns: new PatternDefinition[]
                    {
                        new PatternDefinition("{any}/{tfm}/{any?}", table: DotnetAnyTable),
                        new PatternDefinition("runtimes/{rid}/{any}/{tfm}/{any?}", table: DotnetAnyTable),
                    });

                RuntimeAssemblies = new PatternSet(
                    conventions.Properties,
                    groupPatterns: new PatternDefinition[]
                    {
                        new PatternDefinition("runtimes/{rid}/lib/{tfm}/{any?}", table: DotnetAnyTable),
                        new PatternDefinition("lib/{tfm}/{any?}", table: DotnetAnyTable),
                        new PatternDefinition("lib/{assembly?}", table: DotnetAnyTable, defaults: NetTFMTable)
                    },
                    pathPatterns: new PatternDefinition[]
                    {
                        new PatternDefinition("runtimes/{rid}/lib/{tfm}/{assembly}", table: DotnetAnyTable),
                        new PatternDefinition("lib/{tfm}/{assembly}", table: DotnetAnyTable),
                        new PatternDefinition("lib/{assembly}", table: DotnetAnyTable, defaults: NetTFMTable)
                    });

                CompileRefAssemblies = new PatternSet(
                    conventions.Properties,
                    groupPatterns: new PatternDefinition[]
                        {
                            new PatternDefinition("ref/{tfm}/{any?}", table: DotnetAnyTable),
                        },
                    pathPatterns: new PatternDefinition[]
                        {
                            new PatternDefinition("ref/{tfm}/{assembly}", table: DotnetAnyTable),
                        });

                CompileLibAssemblies = new PatternSet(
                    conventions.Properties,
                    groupPatterns: new PatternDefinition[]
                        {
                            new PatternDefinition("lib/{tfm}/{any?}", table: DotnetAnyTable),
                            new PatternDefinition("lib/{assembly?}", table: DotnetAnyTable, defaults: NetTFMTable)
                        },
                    pathPatterns: new PatternDefinition[]
                        {
                            new PatternDefinition("lib/{tfm}/{assembly}", table: DotnetAnyTable),
                            new PatternDefinition("lib/{assembly}", table: DotnetAnyTable, defaults: NetTFMTable)
                        });

                NativeLibraries = new PatternSet(
                    conventions.Properties,
                    groupPatterns: new PatternDefinition[]
                        {
                            new PatternDefinition("runtimes/{rid}/nativeassets/{tfm}/{any?}", table: DotnetAnyTable),
                            new PatternDefinition("runtimes/{rid}/native/{any?}", table: null, defaults: DefaultTfmAny)
                        },
                    pathPatterns: new PatternDefinition[]
                    {
                        new PatternDefinition("runtimes/{rid}/nativeassets/{tfm}/{any}", table: DotnetAnyTable),
                        new PatternDefinition("runtimes/{rid}/native/{any}", table: null, defaults: DefaultTfmAny)
                    });

                ResourceAssemblies = new PatternSet(
                    conventions.Properties,
                    groupPatterns: new PatternDefinition[]
                    {
                        new PatternDefinition("runtimes/{rid}/lib/{tfm}/{locale?}/{any?}", table: DotnetAnyTable),
                        new PatternDefinition("lib/{tfm}/{locale?}/{any?}", table: DotnetAnyTable),
                    },
                    pathPatterns: new PatternDefinition[]
                    {
                        new PatternDefinition("runtimes/{rid}/lib/{tfm}/{locale}/{satelliteAssembly}", table: DotnetAnyTable),
                        new PatternDefinition("lib/{tfm}/{locale}/{satelliteAssembly}", table: DotnetAnyTable),
                    });

                MSBuildFiles = new PatternSet(
                    conventions.Properties,
                    groupPatterns: new PatternDefinition[]
                    {
                        new PatternDefinition("build/{tfm}/{msbuild?}", table: DotnetAnyTable),
                        new PatternDefinition("build/{msbuild?}", table: null, defaults: DefaultTfmAny)
                    },
                    pathPatterns: new PatternDefinition[]
                    {
                        new PatternDefinition("build/{tfm}/{msbuild}", table: DotnetAnyTable),
                        new PatternDefinition("build/{msbuild}", table: null, defaults: DefaultTfmAny)
                    });

                MSBuildMultiTargetingFiles = new PatternSet(
                    conventions.Properties,
                    groupPatterns: new PatternDefinition[]
                    {
                        new PatternDefinition("buildMultiTargeting/{msbuild?}", table: null, defaults: DefaultTfmAny),

                        // deprecated
                        new PatternDefinition("buildCrossTargeting/{msbuild?}", table: null, defaults: DefaultTfmAny)
                    },
                    pathPatterns: new PatternDefinition[]
                    {
                        new PatternDefinition("buildMultiTargeting/{msbuild}", table: null, defaults: DefaultTfmAny),

                        // deprecated
                        new PatternDefinition("buildCrossTargeting/{msbuild}", table: null, defaults: DefaultTfmAny)
                    });

                ContentFiles = new PatternSet(
                    conventions.Properties,
                    groupPatterns: new PatternDefinition[]
                    {
                        new PatternDefinition("contentFiles/{codeLanguage}/{tfm}/{any?}"),
                    },
                    pathPatterns: new PatternDefinition[]
                    {
                        new PatternDefinition("contentFiles/{codeLanguage}/{tfm}/{any?}"),
                    });

                ToolsAssemblies = new PatternSet(
                    conventions.Properties,
                    groupPatterns: new PatternDefinition[]
                        {
                            new PatternDefinition("tools/{tfm}/{rid}/{any?}", table: AnyTable),
                        },
                    pathPatterns: new PatternDefinition[]
                        {
                            new PatternDefinition("tools/{tfm}/{rid}/{any?}", table: AnyTable),
                    });

                EmbedAssemblies = new PatternSet(
                    conventions.Properties,
                    groupPatterns: new PatternDefinition[]
                        {
                            new PatternDefinition("embed/{tfm}/{any?}", table: DotnetAnyTable),
                        },
                    pathPatterns: new PatternDefinition[]
                        {
                            new PatternDefinition("embed/{tfm}/{assembly}", table: DotnetAnyTable),
                        });

                MSBuildTransitiveFiles = new PatternSet(
                    conventions.Properties,
                    groupPatterns: new PatternDefinition[]
                    {
                        new PatternDefinition("buildTransitive/{tfm}/{msbuild?}", table: DotnetAnyTable),
                        new PatternDefinition("buildTransitive/{msbuild?}", table: null, defaults: DefaultTfmAny)
                    },
                    pathPatterns: new PatternDefinition[]
                    {
                        new PatternDefinition("buildTransitive/{tfm}/{msbuild}", table: DotnetAnyTable),
                        new PatternDefinition("buildTransitive/{msbuild}", table: null, defaults: DefaultTfmAny)
                    });
            }
        }

        public static class PropertyNames
        {
            public static readonly string TargetFrameworkMoniker = "tfm";
            public static readonly string RuntimeIdentifier = "rid";
            public static readonly string AnyValue = "any";
            public static readonly string ManagedAssembly = "assembly";
            public static readonly string Locale = "locale";
            public static readonly string MSBuild = "msbuild";
            public static readonly string SatelliteAssembly = "satelliteAssembly";
            public static readonly string CodeLanguage = "codeLanguage";
        }
    }
}
