// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using MsGraph_Samples.Helpers;
using MsGraph_Samples.Services;

namespace MsGraph_Samples.ViewModels
{
    public class ViewModelLocator
    {
        public static bool IsInDesignMode => Application.Current.MainWindow == null;

        public IServiceProvider Services { get; }

        public MainViewModel? MainVM => Services.GetService<MainViewModel>();

        public ViewModelLocator()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            Services = serviceCollection.BuildServiceProvider();
        }

        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
            if(IsInDesignMode)
            {
                serviceCollection.AddSingleton<IAuthService, FakeAuthService>();
                serviceCollection.AddSingleton<IGraphDataService, FakeGraphDataService>();
            }
            else
            {
                IAuthService authService = new AuthService(SecretConfig.ClientId);
                serviceCollection.AddSingleton(authService);
                serviceCollection.AddSingleton(authService.GetServiceClient());
                serviceCollection.AddSingleton<IGraphDataService, GraphDataService>();
            }

            serviceCollection.AddSingleton<MainViewModel>();
        }
    }
}