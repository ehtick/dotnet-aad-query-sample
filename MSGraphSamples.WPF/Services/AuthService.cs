﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using MsGraph_Samples.Helpers;

namespace MsGraph_Samples.Services
{
    public interface IAuthService : IAuthenticationProvider
    {
        IAccount? Account { get; }
        event Action? AuthenticationSuccessful;
        GraphServiceClient GetServiceClient();
        Task Logout();
    }

    public class AuthService : IAuthService
    {
        public IAccount? Account { get; private set; }
        public event Action? AuthenticationSuccessful;

        /// <summary>
        /// The content of Tenant by the information about the accounts allowed to sign-in in your application:
        /// - for Work or School account in your org, use your tenant ID, or domain
        /// - for any Work or School accounts, use organizations
        /// - for any Work or School accounts, or Microsoft personal account, use common
        /// - for Microsoft Personal account, use consumers
        /// </summary>
        private const string Tenant = "organizations";

        // To change from Microsoft public cloud to a national cloud, use another value of AzureCloudInstance
        private const AzureCloudInstance CloudInstance = AzureCloudInstance.AzurePublic;

        // Make sure the user you login with has "Directory.Read.All" permissions
        private readonly string[] _scopes = { "Directory.Read.All" };
        private readonly IPublicClientApplication _publicClientApp;
        private GraphServiceClient? _graphClient;

        public AuthService(string clientId)
        {
            _publicClientApp = PublicClientApplicationBuilder.Create(clientId)
                .WithAuthority(CloudInstance, Tenant)
                .WithDefaultRedirectUri() // MAKE SURE YOU SET http://localhost AS REDIRECT URI IN THE AZURE PORTAL
                .Build();
            TokenCacheHelper.EnableSerialization(_publicClientApp.UserTokenCache);
        }

        public GraphServiceClient GetServiceClient()
        {
            //var authProvider = new DelegateAuthenticationProvider(AuthenticateRequestAsync);
            //return _graphClient ??= new GraphServiceClient(authProvider);

            InteractiveAuthenticationProvider authenticationProvider = new InteractiveAuthenticationProvider(_publicClientApp, _scopes);
            return _graphClient ??= new GraphServiceClient(authenticationProvider);
        }

        public async Task AuthenticateRequestAsync(HttpRequestMessage requestMessage)
        {
            var accessToken = await AcquireTokenAsync();
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            if (accessToken != null)
                AuthenticationSuccessful?.Invoke();
        }

        private async Task<string?> AcquireTokenAsync()
        {
            var accounts = await _publicClientApp
                .GetAccountsAsync().ConfigureAwait(false);
            Account = accounts.FirstOrDefault();
            
            AuthenticationResult authResult;
            try
            {
                authResult = await _publicClientApp
                    .AcquireTokenSilent(_scopes, Account)
                    .ExecuteAsync().ConfigureAwait(false);
            }
            catch (MsalUiRequiredException ex)
            {
                // A MsalUiRequiredException happened on AcquireTokenSilentAsync.
                // This indicates you need to call AcquireTokenAsync to acquire a token
                Debug.WriteLine($"MsalUiRequiredException: {ex.Message}");
                try
                {
                    authResult = await _publicClientApp
                        .AcquireTokenInteractive(_scopes)
                        .ExecuteAsync().ConfigureAwait(false);

                    accounts = await _publicClientApp
                        .GetAccountsAsync().ConfigureAwait(false);
                    Account = accounts.FirstOrDefault();
                }
                catch (MsalException msalex)
                {
                    Debug.WriteLine($"Error Acquiring Token:{Environment.NewLine}{msalex}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error Acquiring Token Silently:{Environment.NewLine}{ex}");
                return null;
            }

            return authResult?.AccessToken;
        }

        public async Task Logout()
        {
            TokenCacheHelper.Clear();
            _graphClient = null;
            await _publicClientApp.RemoveAsync(Account)
                .ConfigureAwait(false);
        }
    }
}