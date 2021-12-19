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

        private readonly Lazy<ISessionRepository> FSessionRepository;
        public ISessionRepository SessionRepository => FSessionRepository.Value;

        public IRequestContext RequestContext { get; }

        public IMapper Mapper { get; }

        public UserManager(Lazy<IUserRepository> userRepository, Lazy<ISessionRepository> sessionRepository, IRequestContext requestContext) 
        {
            FUserRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            FSessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
            RequestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));

            Mapper = new MapperConfiguration(cfg => cfg.AddProfiles(new Profile[]
            {
                new UserProfile(),
                new UserListProfile()
            })).CreateMapper();
        }

        public async Task<Guid> Create(API.User user, string pw, string[] groups) => await UserRepository
            .Create(Mapper.Map<DAL.API.User>(user), pw, groups, RequestContext.Cancellation);

        public async Task<Guid> Login(string emailOrUserName, string pw)
        {
            DAL.API.UserEx user = await UserRepository.GetByCredentials(emailOrUserName, pw, RequestContext.Cancellation);

            return await SessionRepository.GetOrCreate(user.Id, RequestContext.Cancellation);
        }

        public async Task Logout()
        {
            if (RequestContext.SessionId is not null)
                await SessionRepository.ExpireById(Guid.Parse(RequestContext.SessionId), RequestContext.Cancellation);
        }

        public async Task<API.PartialUserList> List(int skip, int count) => Mapper.Map<DAL.API.PartialUserList, API.PartialUserList>
        (
            await UserRepository.List(skip, count, RequestContext.Cancellation)
        );

        public async Task Delete(Guid userId)
        {
            await UserRepository.DeleteById(userId, RequestContext.Cancellation);
            await SessionRepository.ExpireByUserId(userId, RequestContext.Cancellation);
        }

        public async Task DeleteCurrent()
        {
            await Delete(await SessionRepository.GetUserId(Guid.Parse(RequestContext.SessionId!)));
        }
    }
}
