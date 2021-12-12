namespace Services.API
{
    public class InstallArguments
    {
        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string User { get; init; }
        #pragma warning restore CS8618

        public string? Password { get; init; }

        public string? PasswordVariable { get; init; }
    }
}
