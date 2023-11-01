// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client;
using NuGet.ContentModel;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.RuntimeModel;

namespace Microsoft.DotNet.Cli.ToolPackage
{
    internal class ExtendedManagedCodeConventions: ManagedCodeConventions
    {
        public class ExtendedManagedCodeCriteria
        {
            private ExtendedManagedCodeConventions _conventions;

            internal ExtendedManagedCodeCriteria(ExtendedManagedCodeConventions conventions)
            {
                _conventions = conventions;
            }

            public SelectionCriteria ForFrameworkAndRuntime(NuGetFramework framework, string runtimeIdentifier)
            {
                if (framework is FallbackFramework)
                {
                    throw new NotSupportedException("FallbackFramework is not supported.");
                }

                SelectionCriteriaBuilder selectionCriteriaBuilder = new SelectionCriteriaBuilder(_conventions.Properties);
                if (!string.IsNullOrEmpty(runtimeIdentifier))
                {
                    selectionCriteriaBuilder = selectionCriteriaBuilder.Add[PropertyNames.TargetFrameworkMoniker, framework][PropertyNames.RuntimeIdentifier, runtimeIdentifier];
                }

                selectionCriteriaBuilder = selectionCriteriaBuilder.Add[PropertyNames.TargetFrameworkMoniker, framework][PropertyNames.RuntimeIdentifier, null];
                return selectionCriteriaBuilder.Criteria;
            }

            public SelectionCriteria ForFramework(NuGetFramework framework)
            {
                return ForFrameworkAndRuntime(framework, null);
            }

            public SelectionCriteria ForRuntime(string runtimeIdentifier)
            {
                return new SelectionCriteriaBuilder(_conventions.Properties).Add[PropertyNames.RuntimeIdentifier, runtimeIdentifier].Criteria;
            }
        }

        public class ExtendedManagedCodePatterns
        {
            public PatternSet AnyTargettedFile { get; }

            public PatternSet RuntimeAssemblies { get; }

            public PatternSet CompileRefAssemblies { get; }

            public PatternSet CompileLibAssemblies { get; }

            public PatternSet NativeLibraries { get; }

            public PatternSet ResourceAssemblies { get; }

            public PatternSet MSBuildFiles { get; }

            public PatternSet MSBuildMultiTargetingFiles { get; }

            public PatternSet ContentFiles { get; }

            public PatternSet ToolsAssemblies { get; }

            public PatternSet EmbedAssemblies { get; }

            public PatternSet MSBuildTransitiveFiles { get; }

