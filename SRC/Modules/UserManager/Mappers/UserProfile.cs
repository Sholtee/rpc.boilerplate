using AutoMapper;

namespace Modules
{
    internal class UserProfile: Profile
    {
        public UserProfile() => CreateMap<API.User, DAL.API.User>()
            .ForMember(dst => dst.EmailOrUserName, opts => opts.MapFrom(src => src.EmailOrUserName))
            .ForMember(dst => dst.FullName, opts => opts.MapFrom(src => src.FullName));
    }
}
