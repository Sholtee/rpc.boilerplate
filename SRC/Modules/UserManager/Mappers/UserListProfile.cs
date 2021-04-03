using AutoMapper;

namespace Modules
{
    internal class UserListProfile : Profile
    {
        public UserListProfile() => CreateMap<API.PartialUserList, DAL.API.PartialUserList>()
            .ForMember(dst => dst.AllEntries, opt => opt.MapFrom(src => src.AllEntries))
            .ForMember(dst => dst.Entries, opt => opt.MapFrom(src => src.Entries));
    }
}