            internal ExtendedManagedCodePatterns(ExtendedManagedCodeConventions conventions)
            {
                AnyTargettedFile = new PatternSet(conventions.Properties, new PatternDefinition[2]
                {
                new PatternDefinition("{any}/{tfm}/{any?}", DotnetAnyTable),
                new PatternDefinition("runtimes/{rid}/{any}/{tfm}/{any?}", DotnetAnyTable)
                }, new PatternDefinition[2]
                {
                new PatternDefinition("{any}/{tfm}/{any?}", DotnetAnyTable),
                new PatternDefinition("runtimes/{rid}/{any}/{tfm}/{any?}", DotnetAnyTable)
                });
                RuntimeAssemblies = new PatternSet(conventions.Properties, new PatternDefinition[3]
                {
                new PatternDefinition("runtimes/{rid}/lib/{tfm}/{any?}", DotnetAnyTable),
                new PatternDefinition("lib/{tfm}/{any?}", DotnetAnyTable),
                new PatternDefinition("lib/{assembly?}", DotnetAnyTable, NetTFMTable)
                }, new PatternDefinition[3]
                {
                new PatternDefinition("runtimes/{rid}/lib/{tfm}/{assembly}", DotnetAnyTable),
                new PatternDefinition("lib/{tfm}/{assembly}", DotnetAnyTable),
                new PatternDefinition("lib/{assembly}", DotnetAnyTable, NetTFMTable)
                });
                CompileRefAssemblies = new PatternSet(conventions.Properties, new PatternDefinition[1]
                {
                new PatternDefinition("ref/{tfm}/{any?}", DotnetAnyTable)
                }, new PatternDefinition[1]
                {
                new PatternDefinition("ref/{tfm}/{assembly}", DotnetAnyTable)
                });
                CompileLibAssemblies = new PatternSet(conventions.Properties, new PatternDefinition[2]
                {
                new PatternDefinition("lib/{tfm}/{any?}", DotnetAnyTable),
                new PatternDefinition("lib/{assembly?}", DotnetAnyTable, NetTFMTable)
                }, new PatternDefinition[2]
                {
                new PatternDefinition("lib/{tfm}/{assembly}", DotnetAnyTable),
                new PatternDefinition("lib/{assembly}", DotnetAnyTable, NetTFMTable)
                });
                NativeLibraries = new PatternSet(conventions.Properties, new PatternDefinition[2]
                {
                new PatternDefinition("runtimes/{rid}/nativeassets/{tfm}/{any?}", DotnetAnyTable),
                new PatternDefinition("runtimes/{rid}/native/{any?}", null, DefaultTfmAny)
                }, new PatternDefinition[2]
                {
                new PatternDefinition("runtimes/{rid}/nativeassets/{tfm}/{any}", DotnetAnyTable),
                new PatternDefinition("runtimes/{rid}/native/{any}", null, DefaultTfmAny)
                });
                ResourceAssemblies = new PatternSet(conventions.Properties, new PatternDefinition[2]
                {
                new PatternDefinition("runtimes/{rid}/lib/{tfm}/{locale?}/{any?}", DotnetAnyTable),
                new PatternDefinition("lib/{tfm}/{locale?}/{any?}", DotnetAnyTable)
                }, new PatternDefinition[2]
                {
                new PatternDefinition("runtimes/{rid}/lib/{tfm}/{locale}/{satelliteAssembly}", DotnetAnyTable),
                new PatternDefinition("lib/{tfm}/{locale}/{satelliteAssembly}", DotnetAnyTable)
                });
                MSBuildFiles = new PatternSet(conventions.Properties, new PatternDefinition[2]
                {
                new PatternDefinition("build/{tfm}/{msbuild?}", DotnetAnyTable),
                new PatternDefinition("build/{msbuild?}", null, DefaultTfmAny)
                }, new PatternDefinition[2]
                {
                new PatternDefinition("build/{tfm}/{msbuild}", DotnetAnyTable),
                new PatternDefinition("build/{msbuild}", null, DefaultTfmAny)
                });
                MSBuildMultiTargetingFiles = new PatternSet(conventions.Properties, new PatternDefinition[2]
                {
                new PatternDefinition("buildMultiTargeting/{msbuild?}", null, DefaultTfmAny),
                new PatternDefinition("buildCrossTargeting/{msbuild?}", null, DefaultTfmAny)
                }, new PatternDefinition[2]
                {
                new PatternDefinition("buildMultiTargeting/{msbuild}", null, DefaultTfmAny),
                new PatternDefinition("buildCrossTargeting/{msbuild}", null, DefaultTfmAny)
                });
                ContentFiles = new PatternSet(conventions.Properties, new PatternDefinition[1]
                {
                new PatternDefinition("contentFiles/{codeLanguage}/{tfm}/{any?}")
                }, new PatternDefinition[1]
                {
                new PatternDefinition("contentFiles/{codeLanguage}/{tfm}/{any?}")
                });
                ToolsAssemblies = new PatternSet(conventions.Properties, new PatternDefinition[1]
                {
                new PatternDefinition("tools/{tfm}/{rid}/{any?}", AnyTable)
                }, new PatternDefinition[1]
                {
                new PatternDefinition("tools/{tfm}/{rid}/{any?}", AnyTable)
                });
                EmbedAssemblies = new PatternSet(conventions.Properties, new PatternDefinition[1]
                {
                new PatternDefinition("embed/{tfm}/{any?}", DotnetAnyTable)
                }, new PatternDefinition[1]
                {
                new PatternDefinition("embed/{tfm}/{assembly}", DotnetAnyTable)
                });
                MSBuildTransitiveFiles = new PatternSet(conventions.Properties, new PatternDefinition[2]
                {
                new PatternDefinition("buildTransitive/{tfm}/{msbuild?}", DotnetAnyTable),
                new PatternDefinition("buildTransitive/{msbuild?}", null, DefaultTfmAny)
                }, new PatternDefinition[2]
                {
                new PatternDefinition("buildTransitive/{tfm}/{msbuild}", DotnetAnyTable),
                new PatternDefinition("buildTransitive/{msbuild}", null, DefaultTfmAny)
                });
            }
        }

