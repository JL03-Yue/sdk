// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Security.AccessControl;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Tool.Common;

namespace Microsoft.DotNet.Tools.Tool.Update
{
    internal class ToolUpdateCommand : CommandBase
    {
        private readonly ToolUpdateLocalCommand _toolUpdateLocalCommand;
        private readonly ToolUpdateGlobalOrToolPathCommand _toolUpdateGlobalOrToolPathCommand;
        private readonly bool _global;
        private readonly string _toolPath;
        private readonly bool _all;

        public ToolUpdateCommand(
            ParseResult result,
            IReporter reporter = null,
            ToolUpdateGlobalOrToolPathCommand toolUpdateGlobalOrToolPathCommand = null,
            ToolUpdateLocalCommand toolUpdateLocalCommand = null)
            : base(result)
        {
            _toolUpdateLocalCommand
                = toolUpdateLocalCommand ??
                  new ToolUpdateLocalCommand(result);

            _toolUpdateGlobalOrToolPathCommand =
                toolUpdateGlobalOrToolPathCommand
                ?? new ToolUpdateGlobalOrToolPathCommand(result);

            _global = result.GetValue(ToolUpdateCommandParser.GlobalOption);
            _all = result.GetValue(ToolUpdateCommandParser.UpdateAllOption);
            _toolPath = result.GetValue(ToolUpdateCommandParser.ToolPathOption);
        }

        public override int Execute()
        {
            ToolAppliedOption.EnsureNoConflictGlobalLocalToolPathOption(
                _parseResult,
                LocalizableStrings.UpdateToolCommandInvalidGlobalAndLocalAndToolPath);

            ToolAppliedOption.EnsureToolManifestAndOnlyLocalFlagCombination(_parseResult);

            if(_all)
            {
                Console.WriteLine("detected --all option, but it is not implemented yet");
            }

            if (_global || !string.IsNullOrWhiteSpace(_toolPath))
            {
                return _toolUpdateGlobalOrToolPathCommand.Execute();
            }
            else
            {
                return _toolUpdateLocalCommand.Execute();
            }
        }
    }
}
