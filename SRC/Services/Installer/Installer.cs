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

        public Installer(IDbSchemaManager schemaManager, [Options(Name = "CommandLineArgs")] IReadOnlyList<string> cmdArgs, IUserRepository userRepository)
        {
            SchemaManager = schemaManager;
            UserRepository = userRepository;

            using var parser = new CommandLine.Parser(settings =>
            {
                settings.AutoHelp = false;
                settings.AutoVersion = false;
                settings.IgnoreUnknownArguments = true;
            });

            Options = parser
                .ParseArguments<CmdArgs>(cmdArgs)
                .MapResult(opts => opts, errs => new CmdArgs());
        }

        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private sealed class CmdArgs 
        {
            [Option('u', nameof(User))]
            public string User { get; init; }

            [Option('p', nameof(Password))]
            public string Password { get; init; }

            [Option('v', nameof(PasswordVariable))]
            public string PasswordVariable { get; init; }
        }
        #pragma warning restore CS8618

        private static readonly Regex FAsmMatcher = new("\\w+\\.DAL\\.\\w+$", RegexOptions.Compiled);

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
                    FullName = "Root" 
                },
                Options.PasswordVariable is not null
                    ? Environment.GetEnvironmentVariable(Options.PasswordVariable)!
                    : Options.Password, 
                new[] { "Admins" }
            ).GetAwaiter().GetResult();
        }
    }
}
