// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.CommandFactory;
using Microsoft.DotNet.ToolManifest;
using Microsoft.DotNet.ToolPackage;
using Microsoft.Extensions.EnvironmentAbstractions;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Tools.Tool.Run
{
    internal class ToolRunCommand : CommandBase
    {
        private readonly string _toolCommandName;
        private readonly LocalToolsCommandResolver _localToolsCommandResolver;
        private readonly IEnumerable<string> _forwardArgument;
        private readonly string _rollForward;
        private readonly ToolManifestFinder _toolManifest;
        // private readonly ToolManifestEditor _toolManifestEditor;

        public ToolRunCommand(
            ParseResult result,
            LocalToolsCommandResolver localToolsCommandResolver = null,
            ToolManifestFinder toolManifest = null)
            : base(result)
        {
            _toolCommandName = result.GetValue(ToolRunCommandParser.CommandNameArgument);
            _forwardArgument = result.GetValue(ToolRunCommandParser.CommandArgument);
            _localToolsCommandResolver = localToolsCommandResolver ?? new LocalToolsCommandResolver();
            _rollForward = result.GetValue(ToolRunCommandParser.RollForwardOption);
            _toolManifest = toolManifest ?? new ToolManifestFinder(new DirectoryPath(Directory.GetCurrentDirectory()));
        }

        public override int Execute()
        {
            // Update --roll-forward if it is true for the tool in the manifest file
            if (_toolManifest.TryFind(new ToolCommandName(_toolCommandName), out var toolManifestPackage))
            {
                IReadOnlyList<FilePath> manifestFilesContainPackageId
                 = _toolManifest.FindByPackageId(toolManifestPackage.PackageId);

                bool rollForwardValue = false;

                if (manifestFilesContainPackageId != null && manifestFilesContainPackageId.Count() == 1)
                {
                    string jsonContent = File.ReadAllText(manifestFilesContainPackageId[0].ToString());
                    JObject jsonObject = JObject.Parse(jsonContent);
                    rollForwardValue = bool.Parse((string)jsonObject["tools"][toolManifestPackage.PackageId.ToString()]["rollForward"]);
                }
            }

            CommandSpec commandspec = _localToolsCommandResolver.ResolveStrict(new CommandResolverArguments()
            {
                // since LocalToolsCommandResolver is a resolver, and all resolver input have dotnet-
                CommandName = $"dotnet-{_toolCommandName}",
                CommandArguments = (_rollForward != null ? new List<string> { "--roll-forward", _rollForward } : Enumerable.Empty<string>()).Concat(_forwardArgument)
            });

            if (commandspec == null)
            {
                throw new GracefulException(
                    new string[] { string.Format(LocalizableStrings.CannotFindCommandName, _toolCommandName) },
                    isUserError: false);
            }

            var result = CommandFactoryUsingResolver.Create(commandspec).Execute();
            return result.ExitCode;
        }
    }
}
