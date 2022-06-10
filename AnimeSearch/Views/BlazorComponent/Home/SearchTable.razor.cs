using AnimeSearch.Controllers.api;
using AnimeSearch.Database;
using AnimeSearch.Models;
using BlazorTable.Components.ServerSide;
using BlazorTable.Interfaces;
using CurrieTechnologies.Razor.SweetAlert2;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnimeSearch.Core;
using AnimeSearch.Models.Search;

namespace AnimeSearch.Views.BlazorComponent.Home
{
    public partial class SearchTable
    {
        [Inject]
        private AsmsearchContext Database { get; set; }

        [Inject]
        private IHttpContextAccessor HttpContext { get; set; }

        [Inject]
        private SweetAlertService Swal { get; set; }

        [Parameter]
        public string Search { get; set; }

        [Parameter]
        public string Type { get => type.ToString(); set => type = value == "tv" ? 0 : value == "movie" ? 1 : value == "tvmovie" ? 2 : -1; }

        [Parameter]
        public int Id { get; set; }

        [Parameter]
        public string InfosLink { get; set; }

        [Parameter]
        public string BaLink { get; set; }

        [Parameter]
        public string UserName { get; set; }

        [Parameter]
        public bool IsSaved { get; set; } = false;

        [Parameter]
        public int NA { get; set; } = 0;

        [CascadingParameter(Name = "Table")]
        private Table<KeyValuePair<string, ModelSearchResult>> ChildTable { get; set; }

        private int type = 0;

        private ModelAPI Model { get; } = new();

        private Users CurrentUser { get; set; }

        private bool SaveAlreadyExist { get; set; } = false;

        private DataLoader Loader = null;

        private int NbResponses { get; set; } = 0;

        protected override async Task OnParametersSetAsync()
        {
            Loader = new(Search, Model, Database, HttpContext?.HttpContext?.Request?.Cookies, UserName, Id, type, IsSaved, (site) => { NbResponses++; InvokeAsync(StateHasChanged); });
            
            _ = Loader.LoadDataAsync(null).ContinueWith(t => InvokeAsync(StateHasChanged));

            await base.OnParametersSetAsync();
        }

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            if (ChildTable != null && Loader?.ChildTable == null)
                Loader.ChildTable = ChildTable;
        }

        protected override async Task OnInitializedAsync()
        {
            Model.InfoLink = InfosLink;
            Model.Bande_Annone = BaLink;

            if (!string.IsNullOrWhiteSpace(UserName) && UserName != Utilities.GUEST && CurrentUser == null)
                CurrentUser = Database.Users.FirstOrDefault(u => u.UserName == UserName);

            if (!IsSaved && CurrentUser != null && !SaveAlreadyExist)
            {
                SaveAlreadyExist = Database.SavedSearch.Any(ss => ss.Search == Search && ss.UserId == CurrentUser.Id);
            }

            await base.OnInitializedAsync();
        }

        private void Save()
        {
            Swal.FireAsync(new()
            {
                Title = SaveAlreadyExist ? "Mettre à jour la recherche ?" : "Sauvegarder la recherche ?",
                ConfirmButtonText = "Oui",
                ShowCancelButton = true,
                CancelButtonText = "Non",
                Icon = SweetAlertIcon.Question
            }).ContinueWith(task =>
            {
                var res = task.Result;

                if (res.IsConfirmed)
                {
                    DatabaseCom.SaveRechercheResult(Database, UserName, Model, SaveAlreadyExist).ContinueWith(t =>
                    {
                        var isSuccess = t.Result;

                        Swal.FireAsync(new()
                        {
                            ShowCancelButton = false,
                            ConfirmButtonText = "Ok",
                            Icon = isSuccess ? SweetAlertIcon.Success : SweetAlertIcon.Error,
                            Title = isSuccess ? SaveAlreadyExist ? "Mise à jour effectuée !" : "Sauvegardé !" : "Une erreur est survenue... "
                        });

                        if (isSuccess && !SaveAlreadyExist)
                        {
                            SaveAlreadyExist = true;

                            InvokeAsync(StateHasChanged);
                        }
                    });
                }
            });
        }

        private void DeleteSave()
        {
            Swal.FireAsync(new()
            {
                Title = "Supprimer la recherche sauvegardée ?",
                Icon = SweetAlertIcon.Question,
                ShowCancelButton = true,
                CancelButtonText = "Au final non.",
                ConfirmButtonText = "Ouais vas-y"
            }).ContinueWith(task =>
            {
                var res = task.Result;

                if (res.IsConfirmed)
                {
                    DatabaseCom.DeleteSave(Database, Search, CurrentUser.Id).ContinueWith(t =>
                    {
                        var isSuccess = t.Result > 0;

                        Swal.FireAsync(new()
                        {
                            Title = isSuccess ? "Success" : "Erreur",
                            Icon = isSuccess ? SweetAlertIcon.Success : SweetAlertIcon.Error,
                            Text = isSuccess ? "Suppression effectuer, vous pouvez naviguer sur la page mais plus la rechargée." : "Une erreur est survenue... Une double supression ?"
                        });
                    });
                }
            });
        }

