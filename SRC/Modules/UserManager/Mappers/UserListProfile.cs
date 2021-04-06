using AutoMapper;

namespace Modules
{
    internal class UserListProfile : Profile
    {
        public UserListProfile() => CreateMap<DAL.API.PartialUserList, API.PartialUserList>()
            .ForMember(dst => dst.AllEntries, opt => opt.MapFrom(src => src.AllEntries))
            .ForMember(dst => dst.Entries, opt => opt.MapFrom(src => src.Entries));
    }
}
