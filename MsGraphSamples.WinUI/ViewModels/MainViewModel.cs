﻿using System.Diagnostics;
using System.Net;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Kiota.Abstractions;
using MsGraphSamples.Services;
using MsGraphSamples.WinUI.Helpers;
using System.Collections.Immutable;
using System.Reflection;

namespace MsGraphSamples.WinUI.ViewModels;

public partial class MainViewModel(
    IAuthService authService,
    IAsyncEnumerableGraphDataService graphDataService,
    IDialogService dialogService) : ObservableRecipient
{
    public ushort PageSize { get; set; } = 25;
    private readonly Stopwatch _stopWatch = new();
    public long ElapsedMs => _stopWatch.ElapsedMilliseconds;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIndeterminate))]
    private bool _isBusy = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIndeterminate))]
    private bool _isError = false;

    public bool IsIndeterminate => IsBusy || IsError;

    [ObservableProperty]
    private string? _userName;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LaunchGraphExplorerCommand))]
    [NotifyPropertyChangedFor(nameof(LastUrl))]
    [NotifyPropertyChangedFor(nameof(LastCount))]
    private AsyncLoadingCollection<DirectoryObject>? _directoryObjects;

    [ObservableProperty]
    private DirectoryObject? _selectedObject;

    public static IReadOnlyList<string> Entities => ["Users", "Groups", "Applications", "ServicePrincipals", "Devices"];

    [ObservableProperty]
    private string _selectedEntity = "Users";
    public string? LastUrl => graphDataService.LastUrl;
    public long? LastCount => graphDataService.LastCount;

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

    public async Task PageLoaded()
    {
        var user = await graphDataService.GetUserAsync(["displayName"]);
        UserName = user?.DisplayName;
        await Load();
    }

    [RelayCommand]
    private Task Load()
    {
        return IsBusyWrapper(SelectedEntity switch
        {
            //"Users" =>  _graphDataService.GetUsersInBatch(SplittedSelect, pageSize),
            "Users" => graphDataService.GetUsers(SplittedSelect, Filter, SplittedOrderBy, Search, PageSize),
            "Groups" => graphDataService.GetGroups(SplittedSelect, Filter, SplittedOrderBy, Search, PageSize),
            "Applications" => graphDataService.GetApplications(SplittedSelect, Filter, SplittedOrderBy, Search, PageSize),
            "ServicePrincipals" => graphDataService.GetServicePrincipals(SplittedSelect, Filter, SplittedOrderBy, Search, PageSize),
            "Devices" => graphDataService.GetDevices(SplittedSelect, Filter, SplittedOrderBy, Search, PageSize),
            _ => throw new NotImplementedException("Can't find selected entity")
        });
    }

    public Task DrillDown()
    {
        ArgumentNullException.ThrowIfNull(SelectedObject);

        OrderBy = null;
        Filter = null;
        Search = null;

        return IsBusyWrapper(SelectedEntity switch
        {
            "Users" => graphDataService.GetTransitiveMemberOfAsGroups(SelectedObject.Id!, SplittedSelect, PageSize),
            "Groups" => graphDataService.GetTransitiveMembersAsUsers(SelectedObject.Id!, SplittedSelect, PageSize),
            "Applications" => graphDataService.GetApplicationOwnersAsUsers(SelectedObject.Id!, SplittedSelect, PageSize),
            "ServicePrincipals" => graphDataService.GetServicePrincipalOwnersAsUsers(SelectedObject.Id!, SplittedSelect, PageSize),
            "Devices" => graphDataService.GetDeviceOwnersAsUsers(SelectedObject.Id!, SplittedSelect, PageSize),
            _ => throw new NotImplementedException("Can't find selected entity")
        });
    }

    public Task Sort(object sender, DataGridColumnEventArgs e)
    {
        OrderBy = e.Column.SortDirection == null || e.Column.SortDirection == DataGridSortDirection.Descending
            ? $"{e.Column.Header} asc"
            : $"{e.Column.Header} desc";

        return Load();
    }

    private bool CanLaunchGraphExplorer() => LastUrl is not null;
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
        App.Current.Exit();
    }

    private async Task IsBusyWrapper(IAsyncEnumerable<DirectoryObject> directoryObjects)
    {
        IsError = false;
        IsBusy = true;
        _stopWatch.Restart();

        try
        {
            // Sending message to generate DataGridColumns according to the selected properties
            await GetPropertiesAndSortDirection(directoryObjects);

            DirectoryObjects = new(directoryObjects, PageSize);

            // Trigger load due to bug https://github.com/CommunityToolkit/WindowsCommunityToolkit/issues/3584
            await DirectoryObjects.RefreshAsync();

            SelectedEntity = DirectoryObjects.FirstOrDefault() switch
            {
                User => "Users",
                Group => "Groups",
                Application => "Applications",
                ServicePrincipal => "ServicePrincipals",
                Device => "Devices",
                _ => SelectedEntity,
            };
        }
        catch (ODataError ex)
        {
            IsError = true;
            await dialogService.ShowAsync(ex.Error?.Code ?? "OData Error", ex.Error?.Message);
        }
        catch (ApiException ex)
        {
            IsError = true;
            await dialogService.ShowAsync(Enum.GetName((HttpStatusCode)ex.ResponseStatusCode)!, ex.Message);
        }
        finally
        {
            _stopWatch.Stop();
            OnPropertyChanged(nameof(ElapsedMs));
            IsBusy = false;
        }
    }

    private async Task GetPropertiesAndSortDirection(IAsyncEnumerable<DirectoryObject> directoryObjects)
    {
        var item = await directoryObjects.FirstOrDefaultAsync();
        if (item == null)
            return;

        var propertiesAndSortDirection = item.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .Where(p => p.In(SplittedSelect))
            .ToImmutableSortedDictionary(kv => kv, GetSortDirection);

        WeakReferenceMessenger.Default.Send(propertiesAndSortDirection);

        DataGridSortDirection? GetSortDirection(string propertyName)
        {
            var property = OrderBy?.Split(' ')[0];
            var direction = OrderBy?.Split(' ').ElementAtOrDefault(1) ?? "asc";

            if (propertyName.Equals(property, StringComparison.InvariantCultureIgnoreCase))
                return direction.Equals("asc", StringComparison.InvariantCultureIgnoreCase)
                    ? DataGridSortDirection.Ascending
                    : DataGridSortDirection.Descending;

            return null;
        }
    }
}
