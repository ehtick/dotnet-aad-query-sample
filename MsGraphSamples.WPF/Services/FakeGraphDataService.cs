using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace MsGraph_Samples.Services
{
    public class FakeGraphDataService : IGraphDataService
    {
        public string? LastUrl => "https://graph.microsoft.com/beta/users?$count=true";
        private static IList<User> Users => new[]
        {
            new User { Id = "1", DisplayName = "Luca Spolidoro", Mail = "a@b.c" },
            new User { Id = "2", DisplayName = "Pino Quercia", Mail = "pino@quercia.com" },
            new User { Id = "3", DisplayName = "Test Test", Mail = "test@test.com" }
        };

        public Task<User> GetMe()
        {
            return Task.FromResult(Users[0]);
        }

        public IAsyncEnumerable<Application> GetApplicationsAsync(string filter, string search, string select, string orderBy)
        {
            return AsyncEnumerable.Empty<Application>();
        }

        public IAsyncEnumerable<Device> GetDevicesAsync(string filter, string search, string select, string orderBy)
        {
            return AsyncEnumerable.Empty<Device>();
        }

        public IAsyncEnumerable<Group> GetGroupsAsync(string filter, string search, string select, string orderBy)
        {
            return AsyncEnumerable.Empty<Group>();
        }

        public IAsyncEnumerable<User> GetAppOwnersAsUsersAsync(string id)
        {
            return AsyncEnumerable.Empty<User>();
        }

        public IAsyncEnumerable<Group> GetTransitiveMemberOfAsGroupsAsync(string id)
        {
            return AsyncEnumerable.Empty<Group>();
        }

        public IAsyncEnumerable<User> GetTransitiveMembersAsUsersAsync(string id)
        {
            return AsyncEnumerable.Empty<User>();
        }

        public IAsyncEnumerable<User> GetUsersAsync(string filter, string search, string select, string orderBy)
        {
            return AsyncEnumerable.Empty<User>();
        }

        public Task<int> GetUsersRawCountAsync(string filter, string search)
        {
            return Task.FromResult(Users.Count);
        }
    }
}