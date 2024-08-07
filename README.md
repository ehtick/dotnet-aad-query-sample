---
uid: dotnet-aad-query-sample
description: Learn how to use .NET Graph SDK to query Directory Objects
page_type: sample
createdDate: 09/22/2020 00:00:00 AM
languages:
- csharp
technologies:
  - Microsoft Graph
  - Microsoft identity platform
authors:
- id: Licantrop0
  displayName: Luca Spolidoro
products:
- ms-graph
- dotnet-core
- windows-wpf
extensions:
  contentType: samples
  technologies: 
    - Microsoft Graph
    - Microsoft identity platform
  createdDate: 09/22/2020
codeUrl: https://github.com/microsoftgraph/dotnet-aad-query-sample
zipUrl: https://github.com/microsoftgraph/dotnet-aad-query-sample/archive/master.zip
description: "This sample demonstrates a .NET Desktop (WPF) application showcasing advanced Microsoft Graph Query Capabilities for Directory Objects with .NET"
---
# Explore advanced Microsoft Graph Query Capabilities on Microsoft Entra ID Objects with .NET SDK

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Registration](#registration)
  - [Step 1: Register your application](#step-1-register-your-application)
  - [Step 2: Set the MS Graph permissions](#step-2-set-the-ms-graph-permissions)
- [Setup](#setup)
  - [Step 1:  Clone or download this repository](#step-1--clone-or-download-this-repository)
  - [Step 2: Configure the ClientId using the Secret Manager](#step-2-configure-the-clientid-using-the-secret-manager)
- [Run the sample](#run-the-sample)
  - [On Visual Studio](#on-visual-studio)
  - [On Visual Studio Code](#on-visual-studio-code)
  - [Using the app](#using-the-app)
- [Code Architecture](#code-architecture)

## Overview

This sample helps you explore the Microsoft Graph's [new query capabilities](https://aka.ms/graph-docs/advanced-queries) of the identity APIs using the [Microsoft Graph .NET Client Library v5](https://github.com/microsoftgraph/msgraph-sdk-dotnet) to query Microsoft Entra ID.
The main code is in [AsyncEnumerableGraphDataService.cs](MsGraphSamples.Services/AsyncEnumerableGraphDataService.cs) file where, for every request:

- The required `$count=true` QueryString parameter is added
- The required `ConsistencyLevel=eventual` header is added
- The request URL is extracted and displayed in the UI
- The results are converted to an `IAsyncEnumerable` using the [`ToAsyncEnumerable`](MsGraphSamples.Services/AsyncEnumerableGraphDataService.cs#LL34C51-L34C68) extension method for an easier pagination.

## Prerequisites

- Either [Visual Studio (>v16.8)](https://aka.ms/vsdownload) *or* [Visual Studio Code](https://code.visualstudio.com/) with [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) and [C# for Visual Studio Code Extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)
- A Microsoft Entra ID tenant. For more information, see [How to get an Microsoft Entra ID tenant](https://azure.microsoft.com/documentation/articles/active-directory-howto-tenant/)
- A user account in your Microsoft Entra ID tenant. This sample will not work with a personal Microsoft account (formerly Windows Live account). Therefore, if you signed in to the [Azure portal](https://portal.azure.com) with a Microsoft account and have never created a user account in your directory before, you need to do that now.

## Registration

### Step 1: Register your application

Use the [Microsoft Application Registration Portal](https://aka.ms/appregistrations) to register your application with the Microsoft Graph APIs.  
Click New Registration.

![Application Registration](docs/register_app.png)
**Note:** Make sure to set the right **Redirect URI** (`http://localhost`) and application type is **Public client/native (mobile & desktop)**.

### Step 2: Set the MS Graph permissions

Add the [delegated permissions](https://docs.microsoft.com/graph/permissions-reference#delegated-permissions-20) for `Directory.Read.All`, and grant admin consent.  
We advise you to register and use this sample on a Dev/Test tenant and not on your production tenant.

![Api Permissions](docs/api_permissions.png)

## Setup

### Step 1:  Clone or download this repository

From your shell or command line:

```Shell
git clone https://github.com/microsoftgraph/dotnet-aad-query-sample.git
```

or download and extract the repository .zip file.

### Step 2: Configure the ClientId using the Secret Manager

This application use the [.NET Core Secret Manager](https://docs.microsoft.com/aspnet/core/security/app-secrets) to store the **ClientId**.  
To add the **ClientId** created on step 1 of registration:

1. Open a **Developer Command Prompt** or an **Integrated Terminal** and locate the `dotnet-aad-query-sample\MsGraphSamples.Services\` directory.
1. Type `dotnet user-secrets set "clientId" "<YOUR CLIENT ID>"`

## Run the sample

### On Visual Studio

Press F5. This will restore the missing nuget packages, build the solution and run the project.

### On Visual Studio Code

Shortly after you open the project folder in VS Code, a prompt by C# extension will appear on bottom right corner:  
`Required assets to build and debug are missing from 'dotnet-aad-query-sample'. Add them?`.  
Select **Yes** and the [C# Dev Kit extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) should install.  
Use The Solution Explorer to browse the projects (WinUI or WPF), right-click and debug:

![Solution Explorer](docs/solution_explorer.png)

> Note: Make sure to select `x64` Platform in the lower left corner of the window if you need to build WinUI, as `AnyCPU` is not supported.

### Using the app

If everything was configured correctly, you should be able to see the login prompt opening in a web browser.  
The auth token will be cached in a file for the subsequent runs thanks to [GetBrowserCredential](MsGraphSamples.Services/AuthService.cs#L41C42-L41C62) Method.  
You can query your tenant by typing the arguments of the standard OData `$select`, `$filter`, `$orderBy`, `$search` clauses in the relative text boxes.  
In the screenshot below you can see the $search operator in action:

![Screenshot of the App](docs/app1.png)

- If you double click on a row, a default drill-down will happen (for example by showing the list of transitive groups a user is part of).
- If you click on a header, the results will be sorted by that column. **Note: not all columns are supported and you may receive an error**.
- If any query error happen, it will displayed with a Message box.

The generated URL will appear in the readonly Url textbox. You can click the Graph Explorer button to open the current query in Graph Explorer.

## Code Architecture

This app provides a good starting point for enterprise desktop applications that connects to Microsoft Graph.  
The uses [MVVM](https://docs.microsoft.com/windows/uwp/data-binding/data-binding-and-mvvm) pattern and [.NET Community Toolkit](https://github.com/CommunityToolkit/dotnet).  
There are two UI projects, one for [WPF](MsGraphSamples.WPF/) and one for [WinUI](MsGraphSamples.WinUI/). The WinUI project implements an advanced technique to iterate all pages of a response using [IAsyncEnumerable](https://learn.microsoft.com/en-us/archive/msdn-magazine/2019/november/csharp-iterating-with-async-enumerables-in-csharp-8) and [ISupportIncrementalLoading](https://learn.microsoft.com/uwp/api/windows.ui.xaml.data.isupportincrementalloading).  
Dependency Injection is implemented using [Microsoft.Extensions.DependencyInjection](https://docs.microsoft.com/aspnet/core/fundamentals/dependency-injection).  
**Nullable** and **Code Analysis** are enabled to enforce code quality.
