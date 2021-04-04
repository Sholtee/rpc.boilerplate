using System;
using System.Threading.Tasks;

using AutoMapper;

using Solti.Utils.Rpc.Interfaces;

namespace Modules
{
    using API;
    using DAL.API;

    public class UserManager : IUserManager
    {
        private readonly Lazy<IUserRepository> FUserRepository;
        public IUserRepository UserRepository => FUserRepository.Value;

        public IRequestContext RequestContext { get; }

        public IMapper Mapper { get; }

        public UserManager(Lazy<IUserRepository> userRepository, IRequestContext requestContext) 
        {
            FUserRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            RequestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));

            Mapper = new MapperConfiguration(cfg => cfg.AddProfiles(new Profile[]
            {
                new UserProfile(),
                new UserListProfile()
            })).CreateMapper();
        }

        public async Task<Guid> Create(API.User user, string pw) => await UserRepository
            .Create(Mapper.Map<DAL.API.User>(user), pw, RequestContext.Cancellation);

        public async Task<Guid> Login(string emailOrUserName, string pw)
        {
            DAL.API.User user = await UserRepository.QueryByCredentials(emailOrUserName, pw, RequestContext.Cancellation);

            return await UserRepository.CreateSession(user.Id!.Value, RequestContext.Cancellation);
        }

        public async Task Logout()
        {
            if (RequestContext.SessionId is not null)
                await UserRepository.DeleteSession(Guid.Parse(RequestContext.SessionId));
        }

        public async Task<API.PartialUserList> List(int skip, int count) => Mapper.Map<API.PartialUserList>
        (
            await UserRepository.List(skip, count)
        );
    }
}
