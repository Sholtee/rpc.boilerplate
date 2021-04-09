using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using CommandLine;

using Solti.Utils.DI.Interfaces;
using Solti.Utils.Rpc.Interfaces;

namespace Server
{
    using DAL.API;
    using Services.API;

    [TransactionAspect]
    public interface IInstaller 
    {
        [Transactional]
        void Run();
    }

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

        public void Run() 
        {
            SchemaManager.CreateTables(typeof(AppHost)
                .Assembly
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
