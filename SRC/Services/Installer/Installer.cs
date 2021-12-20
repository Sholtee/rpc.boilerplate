using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Services
{
    using API;
    using DAL.API;

    using Properties;

    public class Installer : IInstaller
    {
        public IUserRepository UserRepository { get; }

        public IDbSchemaManager Schema { get; }

        public IConfig Config { get; }

        public Installer(IDbSchemaManager schemaManager, IUserRepository userRepository, IConfig config)
        {
            UserRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            Schema = schemaManager ?? throw new ArgumentNullException(nameof(schemaManager));
            Config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void Install(InstallArguments args)
        {
            if (args is null)
                throw new ArgumentNullException(nameof(args));

            if (!Schema.IsInitialized)
                Schema.Initialize();

            string? password = !string.IsNullOrEmpty(args.PasswordVariable)
                ? Environment.GetEnvironmentVariable(args.PasswordVariable)!
                : args.Password;

            if (password is null)
                throw new InvalidOperationException(Resources.NO_PASSWORD);

            UserRepository.Create
            (
                new User
                {
                    EmailOrUserName = args.User,
                    FullName = "Superuser"
                },
                password,
                new[] { "Admins" }
            ).GetAwaiter().GetResult();
        }

        public string Status => Schema.IsInitialized
            ? string.Format(null, $"{Resources.INSTALLED} {Resources.LAST_MIGRATION}", Schema.GetLastMigrationUtc())
            : Resources.NOT_INSTALLED;

        public IEnumerable<string> Migrate()
        {
            foreach (string sqlFile in Directory.EnumerateFiles(Config.Database.MigrationDir, "*.sql", SearchOption.TopDirectoryOnly).OrderBy(f => f))
            {
                bool installed = Schema.Migrate
                (
                    File.GetCreationTimeUtc(sqlFile),
                    File.ReadAllText(sqlFile),
                    Path.GetFileName(sqlFile)
                );
                yield return $"{sqlFile} [{(installed ? Resources.INSTALLED : Resources.NOT_REQUIRED)}]";
            }
        }
    }
}
