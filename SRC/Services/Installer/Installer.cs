using System;

namespace Services
{
    using API;
    using DAL.API;

    using Properties;

    public class Installer : IInstaller
    {
        public IUserRepository UserRepository { get; }

        public IDbSchemaManager Schema { get; }

        public Installer(IDbSchemaManager schemaManager, IUserRepository userRepository)
        {
            UserRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            Schema = schemaManager ?? throw new ArgumentNullException(nameof(schemaManager));
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
            ? string.Format(null, Resources.INSTALLED, Schema.GetLastMigrationUtc())
            : Resources.NOT_INSTALLED;

        public void Migrate(string migrationFilesDir) => throw new NotImplementedException();
    }
}
