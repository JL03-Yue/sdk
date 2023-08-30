// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ShellShim;
using Microsoft.DotNet.ToolPackage;
using Microsoft.DotNet.Tools;
using Microsoft.DotNet.Tools.Tests.ComponentMocks;
using Microsoft.DotNet.Tools.Tool.Install;
using Microsoft.DotNet.Tools.Tool.Uninstall;
using Microsoft.Extensions.DependencyModel.Tests;
using Microsoft.Extensions.EnvironmentAbstractions;
using InstallLocalizableStrings = Microsoft.DotNet.Tools.Tool.Install.LocalizableStrings;
using LocalizableStrings = Microsoft.DotNet.Tools.Tool.Uninstall.LocalizableStrings;
using Parser = Microsoft.DotNet.Cli.Parser;

namespace Microsoft.DotNet.Tests.Commands.Tool
{
    public class ToolUninstallGlobalOrToolPathCommandTests
    {
        private readonly BufferedReporter _reporter;
        private readonly IFileSystem _fileSystem;
        private readonly EnvironmentPathInstructionMock _environmentPathInstructionMock;

        private const string PackageId = "global.tool.console.demo";
        private const string PackageVersion = "1.0.4";
        private readonly string _shimsDirectory;
        private readonly string _toolsDirectory;

        public ToolUninstallGlobalOrToolPathCommandTests()
        {
            _reporter = new BufferedReporter();
            _fileSystem = new FileSystemMockBuilder().UseCurrentSystemTemporaryDirectory().Build();
            var tempDirectory = _fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            _shimsDirectory = Path.Combine(tempDirectory, "shims");
            _toolsDirectory = Path.Combine(tempDirectory, "tools");
            _environmentPathInstructionMock = new EnvironmentPathInstructionMock(_reporter, _shimsDirectory);
        }

        [Fact]
        public void GivenANonExistentPackageItErrors()
        {
            var packageId = "does.not.exist";
            var command = CreateUninstallCommand($"-g {packageId}");

            Action a = () => command.Execute();

            a.Should().Throw<GracefulException>()
                .And
                .Message
                .Should()
                .Be(string.Format(LocalizableStrings.ToolNotInstalled, packageId));
        }

        [Fact]
        public void GivenAPackageItUninstalls()
        {
            CreateInstallCommand($"-g {PackageId}").Execute().Should().Be(0);

            _reporter
                .Lines
                .Last()
                .Should()
                .Contain(string.Format(
                    InstallLocalizableStrings.InstallationSucceeded,
                    ProjectRestorerMock.DefaultToolCommandName,
                    PackageId,
                    PackageVersion));

            var packageDirectory = new DirectoryPath(Path.GetFullPath(_toolsDirectory))
                .WithSubDirectories(PackageId, PackageVersion);
            var shimPath = Path.Combine(
                _shimsDirectory,
                ProjectRestorerMock.DefaultToolCommandName +
                    (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : ""));

            _fileSystem.Directory.Exists(packageDirectory.Value).Should().BeTrue();
            _fileSystem.File.Exists(shimPath).Should().BeTrue();

            _reporter.Lines.Clear();

            CreateUninstallCommand($"-g {PackageId}").Execute().Should().Be(0);

            _reporter
                .Lines
                .Single()
                .Should()
                .Contain(string.Format(
                    LocalizableStrings.UninstallSucceeded,
                    PackageId,
                    PackageVersion));

            _fileSystem.Directory.Exists(packageDirectory.Value).Should().BeFalse();
            _fileSystem.File.Exists(shimPath).Should().BeFalse();
        }

        [Fact]
        public void GivenAPackageWhenCallFromUninstallRedirectCommandItUninstalls()
        {
            CreateInstallCommand($"-g {PackageId}").Execute().Should().Be(0);

            _reporter
                .Lines
                .Last()
                .Should()
                .Contain(string.Format(
                    InstallLocalizableStrings.InstallationSucceeded,
                    ProjectRestorerMock.DefaultToolCommandName,
                    PackageId,
                    PackageVersion));

            var packageDirectory = new DirectoryPath(Path.GetFullPath(_toolsDirectory))
                .WithSubDirectories(PackageId, PackageVersion);
            var shimPath = Path.Combine(
                _shimsDirectory,
                ProjectRestorerMock.DefaultToolCommandName +
                (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : ""));

            _fileSystem.Directory.Exists(packageDirectory.Value).Should().BeTrue();
            _fileSystem.File.Exists(shimPath).Should().BeTrue();

            _reporter.Lines.Clear();


            ParseResult result = Parser.Instance.Parse("dotnet tool uninstall " + $"-g {PackageId}");

            (IToolPackageStore, IToolPackageStoreQuery, IToolPackageUninstaller) CreateToolPackageStoreAndUninstaller(
                DirectoryPath? directoryPath)
            {
                var store = new ToolPackageStoreMock(
                    new DirectoryPath(_toolsDirectory),
                    _fileSystem);
                var packageUninstaller = new ToolPackageUninstallerMock(_fileSystem, store);
                return (store, store, packageUninstaller);
            }

            var toolUninstallGlobalOrToolPathCommand = new ToolUninstallGlobalOrToolPathCommand(
                result,
                CreateToolPackageStoreAndUninstaller,
                (_, _) => new ShellShimRepository(
                    new DirectoryPath(_shimsDirectory),
                    string.Empty,
                    fileSystem: _fileSystem,
                    appHostShellShimMaker: new AppHostShellShimMakerMock(_fileSystem)),
                _reporter);

            var uninstallCommand
                = new ToolUninstallCommand(
                    result,
                    toolUninstallGlobalOrToolPathCommand: toolUninstallGlobalOrToolPathCommand);

            uninstallCommand.Execute().Should().Be(0);

            _reporter
                .Lines
                .Single()
                .Should()
                .Contain(string.Format(
                    LocalizableStrings.UninstallSucceeded,
                    PackageId,
                    PackageVersion));

            _fileSystem.Directory.Exists(packageDirectory.Value).Should().BeFalse();
            _fileSystem.File.Exists(shimPath).Should().BeFalse();
        }

