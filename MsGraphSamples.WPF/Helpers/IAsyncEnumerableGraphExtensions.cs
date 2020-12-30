using System;
using System.Collections.Generic;
using Microsoft.Graph;

namespace MsGraph_Samples.Helpers
{
    public static class IAsyncEnumerableGraphExtensions
    {
        public static async IAsyncEnumerable<Application> ToAsyncEnumerable(this IGraphServiceApplicationsCollectionRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            do
            {
                var page = await request.GetAsync().ConfigureAwait(false);
                foreach (var item in page)
                    yield return item;

                request = page.NextPageRequest;
            } while (request != null);
        }

        public static async IAsyncEnumerable<Group> ToAsyncEnumerable(this IGraphServiceGroupsCollectionRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            do
            {
                var page = await request.GetAsync().ConfigureAwait(false);
                foreach (var item in page)
                    yield return item;

                request = page.NextPageRequest;
            } while (request != null);
        }

        public static async IAsyncEnumerable<Device> ToAsyncEnumerable(this IGraphServiceDevicesCollectionRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            do
            {
                var page = await request.GetAsync().ConfigureAwait(false);
                foreach (var item in page)
                    yield return item;

                request = page.NextPageRequest;
            } while (request != null);
        }

        public static async IAsyncEnumerable<User> ToAsyncEnumerable(this IGraphServiceUsersCollectionRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            do
            {
                var page = await request.GetAsync().ConfigureAwait(false);
                foreach (var item in page)
                    yield return item;

                request = page.NextPageRequest;
            } while (request != null);
        }
    }
}
