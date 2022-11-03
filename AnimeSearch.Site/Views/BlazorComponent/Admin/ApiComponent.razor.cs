using AnimeSearch.Core.Models.Api;
using AnimeSearch.Data;
using AnimeSearch.Data.Models;
using AnimeSearch.Services.Database;
using CurrieTechnologies.Razor.SweetAlert2;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace AnimeSearch.Site.Views.BlazorComponent.Admin;

public partial class ApiComponent
{
    [Inject] private SweetAlertService Swal { get; set; }
    [Inject] private AsmsearchContext Database { get; set; }
    [Inject] private NavigationManager NavManager { get; set; }
    [Inject] private DatasUtilsService DatasUtilsService { get; set; }

    [Parameter] public int ApiId { get; set; } = -1;
    
    private ApiObject Api { get; set; }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        Api = Database.Apis.AsNoTracking()
            .Include(a => a.Sorts)
            .ThenInclude(s => s.ApiSort)
            .Include(a => a.Filters)
            .ThenInclude(f => f.ApiFilter)
            .FirstOrDefault(a => a.Id == ApiId) ?? new();

        Filters = Database.ApiFilterTypes.ToList();
        Sorts = Database.ApiSortTypes.ToList();
    }

    private void OnFinish()
    {
        Swal.ShowLoadingAsync();

        try
        {
            var sorts = Api.Sorts;
            var filters = Api.Filters;
            
            if (Api.Id > 0)
            {
                Api.Sorts = null;
                Api.Filters = null;
            }
            
            if (!Task.Run(async () => await DatasUtilsService.UpsertApi(Api)).GetAwaiter().GetResult())
                throw new Exception("L'api n'as pus être sauvegardée, veuillez vérifier les données");
            
            if (Api.Id > 0)
            {
                var state = Database.Apis.Update(Api);
                #region SortUpdate

                var newSortList = sorts.ToList();
                var sortList = Database.ApiObjectSort.Where(s => s.IdApiObject == Api.Id).ToList();
                
                sortList.ForEach(s =>
                {
                    if (newSortList.Find(sort => sort.IdApiSort == s.IdApiSort) is { } newSortFinded)
                    {
                        if (s.FieldValue != newSortFinded.FieldValue)
                            s.FieldValue = newSortFinded.FieldValue;

                        newSortList.Remove(newSortFinded);
                    }
                    else
                    {
                        Database.ApiObjectSort.Remove(s);
                    }
                });

                if(newSortList.Any())
                    Database.ApiObjectSort.AddRange(newSortList);

                #endregion

                #region FiltersUpdate

                var newFilterList = filters.ToList();
                var filterList = Database.ApiObjectFilter.Where(s => s.IdApiObject == Api.Id).ToList();
                
                filterList.ForEach(s =>
                {
                    if (newFilterList.Find(sort => sort.IdApiFilter == s.IdApiFilter) is { } newFilterFinded)
                    {
                        if (s.FieldValue != newFilterFinded.FieldValue)
                            s.FieldValue = newFilterFinded.FieldValue;

                        newFilterList.Remove(newFilterFinded);
                    }
                    else
                    {
                        Database.ApiObjectFilter.Remove(s);
                    }
                });
                
                if(newFilterList.Any())
                    Database.ApiObjectFilter.AddRange(newFilterList);

                #endregion
                
                Database.SaveChanges();
                
                state.State = EntityState.Detached;
                Api.Sorts = sorts;
                Api.Filters = filters;
            }

            Swal.FireAsync(new()
            {
                Icon = SweetAlertIcon.Info,
                TitleText = "Sauvegarde effectué"
            }).ContinueWith(res =>
            {
                if(res.IsCompletedSuccessfully)
                    NavManager.NavigateTo("/admin/apis", true);
            });
        }
        catch (Exception e)
        {
            Swal.FireAsync(new()
            {
                Icon = SweetAlertIcon.Error,
                TitleText = "Une erreur est survenue",
                Html = $"<p>{e.Message}</p><div>{e.StackTrace?.Replace("\n", "<br/>")}</div>"
            });
        }
    }

    #region ValidateStepMethods

    private string ValidateBasInfos()
    {
        bool Condition(string s, string s2 = null)
        {
            if (s != null && s2 != null) return string.IsNullOrWhiteSpace(s) != string.IsNullOrWhiteSpace(s2);

            return string.IsNullOrWhiteSpace(s);
        }

        var tmpTab = new[]
        {
            new { value = 1 , isInvalid = Condition(Api.Name), message = "Le nom est requis" },
            new { value = 2 , isInvalid = Condition(Api.SiteUrl), message = "L'adresse du site de l'api est requise" },
            new { value = 4 , isInvalid = Condition(Api.ApiUrl), message = "L'adresse de l'api est requise (évidemment)" },
            new { value = 8, isInvalid = Condition(Api.Token, Api.TokenName), message = "Si le nom du token est remplis, le token doit l'être et inversement." },
            new { value = 16, isInvalid = Condition(Api.Description), message = "Une description est requise" }
        };

        return tmpTab.Where(t => t.isInvalid).Aggregate("", (current, tmp) => current + (tmp.message + "\n"));
    }

    private string ValidateSearchUrls()
    {
        var message = string.Empty;
        
        if (string.IsNullOrWhiteSpace(Api.SearchUrl))
            message += "L'url de recherche de base doit être remplie.";

        if (new[] {Api.GlobalSearchUrl, Api.AnimeSearchUrl, Api.MoviesSearchUrl, Api.TvSearchUrl}.All(string.IsNullOrWhiteSpace))
            message += "\nAu moins une des urls de recherche est obligatoire.";
        
        if (new[] {Api.AnimeIdUrl, Api.MoviesIdUrl, Api.AnimeIdUrl}.All(string.IsNullOrWhiteSpace))
            message += "\nAu moins une des urls d'id est requise";

        return message;
    }

    private string ValidateJsonStep()
    {
        if (!string.IsNullOrWhiteSpace(Api.OtherNamesUrl) && Api.PathInOnResObject?.Count(c => c == '|') != 1)
            return "Il faut 1 '|' por différencier les deux champs d'un objet représentant un autre nom.";

        return string.Empty;
    }

    private string ValidatePropertiesResult()
    {
        if (Api.TableFields?.Count == 0)
            return "Il faut renseignée des valeurs pour que le serveur puisse les faire correspondre à son modèle.";

        return string.Empty;
    }

    #endregion

    #region TableFieldCRUD
    
    private string newTableFieldKey;
    private string newTableFieldValue;
    
    private void ClickOnTableFieldTable(KeyValuePair<string, string> kv)
    {
        Swal.FireAsync(new()
            {
                Title = $"Voulez-vous supprimer {kv.Key} ?",
                Icon = SweetAlertIcon.Warning,
                ShowDenyButton = true,
                ConfirmButtonText = "Oui",
                DenyButtonText = "Non"
            })
            .ContinueWith(res =>
            {
                if (res.Result.IsConfirmed)
                {
                    Api.TableFields.Remove(kv.Key);
                    InvokeAsync(StateHasChanged);
                }
            });
    }
    
    private string[] TabFieldsValues { get; } =
    {
        "id",
        "name",
        "language",
        "other_name",
        "type",
        "release_date",
        "description",
        "popularity",
        "image",
        "genres",
        "genre_ids",
        "url",
        "status",
        "18+"
    };

    private void AddTableField()
    {
        if (string.IsNullOrWhiteSpace(newTableFieldKey) || !TabFieldsValues.Contains(newTableFieldValue) || !Api.TableFields.TryAdd(newTableFieldKey, newTableFieldValue))
        {
            Swal.FireAsync(new()
            {
                Icon = SweetAlertIcon.Error,
                TitleText = "Le champs est vide, existe déjà ou la correspondance est invalide."
            });
        }
        else
        {
            newTableFieldKey = newTableFieldValue = string.Empty;
            InvokeAsync(StateHasChanged);
        }
    }
    
    #endregion

    #region FiltersCRUD

    private List<ApiFilter> Filters { get; set; }
    
    private string newFilterValue;
    private int filterId;

    private void ClickOnTableFilters(ApiObjectFilter filter)
    {
        Swal.FireAsync(new()
            {
                Title = $"Voulez-vous supprimer {filter.FieldValue} ?",
                Icon = SweetAlertIcon.Warning,
                ShowDenyButton = true,
                ConfirmButtonText = "Oui",
                DenyButtonText = "Non"
            })
            .ContinueWith(res =>
            {
                if (res.Result.IsConfirmed)
                {
                    if(Filters.All(f => f.Id != filter.IdApiFilter))
                        Filters.Add(filter.ApiFilter);
                    
                    Api.Filters.Remove(filter);
                    InvokeAsync(StateHasChanged);
                }
            });
    }

    private void AddFilter()
    {
        if (filterId == -1 || string.IsNullOrWhiteSpace(newFilterValue) || Api.Filters.Any(f => f.IdApiFilter == filterId))
        {
            Swal.FireAsync(new()
            {
                Icon = SweetAlertIcon.Error,
                TitleText = "valeur vide ou le filtre existe déjà"
            });
        }
        else
        {
            var newValue = new ApiObjectFilter
            {
                ApiObject = Api,
                ApiFilter = Filters.FirstOrDefault(f => f.Id == filterId),
                FieldValue = newFilterValue,
                IdApiFilter = filterId,
                IdApiObject = Api.Id
            };
            
            Api.Filters.Add(newValue);
            Filters.RemoveAt(Filters.FindIndex(f => f.Id == newValue.IdApiFilter));

            newFilterValue = string.Empty;
            filterId = -1;
            InvokeAsync(StateHasChanged);
        }
    }

    #endregion

    #region SortCRUD

    private List<ApiSort> Sorts { get; set; }
    private string newSortValue;
    private int sortId;

    private void ClickOnSortTable(ApiObjectSort sort)
    {
        Swal.FireAsync(new()
            {
                Title = $"Voulez-vous supprimer {sort.FieldValue} ?",
                Icon = SweetAlertIcon.Warning,
                ShowDenyButton = true,
                ConfirmButtonText = "Oui",
                DenyButtonText = "Non"
            })
            .ContinueWith(res =>
            {
                if (res.Result.IsConfirmed)
                {
                    if(Sorts.All(f => f.Id != sort.IdApiSort))
                        Sorts.Add(sort.ApiSort);
                    
                    Api.Sorts.Remove(sort);
                    InvokeAsync(StateHasChanged);
                }
            });
    }

    private void AddSort()
    {
        if (sortId == -1 || string.IsNullOrWhiteSpace(newSortValue) || Api.Sorts.Any(f => f.IdApiSort == sortId))
        {
            Swal.FireAsync(new()
            {
                Icon = SweetAlertIcon.Error,
                TitleText = "valeur vide ou le trie existe déjà"
            });
        }
        else
        {
            var newValue = new ApiObjectSort
            {
                ApiObject = Api,
                ApiSort = Sorts.FirstOrDefault(f => f.Id == sortId),
                FieldValue = newSortValue,
                IdApiSort = sortId,
                IdApiObject = Api.Id
            };
            
            Api.Sorts.Add(newValue);
            Sorts.RemoveAt(Sorts.FindIndex(f => f.Id == newValue.IdApiSort));

            newSortValue = string.Empty;
            sortId = -1;
            InvokeAsync(StateHasChanged);
        }
    }

    #endregion
}