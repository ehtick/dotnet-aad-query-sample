﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Kiota.Abstractions;
using MsGraphSamples.Services;

namespace MsGraphSamples.WPF.ViewModels;

public partial class MainViewModel(IAuthService authService, IGraphDataService graphDataService) : ObservableObject
{
    private readonly ushort pageSize = 25;
    private readonly Stopwatch _stopWatch = new();
    public long ElapsedMs => _stopWatch.ElapsedMilliseconds;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _userName;

    public string? LastUrl => graphDataService.LastUrl;

    public static IReadOnlyList<string> Entities => ["Users", "Groups", "Applications", "ServicePrincipals", "Devices"];

    [ObservableProperty]
    private string _selectedEntity = "Users";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DrillDownCommand))]
    private DirectoryObject? _selectedObject;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LaunchGraphExplorerCommand))]
    [NotifyCanExecuteChangedFor(nameof(LoadNextPageCommand))]
    [NotifyPropertyChangedFor(nameof(LastUrl))]
    private BaseCollectionPaginationCountResponse? _directoryObjects;

    #region OData Operators

    public string[] SplittedSelect => Select.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    [ObservableProperty]
    public string _select = "id,displayName,mail,userPrincipalName";

    [ObservableProperty]
    public string? _filter;

    public string[]? SplittedOrderBy => OrderBy?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    [ObservableProperty]
    public string? _orderBy;

    private string? _search;
    public string? Search
    {
        get => _search;
        set
        {
            if (_search != value)
            {
                _search = FixSearchSyntax(value);
                OnPropertyChanged();
            }
        }
    }

    private static string? FixSearchSyntax(string? searchValue)
    {
        if (searchValue == null)
            return null;

        if (searchValue.Contains('"'))
            return searchValue; // Assume already correctly formatted

        var elements = searchValue.Trim().Split(' ');
        var sb = new StringBuilder(elements.Length);

        foreach (var element in elements)
        {
            string? newElement;

            if (element.Contains(':'))
                newElement = $"\"{element}\""; // Search clause needs to be wrapped by double quotes
            else if (element.In("AND", "OR"))
                newElement = $" {element.ToUpperInvariant()} "; // [AND, OR] Operators need to be uppercase
            else
                newElement = element;

            sb.Append(newElement);
        }

        return sb.ToString();
    }

    #endregion

    [RelayCommand]
    public async Task PageLoaded()
    {
        var user = await graphDataService.GetUserAsync(["displayName"]);
        UserName = user?.DisplayName;

        await Load();
    }

    [RelayCommand]
    private async Task Load()
    {
        await IsBusyWrapper(async () => DirectoryObjects = SelectedEntity switch
        {
            "Users" => await graphDataService.GetUserCollectionAsync(SplittedSelect, Filter, SplittedOrderBy, Search, pageSize),
            "Groups" => await graphDataService.GetGroupCollectionAsync(SplittedSelect, Filter, SplittedOrderBy, Search, pageSize),
            "Applications" => await graphDataService.GetApplicationCollectionAsync(SplittedSelect, Filter, SplittedOrderBy, Search, pageSize),
            "ServicePrincipals" => await graphDataService.GetServicePrincipalCollectionAsync(SplittedSelect, Filter, SplittedOrderBy, Search, pageSize),
            "Devices" => await graphDataService.GetDeviceCollectionAsync(SplittedSelect, Filter, SplittedOrderBy, Search, pageSize),
            _ => throw new NotImplementedException("Can't find selected entity")
        });
    }

    private bool CanDrillDown() => SelectedObject is not null;
    [RelayCommand(CanExecute = nameof(CanDrillDown))]
    private async Task DrillDown()
    {
        ArgumentNullException.ThrowIfNull(SelectedObject);

        OrderBy = null;
        Filter = null;
        Search = null;

        await IsBusyWrapper(async () => DirectoryObjects = DirectoryObjects switch
        {
            UserCollectionResponse => await graphDataService.GetTransitiveMemberOfAsGroupCollectionAsync(SelectedObject.Id!, SplittedSelect, pageSize),
            GroupCollectionResponse => await graphDataService.GetTransitiveMembersAsUserCollectionAsync(SelectedObject.Id!, SplittedSelect, pageSize),
            ApplicationCollectionResponse => await graphDataService.GetApplicationOwnersAsUserCollectionAsync(SelectedObject.Id!, SplittedSelect, pageSize),
            ServicePrincipalCollectionResponse => await graphDataService.GetServicePrincipalOwnersAsUserCollectionAsync(SelectedObject.Id!, SplittedSelect, pageSize),
            DeviceCollectionResponse => await graphDataService.GetDeviceOwnersAsUserCollectionAsync(SelectedObject.Id!, SplittedSelect, pageSize),
            _ => throw new NotImplementedException("Can't find Entity Type")
        });
    }

    private bool CanGoNextPage => DirectoryObjects?.OdataNextLink is not null;
    [RelayCommand(CanExecute = nameof(CanGoNextPage))]
    private async Task LoadNextPage()
    {
        await IsBusyWrapper(async () => DirectoryObjects = DirectoryObjects switch
        {
            UserCollectionResponse userCollection => await graphDataService.GetNextPageAsync(userCollection),
            GroupCollectionResponse groupCollection => await graphDataService.GetNextPageAsync(groupCollection),
            ApplicationCollectionResponse applicationCollection => await graphDataService.GetNextPageAsync(applicationCollection),
            ServicePrincipalCollectionResponse servicePrincipalCollection => await graphDataService.GetNextPageAsync(servicePrincipalCollection),
            DeviceCollectionResponse deviceCollection => await graphDataService.GetNextPageAsync(deviceCollection),
            _ => throw new NotImplementedException("Can't find Entity Type")
        });
    }

    [RelayCommand]
    private Task Sort(DataGridSortingEventArgs e)
    {
        OrderBy = e.Column.SortDirection == null || e.Column.SortDirection == ListSortDirection.Descending
                ? $"{e.Column.Header} asc"
                : $"{e.Column.Header} desc";

        // Prevent client-side sorting
        e.Handled = true;

        return Load();
    }

    private bool CanLaunchGraphExplorer => LastUrl is not null;
    [RelayCommand(CanExecute = nameof(CanLaunchGraphExplorer))]
    private void LaunchGraphExplorer()
    {
        ArgumentNullException.ThrowIfNull(LastUrl);

        var geBaseUrl = "https://developer.microsoft.com/en-us/graph/graph-explorer";
        var graphUrl = "https://graph.microsoft.com";
        var version = "v1.0";
        var startOfQuery = LastUrl.NthIndexOf('/', 4) + 1;
        var encodedUrl = WebUtility.UrlEncode(LastUrl[startOfQuery..]);
        var encodedHeaders = "W3sibmFtZSI6IkNvbnNpc3RlbmN5TGV2ZWwiLCJ2YWx1ZSI6ImV2ZW50dWFsIn1d"; // ConsistencyLevel = eventual

        var url = $"{geBaseUrl}?request={encodedUrl}&method=GET&version={version}&GraphUrl={graphUrl}&headers={encodedHeaders}";

        var psi = new ProcessStartInfo { FileName = url, UseShellExecute = true };
        System.Diagnostics.Process.Start(psi);
    }

    [RelayCommand]
    private void Logout()
    {
        authService.Logout();
        App.Current.Shutdown();
    }

    private async Task IsBusyWrapper(Func<Task> loadOperation)
    {
        IsBusy = true;
        _stopWatch.Restart();

        try
        {
            await loadOperation();

            SelectedEntity = DirectoryObjects switch
            {
                UserCollectionResponse => "Users",
                GroupCollectionResponse => "Groups",
                ApplicationCollectionResponse => "Applications",
                ServicePrincipalCollectionResponse => "ServicePrincipals",
                DeviceCollectionResponse => "Devices",
                _ => SelectedEntity,
            };
        }
        catch (ODataError ex)
        {
            await ShowDialogAsync(ex.Error?.Code ?? "OData Error", ex.Error?.Message);
        }
        catch (ApiException ex)
        {
            await ShowDialogAsync(ex.Message, Enum.GetName((HttpStatusCode)ex.ResponseStatusCode));
        }
        finally
        {
            _stopWatch.Stop();
            OnPropertyChanged(nameof(ElapsedMs));
            IsBusy = false;
        }
    }

    /// <summary>
    /// Shows a content dialog
    /// </summary>
    /// <param name="text">The text of the content dialog</param>
    /// <param name="title">The title of the content dialog</param>
    public static Task ShowDialogAsync(string title, string? text)
    {
        return Task.Run(() => System.Windows.MessageBox.Show(text, title));
    }
}