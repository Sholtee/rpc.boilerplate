using AutoMapper;

namespace Modules
{
    internal class UserProfile: Profile
    {
        public UserProfile() => CreateMap<API.User, DAL.API.User>()
            .ForMember(dst => dst.Id, opt => opt.Ignore());
    }
}