        private class DataLoader : IDataLoader<KeyValuePair<string, ModelSearchResult>>
        {
            internal Table<KeyValuePair<string, ModelSearchResult>> ChildTable { get; set; }

            private readonly string Search;
            private readonly ModelAPI Model;
            private readonly AsmsearchContext _database;
            private readonly IRequestCookieCollection Cookies;
            private readonly int Id;
            private readonly int Type;
            private readonly bool IsSaved;
            private readonly string UserName; 
            private readonly Action<Search> CallBack;

            private bool isSearchLaunched;
            private bool isFinish;

            public DataLoader(string search, ModelAPI model, AsmsearchContext database, IRequestCookieCollection cookies, string userName, int id = 0, int type = -1, bool isSaved = false, Action<Search> callBack = null)
            {
                Model = model;
                Search = search;
                _database = database;
                Cookies = cookies;
                Id = id;
                Type = type;
                IsSaved = isSaved;
                UserName = userName;

                isSearchLaunched = isFinish = false;

                CallBack = (site) =>
                {
                    if (site.GetNbResult() > 0)
                    {
                        string url = null;

                        if (site is SearchGet)
                        {
                            string javascript = site.GetJavaScriptClickEvent();
                            int start = javascript.IndexOf("\"") + 1;

                            url = javascript[start..javascript.LastIndexOf("\"")];
                        }

                        Model.SearchResults.TryAdd(site.GetSiteTitle(), new()
                        {
                            IconUrl = site.GetUrlImageIcon(),
                            NbResults = site.GetNbResult(),
                            OpenJavaScript = site.GetJavaScriptClickEvent(),
                            SiteUrl = site.GetBaseURL(),
                            Type = site.GetTypeSite(),
                            Url = url
                        });
                    }

                    callBack?.Invoke(site);
                };
            }


            public Task<PaginationResult<KeyValuePair<string, ModelSearchResult>>> LoadDataAsync(FilterData parameters) => Task.Run(() =>
            {
                if (!isSearchLaunched)
                {
                    if (ChildTable != null)
                        ChildTable.LoadingInProgress = true;

                    isSearchLaunched = true;
                    ModelAPI model = null;

                    if (IsSaved)
                    {
                        Users user = _database.Users.FirstOrDefault(u => u.UserName == UserName);
                        SavedSearch ss = user != null ? _database.SavedSearch.AsNoTracking().FirstOrDefault(ss => ss.UserId == user.Id && ss.Search == Search) : null;

                        model = ss != null ? ss.Results : new();
                    }
                    else
                    {
                        model = !string.IsNullOrWhiteSpace(Search) ? SiteSearch.OptiSearch(Search, _database, Cookies, _database.Sites.Where(s => s.Etat == EtatSite.VALIDER).ToList(), false, CallBack)
                                                : SiteSearch.Search(Id, Cookies, Type, _database.Sites.Where(s => s.Etat == EtatSite.VALIDER).ToList(), false, CallBack);
                    }

                    foreach (var msr in model.SearchResults)
                        Model.SearchResults.TryAdd(msr.Key, msr.Value);

                    Model.Result = model.Result;
                    Model.Search = model.Search;

                    isFinish = true;

                    if(ChildTable != null)
                        ChildTable.LoadingInProgress = false;
                }

                IEnumerable<KeyValuePair<string, ModelSearchResult>> tmpDatas = Model.SearchResults;

                if (parameters != null && !string.IsNullOrWhiteSpace(parameters.OrderBy))
                {
                    var orders = parameters.OrderBy.Split(" ");

                    if (orders.Length > 1)
                    {
                        var type = typeof(ModelSearchResult);
                        Func<KeyValuePair<string, ModelSearchResult>, object> func = orders[0] == "Key" ? (kv) => kv.Key : (kv) => type.GetProperty(orders[0]).GetValue(kv.Value, null);

                        tmpDatas = orders[1] == "desc" ? tmpDatas.OrderByDescending(func) : tmpDatas.OrderBy(func);
                    }
                }

                return new PaginationResult<KeyValuePair<string, ModelSearchResult>>()
                {
                    Records = isFinish || tmpDatas.Any() ? tmpDatas.Skip(parameters?.Skip ?? 0).Take(parameters?.Top ?? 0) : null,
                    Skip = parameters?.Skip ?? 0,
                    Total = Model.SearchResults.Count,
                    Top = parameters?.Top ?? 0
                };
            });

            public bool IsFinish() => isFinish;
        }
    }
}