        [Fact]
        public void GivenAFailureToUninstallItLeavesItInstalled()
        {
            CreateInstallCommand($"-g {PackageId}").Execute().Should().Be(0);

            _reporter
                .Lines
                .Last()
                .Should()
                .Contain(string.Format(
                    InstallLocalizableStrings.InstallationSucceeded,
                    ProjectRestorerMock.DefaultToolCommandName,
                    PackageId,
                    PackageVersion));

            var packageDirectory = new DirectoryPath(Path.GetFullPath(_toolsDirectory))
                .WithSubDirectories(PackageId, PackageVersion);
            var shimPath = Path.Combine(
                _shimsDirectory,
                ProjectRestorerMock.DefaultToolCommandName +
                    (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : ""));

            _fileSystem.Directory.Exists(packageDirectory.Value).Should().BeTrue();
            _fileSystem.File.Exists(shimPath).Should().BeTrue();

            Action a = () => CreateUninstallCommand(
                options: $"-g {PackageId}",
                uninstallCallback: () => throw new IOException("simulated error"))
                .Execute();

            a.Should().Throw<GracefulException>()
                .And
                .Message
                .Should()
                .Be(string.Format(
                    CommonLocalizableStrings.FailedToUninstallToolPackage,
                    PackageId,
                    "simulated error"));

            _fileSystem.Directory.Exists(packageDirectory.Value).Should().BeTrue();
            _fileSystem.File.Exists(shimPath).Should().BeTrue();
        }

        [Fact]
        public void GivenAnInvalidToolPathItThrowsException()
        {
            var toolPath = "tool-path-does-not-exist";

            var uninstallCommand = CreateUninstallCommand($"--tool-path {toolPath} {PackageId}");

            Action a = () => uninstallCommand.Execute();

            a.Should().Throw<GracefulException>()
                .And
                .Message
                .Should()
                .Be(string.Format(LocalizableStrings.InvalidToolPathOption, toolPath));
        }

        private ToolInstallGlobalOrToolPathCommand CreateInstallCommand(string options)
        {
            ParseResult result = Parser.Instance.Parse("dotnet tool install " + options);

            var store = new ToolPackageStoreMock(new DirectoryPath(_toolsDirectory), _fileSystem);
            var packageInstallerMock = new ToolPackageInstallerMock(
                _fileSystem,
                store,
                new ProjectRestorerMock(
                    _fileSystem,
                    _reporter));

            return new ToolInstallGlobalOrToolPathCommand(
                result,
                (location, forwardArguments) => (store, store, packageInstallerMock),
                (_, _) => new ShellShimRepository(
                    new DirectoryPath(_shimsDirectory),
                    string.Empty,
                    fileSystem: _fileSystem,
                    appHostShellShimMaker: new AppHostShellShimMakerMock(_fileSystem)),
                _environmentPathInstructionMock,
                _reporter);
        }

        private ToolUninstallGlobalOrToolPathCommand CreateUninstallCommand(string options, Action uninstallCallback = null)
        {
            ParseResult result = Parser.Instance.Parse("dotnet tool uninstall " + options);

            (IToolPackageStore, IToolPackageStoreQuery, IToolPackageUninstaller) createToolPackageStoreAndUninstaller(
                DirectoryPath? directoryPath)
            {
                var store = new ToolPackageStoreMock(
                    new DirectoryPath(_toolsDirectory),
                    _fileSystem);
                var packageUninstaller = new ToolPackageUninstallerMock(_fileSystem, store, uninstallCallback);
                return (store, store, packageUninstaller);
            }

            return new ToolUninstallGlobalOrToolPathCommand(
                result,
                createToolPackageStoreAndUninstaller,
                (_, _) => new ShellShimRepository(
                    new DirectoryPath(_shimsDirectory),
                    string.Empty,
                    fileSystem: _fileSystem,
                    appHostShellShimMaker: new AppHostShellShimMakerMock(_fileSystem)),
                _reporter);
        }
    }
}
