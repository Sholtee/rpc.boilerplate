using System;
using System.Diagnostics.CodeAnalysis;

namespace Services.API
{
    [Flags]
    [SuppressMessage("Design", "CA1008:Enums should have zero value")]
    public enum Roles
    {
        AnonymousUser = 0,
        AuthenticatedUser
    }
}