        private static readonly ContentPropertyDefinition LocaleProperty = new ContentPropertyDefinition(PropertyNames.Locale, Locale_Parser);

        private static readonly ContentPropertyDefinition AnyProperty = new ContentPropertyDefinition(PropertyNames.AnyValue, (string o, PatternTable t) => o);

        private static readonly ContentPropertyDefinition AssemblyProperty = new ContentPropertyDefinition(PropertyNames.ManagedAssembly, AllowEmptyFolderParser, new string[3] { ".dll", ".winmd", ".exe" });

        private static readonly ContentPropertyDefinition MSBuildProperty = new ContentPropertyDefinition(PropertyNames.MSBuild, AllowEmptyFolderParser, new string[2] { ".targets", ".props" });

        private static readonly ContentPropertyDefinition SatelliteAssemblyProperty = new ContentPropertyDefinition(PropertyNames.SatelliteAssembly, AllowEmptyFolderParser, new string[1] { ".resources.dll" });

        private static readonly ContentPropertyDefinition CodeLanguageProperty = new ContentPropertyDefinition(PropertyNames.CodeLanguage, CodeLanguage_Parser);

        private static readonly Dictionary<string, object> NetTFMTable = new Dictionary<string, object>
    {
        {
            "tfm",
            new NuGetFramework(".NETFramework", FrameworkConstants.EmptyVersion)
        },
        { "tfm_raw", "net0" }
    };

        private static readonly Dictionary<string, object> DefaultTfmAny = new Dictionary<string, object>
    {
        {
            PropertyNames.TargetFrameworkMoniker,
            AnyFramework.Instance
        },
        {
            PropertyNames.TargetFrameworkMoniker + "_raw",
            "any"
        }
    };

        private static readonly PatternTable DotnetAnyTable = new PatternTable(new PatternTableEntry[1]
        {
        new PatternTableEntry(PropertyNames.TargetFrameworkMoniker, "any", FrameworkConstants.CommonFrameworks.DotNet)
        });

        private static readonly PatternTable AnyTable = new PatternTable(new PatternTableEntry[1]
        {
        new PatternTableEntry(PropertyNames.TargetFrameworkMoniker, "any", AnyFramework.Instance)
        });

        private static readonly FrameworkReducer FrameworkReducer = new FrameworkReducer();

        private RuntimeGraph _runtimeGraph;

        private Dictionary<string, NuGetFramework> _frameworkCache = new Dictionary<string, NuGetFramework>(StringComparer.Ordinal);

        public ExtendedManagedCodeCriteria ExtendedCriteria { get; }

        public IReadOnlyDictionary<string, ContentPropertyDefinition> ExtendedProperties { get; }

        public ExtendedManagedCodePatterns ExtendedPatterns { get; }

        public ExtendedManagedCodeConventions(RuntimeGraph runtimeGraph): base(runtimeGraph)
        {
            _runtimeGraph = runtimeGraph;
            Console.WriteLine("hello");
            ExtendedProperties = new ReadOnlyDictionary<string, ContentPropertyDefinition>(new Dictionary<string, ContentPropertyDefinition>
            {
                [AnyProperty.Name] = AnyProperty,
                [AssemblyProperty.Name] = AssemblyProperty,
                [LocaleProperty.Name] = LocaleProperty,
                [MSBuildProperty.Name] = MSBuildProperty,
                [SatelliteAssemblyProperty.Name] = SatelliteAssemblyProperty,
                [CodeLanguageProperty.Name] = CodeLanguageProperty,
                [PropertyNames.RuntimeIdentifier] = new ContentPropertyDefinition(PropertyNames.RuntimeIdentifier, (string o, PatternTable t) => o, RuntimeIdentifier_EqualityTest),
                [PropertyNames.TargetFrameworkMoniker] = new ContentPropertyDefinition(PropertyNames.TargetFrameworkMoniker, TargetFrameworkName_Parser, TargetFrameworkName_CompatibilityTest, TargetFrameworkName_NearestCompareTest)
            });
            ExtendedCriteria = new ExtendedManagedCodeCriteria(this);
            ExtendedPatterns = new ExtendedManagedCodePatterns(this);
        }

