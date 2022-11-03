using AnimeSearch.Core.ViewsModel;
using Blazored.LocalStorage;
using BlazorTable.Components.ServerSide;
using BlazorTable.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace AnimeSearch.Site.Views.BlazorComponent;

public partial class BaseTable<TabType>
{
    [Inject]
    private ILocalStorageService LocalStorageService { get; set; }

    [Inject]
    private NavigationManager NavManager { get; set; }

    [Inject]
    private IJSRuntime JsRuntime { get; set; }

    [Parameter]
    public IEnumerable<TabType> Datas { get; set; }

    [Parameter]
    public IEnumerable<TabColumns<TabType>> Columns { get; set; }

    [Parameter]
    public Func<TabType, string> RowClassSelector { get; set; } = (a) => "";

    [Parameter]
    public Action<TabType> RowClick { get; set; } = (a) => { };
    
    [Parameter]
    public Action<TabType, MouseEventArgs> RightRowClickAction { get; set; }

    [Parameter]
    public Action<MouseEventArgs> Onclick { get; set; } = (a) => { };

    [Parameter]
    public int CurrentPage { get; set; } = 0;

    [Parameter] public List<TabType> ListSelected { get; set; } = null;
    
    [Parameter]
    public Action OnListChange { get; set; }
    
    [Parameter]
    public string EmptyDataText { get; set; }

    private DatasLoader Loader { get; set; }

    private ColOrder OrderInfo { get; set; }

    [CascadingParameter(Name = "Table")]
    public Table<TabType> ChildTable { get; set; }

    protected override void OnInitialized()
    {
        int currentPage = CurrentPage;

        if (currentPage == 0)
        {
            string pageStr = "?page=";

            if (NavManager.Uri.Contains(pageStr))
            {
                int start = Math.Max(NavManager.Uri.LastIndexOf(pageStr), 0) + pageStr.Length;

                if (int.TryParse(NavManager.Uri[start..], out int page))
                    currentPage = page;
            }
        }

        Loader = new(Datas, LocalStorageService, NavManager, JsRuntime, currentPage - 1);
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if(firstRender)
        {
            _ = LocalStorageService.ContainKeyAsync("colorder").AsTask().ContinueWith(res =>
            {
                if (res.Result)
                {
                    LocalStorageService.GetItemAsync<ColOrder>("colorder").AsTask().ContinueWith(t =>
                    {
                        OrderInfo = t.Result;

                        var col = ChildTable.Columns.FirstOrDefault(c => SiteUtils.GetPropertyMemberInfo(c.Field)?.Name == OrderInfo.ColName);

                        if (col != null && (col.SortDescending != OrderInfo.Desc || !col.SortColumn))
                        {
                            col.SortDescending = OrderInfo.Desc;
                            col.SortByAsync();
                        }
                    });
                }
            });
        }

        _ = ChildTable.SetPage(Loader.GetCurrentPage());

        base.OnAfterRender(firstRender);
    }

    private void ToggleAdd(TabType element)
    {
        if (ListSelected.Contains(element))
            ListSelected.Remove(element);
        else
            ListSelected.Add(element);
        
        OnListChange?.Invoke();
    }

    private class DatasLoader : IDataLoader<TabType>
    {
        private readonly IEnumerable<TabType> Datas;
        private readonly ILocalStorageService LocalStorageService;
        private readonly NavigationManager NavManager;
        private readonly IJSRuntime JsRuntime;

        private bool isFirst;

        private int currentPage;

        public DatasLoader(IEnumerable<TabType> datas, ILocalStorageService lss, NavigationManager navManager, IJSRuntime jsruntime, int currentPage)
        {
            Datas = datas;
            LocalStorageService = lss;
            NavManager = navManager;
            JsRuntime = jsruntime;

            isFirst = true;

            this.currentPage = currentPage;
        }

        public Task<PaginationResult<TabType>> LoadDataAsync(FilterData parameters) => Task.Run(() =>
        {
            IEnumerable<TabType> tmpDatas = Datas;

            if (parameters != null && !string.IsNullOrWhiteSpace(parameters.OrderBy))
            {
                var orders = parameters.OrderBy.Split(" ");

                if (orders.Length > 1)
                {
                    var type = typeof(TabType);
                    object func(TabType kv) => type.GetProperty(orders[0]).GetValue(kv, null);

                    bool desc = orders[1] == "desc";

                    tmpDatas = desc ? tmpDatas.OrderByDescending(func) : tmpDatas.OrderBy(func);

                    _ = LocalStorageService.SetItemAsync<ColOrder>("colorder", new() { ColName = orders[0], Desc = desc });
                }
            }

            string url = NavManager.Uri;

            if (url.Contains("?page="))
                url = url[..url.LastIndexOf("?page=")];

            int page = (parameters?.Skip ?? 0) / (parameters?.Top ?? 1);

            if (!isFirst)
            {
                currentPage = page;
                _ = JsRuntime.InvokeVoidAsync("ChangeUrl", $"{url}?page={page + 1}").AsTask();
            }
            else
            {
                parameters.Skip = currentPage * (parameters?.Top ?? 0);

                isFirst = false;
            }

            return new PaginationResult<TabType>()
            {
                Records = tmpDatas,
                Skip = parameters?.Skip ?? 0,
                Total = null,
                Top = parameters?.Top ?? 0
            };
        });

        internal int GetCurrentPage() => currentPage;
    }
    private class ColOrder
    {
        public string ColName { get; set; }
        public bool Desc { get; set; }
    }
}