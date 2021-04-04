using System;
using System.Threading.Tasks;

using AutoMapper;

using Solti.Utils.Rpc.Interfaces;

namespace Modules
{
    using API;
    using DAL.API;
    using Services.API;

    public class UserManager : IUserManager
    {
        private readonly Lazy<ICache> FCache;
        public ICache Cache => FCache.Value;

        private readonly Lazy<IUserRepository> FUserRepository;
        public IUserRepository UserRepository => FUserRepository.Value;

        private readonly Lazy<IConfig> FConfig;
        public IConfig Config => FConfig.Value;

        public IRequestContext RequestContext { get; }

        public IMapper Mapper { get; }

        public UserManager(Lazy<IUserRepository> userRepository, Lazy<ICache> cache, Lazy<IConfig> config, IRequestContext requestContext) 
        {
            FUserRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            FCache = cache ?? throw new ArgumentNullException(nameof(cache));
            FConfig = config ?? throw new ArgumentNullException(nameof(config));
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

            Guid sessionId = Guid.NewGuid();
            Cache.Add(sessionId.ToString(), user, TimeSpan.FromMinutes(Config.Server.SessionTimeoutInMinutes));

            return sessionId;
        }

        public void Logout()
        {
            if (RequestContext.SessionId is not null)
                Cache.Remove(RequestContext.SessionId);
        }

        public async Task<API.PartialUserList> List(int skip, int count) => Mapper.Map<API.PartialUserList>
        (
            await UserRepository.List(skip, count)
        );
    }
}