        private bool RuntimeIdentifier_EqualityTest(object criteria, object available)
        {
            if (_runtimeGraph == null)
            {
                return object.Equals(criteria, available);
            }

            string text = criteria as string;
            string text2 = available as string;
            Console.WriteLine(text, text2);
            if (text != null && text2 != null)
            {
                return _runtimeGraph.AreCompatible(text, text2);
            }

            return false;
        }

        private static object CodeLanguage_Parser(string name, PatternTable table)
        {
            if (table != null && table.TryLookup(PropertyNames.CodeLanguage, name, out var value))
            {
                return value;
            }

            if (!name.All((char c) => char.IsLetterOrDigit(c)))
            {
                return null;
            }

            return name;
        }

        private static object Locale_Parser(string name, PatternTable table)
        {
            if (table != null && table.TryLookup(PropertyNames.Locale, name, out var value))
            {
                return value;
            }

            if (name.Length == 2)
            {
                return name;
            }

            if (name.Length >= 4 && name[2] == '-')
            {
                return name;
            }

            return null;
        }

        private object TargetFrameworkName_Parser(string name, PatternTable table)
        {
            object value = null;
            if (table != null && table.TryLookup(PropertyNames.TargetFrameworkMoniker, name, out value))
            {
                return value;
            }

            if (!string.IsNullOrEmpty(name))
            {
                if (!_frameworkCache.TryGetValue(name, out var value2))
                {
                    value2 = TargetFrameworkName_ParserCore(name);
                    _frameworkCache.Add(name, value2);
                }

                return value2;
            }

            return TargetFrameworkName_ParserCore(name);
        }

        private static NuGetFramework TargetFrameworkName_ParserCore(string name)
        {
            NuGetFramework nuGetFramework = NuGetFramework.ParseFolder(name);
            if (!nuGetFramework.IsUnsupported)
            {
                return nuGetFramework;
            }

            nuGetFramework = NuGetFramework.ParseFrameworkName(name, DefaultFrameworkNameProvider.Instance);
            if (!nuGetFramework.IsUnsupported)
            {
                return nuGetFramework;
            }

            return new NuGetFramework(name, FrameworkConstants.EmptyVersion);
        }

        private static object AllowEmptyFolderParser(string s, PatternTable table)
        {
            if (!PackagingCoreConstants.EmptyFolder.Equals(s, StringComparison.Ordinal))
            {
                return null;
            }

            return s;
        }

        private static bool TargetFrameworkName_CompatibilityTest(object criteria, object available)
        {
            NuGetFramework nuGetFramework = criteria as NuGetFramework;
            NuGetFramework nuGetFramework2 = available as NuGetFramework;
            if (nuGetFramework != null && nuGetFramework2 != null)
            {
                if (nuGetFramework.IsAny && nuGetFramework2.IsAny)
                {
                    return true;
                }

                if (object.Equals(NuGetFramework.AnyFramework, nuGetFramework2))
                {
                    return true;
                }

                if (nuGetFramework.IsAny || nuGetFramework2.IsAny)
                {
                    return false;
                }

                return NuGetFrameworkUtility.IsCompatibleWithFallbackCheck(nuGetFramework, nuGetFramework2);
            }

            return false;
        }

        private static int TargetFrameworkName_NearestCompareTest(object projectFramework, object criteria, object available)
        {
            NuGetFramework nuGetFramework = projectFramework as NuGetFramework;
            NuGetFramework nuGetFramework2 = criteria as NuGetFramework;
            NuGetFramework nuGetFramework3 = available as NuGetFramework;
            if (nuGetFramework2 != null && nuGetFramework3 != null && nuGetFramework != null && !nuGetFramework2.Equals(nuGetFramework3))
            {
                NuGetFramework[] possibleFrameworks = new NuGetFramework[2] { nuGetFramework2, nuGetFramework3 };
                NuGetFramework nearest = FrameworkReducer.GetNearest(nuGetFramework, possibleFrameworks);
                if (nuGetFramework2.Equals(nearest))
                {
                    return -1;
                }

                if (nuGetFramework3.Equals(nearest))
                {
                    return 1;
                }
            }

            return 0;
        }
    }
}
