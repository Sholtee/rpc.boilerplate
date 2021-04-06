using AutoMapper;

namespace DAL
{
    internal class UserProfile: Profile
    {
        public UserProfile() => CreateMap<UserView, API.UserEx>()
            .ForMember(dst => dst.Id, opts => opts.MapFrom(src => src.Id))
            .ForMember(dst => dst.EmailOrUserName, opts => opts.MapFrom(src => src.EmailOrUserName))
            .ForMember(dst => dst.FullName, opts => opts.MapFrom(src => src.FullName))
            .ForMember(dst => dst.Roles, opts => opts.MapFrom(src => src.Roles));
    }
}
