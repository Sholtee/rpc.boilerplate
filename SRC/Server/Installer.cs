using System;
using System.Collections.Generic;
using System.Reflection;

using CommandLine;

using Solti.Utils.DI.Interfaces;

namespace Server
{
    using Modules.API;
    using Services.API;

    public class Installer
    {
        public IDbSchemaManager SchemaManager { get; }

        public IUserManager UserManager { get; }

        private CmdArgs Options { get; }

        public Installer(IDbSchemaManager schemaManager, [Options(Name = "CommandLineArgs")] IReadOnlyList<string> cmdArgs, IUserManager userManager)
        {
            SchemaManager = schemaManager;
            UserManager = userManager;
            Options = CommandLine
                .Parser
                .Default
                .ParseArguments<CmdArgs>(cmdArgs)
                .MapResult(opts => opts, errs => new CmdArgs());
        }

        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private sealed class CmdArgs 
        {
            [Option('r', nameof(Root))]
            public string Root { get; set; }

            [Option('p', nameof(Password))]
            public string Password { get; set; }

            [Option('v', nameof(PasswordVariable))]
            public string PasswordVariable { get; set; }
        }
        #pragma warning restore CS8618

        public void Run(params Assembly[] asmsContainingTableDefs) 
        {
            SchemaManager.CreateTables(asmsContainingTableDefs);

            UserManager.Create
            (
                new User 
                { 
                    EmailOrUserName = Options.Root, 
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
