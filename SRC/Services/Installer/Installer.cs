using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using CommandLine;

using Solti.Utils.DI.Interfaces;

namespace Services
{
    using API;
    using DAL.API;

    public class Installer: IInstaller
    {
        public IDbSchemaManager SchemaManager { get; }

        public IUserRepository UserRepository { get; }

        private CmdArgs Options { get; }

        private static readonly Regex 
            FIsSimpleSwitch = new("^-{1}\\w+$", RegexOptions.Compiled),
            FAsmMatcher = new("\\w+\\.DAL\\.\\w+$", RegexOptions.Compiled);

        public Installer(IDbSchemaManager schemaManager, [Options(Name = "CommandLineArgs")] IReadOnlyList<string> cmdArgs, IUserRepository userRepository)
        {
            SchemaManager = schemaManager;
            UserRepository = userRepository;

            using var parser = new CommandLine.Parser(settings =>
            {
                settings.AutoHelp = false;
                settings.AutoVersion = false;
                settings.IgnoreUnknownArguments = true;
                settings.CaseSensitive = false;
            });

            //
            // ParseArguments() supports long name switches in form of "--longName"
            //

            cmdArgs = cmdArgs
                .Select(arg => FIsSimpleSwitch.IsMatch(arg)
                    ? arg = $"-{arg}"
                    : arg)
                .ToArray();

            Options = ((Parsed<CmdArgs>) parser.ParseArguments<CmdArgs>(cmdArgs)).Value;
        }

        [Verb("install")]
        [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes")]
        private sealed class CmdArgs 
        {
            [Option('u', nameof(User))]
            public string User { get; init; } = string.Empty;

            [Option('p', nameof(Password))]
            public string Password { get; init; } = string.Empty;

            [Option('v', nameof(PasswordVariable))]
            public string PasswordVariable { get; init; } = string.Empty;
        }

        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Parameter is validated by an aspect")]
        public void Run(Assembly hostAssembly) 
        {
            SchemaManager.CreateTables(hostAssembly
                .GetReferencedAssemblies()
                .Where(asm => asm.Name is not null && FAsmMatcher.IsMatch(asm.Name))
                .Select(Assembly.Load)
                .ToArray());

            UserRepository.Create
            (
                new User 
                { 
                    EmailOrUserName = Options.User, 
                    FullName = "Superuser" 
                },
                !string.IsNullOrEmpty(Options.PasswordVariable)
                    ? Environment.GetEnvironmentVariable(Options.PasswordVariable)!
                    : Options.Password, 
                new[] { "Admins" }
            ).GetAwaiter().GetResult();
        }
    }
}
