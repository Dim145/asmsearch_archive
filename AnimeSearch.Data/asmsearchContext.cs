using System.Drawing;
using AnimeSearch.Core;
using AnimeSearch.Core.Models;
using AnimeSearch.Core.Models.Api;
using AnimeSearch.Core.Models.Search;
using AnimeSearch.Core.ViewsModel;
using AnimeSearch.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimeSearch.Data;

public class AsmsearchContext: IdentityDbContext<Users,Roles, int>
{
    public AsmsearchContext(DbContextOptions<AsmsearchContext> options): base(options)
    {
    }

    public virtual DbSet<IP> IPs { get; set; }
    public virtual DbSet<Recherche> Recherches { get; set; }
    public virtual DbSet<Sites> Sites { get; set; }
    public virtual DbSet<Citations> Citations { get; set; }
    public virtual DbSet<TypeSite> TypeSites { get; set; }
    public virtual DbSet<Don> Dons { get; set; }
    public virtual DbSet<SavedSearch> SavedSearch { get; set; }
    public virtual DbSet<Setting> Settings { get; set; }
    public virtual DbSet<ApiObject> Apis { get; set; }
    public virtual DbSet<ApiSort> ApiSortTypes { get; set; }
    public virtual DbSet<ApiFilter> ApiFilterTypes { get; set; }
    public virtual DbSet<ApiObjectSort> ApiObjectSort { get; set; }
    public virtual DbSet<ApiObjectFilter> ApiObjectFilter { get; set; }
    public virtual DbSet<Genre> Genres { get; set; }
    public virtual DbSet<Domains> Domains { get; set; }
    public virtual DbSet<EpisodesUrls> EpisodesUrls { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Users>(entity =>
        {
            entity.HasKey(item => item.Id);

            entity.ToTable("Users");

            entity.Property(e => e.Id).ValueGeneratedOnAdd().HasColumnName("id");
            entity.Property(e => e.UserName).IsRequired().HasColumnName("Name");
            entity.Property(e => e.Navigateur).HasColumnName("Navigateur");
            entity.Property(e => e.Derniere_visite).HasDefaultValueSql("SYSDATETIME()").HasColumnName("Derniere_visite");
            entity.Property(e => e.Dernier_Acces_Admin).HasColumnName("Dernier_Acces_Admin");
        });

        modelBuilder.Entity<IdentityUserLogin<int>>(entity =>
        {
            entity.HasKey(e => e.ProviderKey);
        });

        modelBuilder.Entity<IdentityUserRole<int>>(entity =>
        {
            entity.HasKey("UserId", "RoleId");
        });

        modelBuilder.Entity<IdentityUserToken<int>>(entity =>
        {
            entity.HasKey("LoginProvider", "UserId");
        });

        modelBuilder.Entity<Recherche>(entity =>
        {
            entity.HasKey(item => item.Id);

            entity.ToTable("Recherche");

            entity.Property(e => e.Id).ValueGeneratedOnAdd().HasColumnName("id");
            entity.Property(e => e.User_ID).HasColumnName("User_ID");
            entity.Property(e => e.Nb_recherches).HasColumnName("nb_recherche");
            entity.Property(e => e.Derniere_Recherche).HasDefaultValueSql("SYSDATETIME()").HasColumnName("derniere_recherche");

            entity.Property(e => e.recherche)
                .IsRequired()
                .HasColumnName("recherche");

            entity.Property(e => e.Source)
                .HasColumnName("Source")
                .HasDefaultValue(SearchSource.Api);


            entity.HasOne(r => r.User)
                .WithMany(u => u.Recherches)
                .HasForeignKey(r => r.User_ID)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Recherche_Users");
        });

        modelBuilder.Entity<IP>(entity =>
        {
            entity.HasKey(item => item.Id);

            entity.ToTable("IP");

            entity.Property(e => e.Id).ValueGeneratedOnAdd().HasColumnName("id");

            entity.Property(e => e.Adresse_IP)
                .IsRequired()
                .HasColumnName("Adresse_IP");

            entity.Property(e => e.Users_ID).HasColumnName("User_ID");
            entity.Property(e => e.Derniere_utilisation).HasDefaultValueSql("SYSDATETIME()").HasColumnName("Derniere_utilisation");

            entity.Property(e => e.Localisation).IsRequired(false).HasColumnName("Localisation");

            entity.HasOne(i => i.User)
                .WithMany(u => u.IPs)
                .HasForeignKey(i => i.Users_ID)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_IP_Users");
        });

        modelBuilder.Entity<Sites>(entity =>
        {
            entity.HasKey(e => e.Url);

            entity.Property(e => e.Url)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("url");

            entity.Property(e => e.CheminBaliseA)
                .IsRequired()
                .HasColumnName("cheminBaliseA");

            entity.Property(e => e.CheminToNbResult)
                .HasColumnName("cheminToNbResult");

            entity.Property(e => e.IdBase)
                .HasMaxLength(20)
                .HasColumnName("idBase");

            entity.Property(e => e.Etat).HasDefaultValue(EtatSite.NON_VALIDER).HasColumnName("etat");

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("title");

            entity.Property(e => e.TypeSite)
                .HasMaxLength(50)
                .HasColumnName("typeSite");

            entity.Property(e => e.UrlSearch)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("urlSearch");

            entity.Property(e => e.UrlIcon)
                .HasMaxLength(300)
                .HasColumnName("urlIcon");

            entity.Property(e => e.PostValues)
                .HasColumnName("postValues")
                .HasConversion(e => JsonConvert.SerializeObject(e), e => JsonConvert.DeserializeObject<Dictionary<string, string>>(e));

            entity.Property(e => e.Is_inter)
                .HasDefaultValue(false)
                .HasColumnName("is_internationnal");

            entity.Property(e => e.NbClick)
                .HasColumnName("nbClick");

            entity.HasCheckConstraint("url_check", "([url] like 'http%.[a-z][a-z]%/')");
        });

        modelBuilder.Entity<Citations>(entity =>
        {
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.AuthorName)
                .HasMaxLength(50)
                .HasColumnName("author_name")
                .IsRequired();

            entity.Property(e => e.UserId)
                .HasColumnName("User_ID");

            entity.Property(e => e.Contenue)
                .HasMaxLength(150)
                .HasColumnName("contenue")
                .IsRequired();

            entity.Property(e => e.DateAjout)
                .HasDefaultValueSql("SYSDATETIME()")
                .HasColumnName("date_ajout");

            entity.Property(e => e.IsValidated)
                .HasColumnName("is_validated")
                .HasDefaultValue(false);

            entity.Property(c => c.IsCurrent)
                .HasColumnName("is_current")
                .HasDefaultValue(false);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Citations)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Citations_Users");
        });

        modelBuilder.Entity<TypeSite>(entity =>
        {
            entity.Property(t => t.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("Type_Site");

            entity.Property(t => t.Name)
                .IsRequired()
                .HasColumnName("Name");

            entity.HasKey(t => t.Id);
        });

        modelBuilder.Entity<Don>(entity =>
        {
            entity.Property(d => d.Id)
                .HasColumnName("id");

            entity.HasKey(d => d.Id);

            entity.Property(d => d.Date)
                .HasDefaultValueSql("SYSDATETIME()")
                .HasColumnName("Date");

            entity.Property(d => d.Amout)
                .HasDefaultValue(0)
                .HasColumnName("amout");

            entity.Property(d => d.Done)
                .HasDefaultValue(false)
                .HasColumnName("done");

            entity.HasOne(d => d.User)
                .WithMany(u => u.Dons)
                .HasForeignKey(d => d.User_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Dons_Users");
        });

        modelBuilder.Entity<SavedSearch>(entity =>
        {
            entity.Property(ss => ss.Search).HasColumnName("SavedSearch");

            entity.Property(ss => ss.UserId).HasColumnName("User_id");

            entity.Property(ss => ss.DateSauvegarde).HasDefaultValueSql("SYSDATETIME()").HasColumnName("Date_Sauvegarde");

            entity.Property(ss => ss.Results)
                .HasColumnName("Resultats")
                .HasConversion(
                    r => JsonConvert.SerializeObject(r), (
                        s) => JsonConvert.DeserializeObject<ModelAPI>(s, new ResultJsonConvert()));

            entity.HasOne(ss => ss.User)
                .WithMany(u => u.SavedSearch)
                .HasForeignKey(ss => ss.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_SavedSearchs_Users");

            entity.HasKey(ss => new {ss.Search, ss.UserId});
        });

        modelBuilder.Entity<Roles>(entity =>
        {
            entity.Property(r => r.NiveauAutorisation)
                .HasColumnName("Niveau_Requis")
                .HasDefaultValue(1);

            entity.Property(r => r.Color)
                .HasColumnName("Color")
                .HasDefaultValue(Color.Empty)
                .HasConversion(c => CoreUtils.ColorConverter.ConvertToInvariantString(c), c => (CoreUtils.ColorConverter.ConvertFromInvariantString(c) as Color?).GetValueOrDefault());
        });

        modelBuilder.Entity<Setting>(entity =>
        {
            entity.Property(s => s.Name)
                .HasColumnName("Name")
                .IsRequired();

            entity.HasKey(s => s.Name);

            entity.Property(s => s.Description)
                .HasColumnName("Description")
                .HasDefaultValue(string.Empty)
                .IsRequired(false);

            entity.Property(s => s.TypeValue)
                .HasColumnName("Type")
                .HasDefaultValue(typeof(string).Name)
                .IsRequired();

            entity.Property(s => s.JsonValue)
                .HasColumnName("Json_Value")
                .IsRequired();

            entity.Property(s => s.IsDeletable)
                .HasColumnName("IsDeletable");
        });

        modelBuilder.Entity<ApiObject>(entity =>
        {
            entity.Property(a => a.Id).HasColumnName("id");
            entity.HasKey(a => a.Id);

            entity.Property(a => a.Name).HasColumnName("name").IsRequired();
            entity.Property(a => a.Description).HasColumnName("description").IsRequired();
            entity.Property(a => a.ApiUrl).HasColumnName("apiUrl").IsRequired();
            entity.Property(a => a.SiteUrl).HasColumnName("siteUrl");
            entity.Property(a => a.TokenName).HasColumnName("tokenName");
            entity.Property(a => a.Token).HasColumnName("token");
            entity.Property(a => a.SearchUrl).HasColumnName("searchUrl").IsRequired();
            entity.Property(a => a.AnimeSearchUrl).HasColumnName("animeSearchUrl");
            entity.Property(a => a.GlobalSearchUrl).HasColumnName("globalSearchUrl").IsRequired();
            entity.Property(a => a.MoviesSearchUrl).HasColumnName("moviesSearchUrl");
            entity.Property(a => a.TvSearchUrl).HasColumnName("tvSearchUrl");
            entity.Property(a => a.AnimeIdUrl).HasColumnName("animeIdUrl");
            entity.Property(a => a.MoviesIdUrl).HasColumnName("moviesIdUrl");
            entity.Property(a => a.TvIdUrl).HasColumnName("tvIdUrl");
            entity.Property(a => a.DiscoverUrl).HasColumnName("discoverUrl");
            entity.Property(a => a.PageName).HasColumnName("pageName");
            entity.Property(a => a.GenresMoviesUrl).HasColumnName("genresMoviesUrl");
            entity.Property(a => a.GenresTvUrl).HasColumnName("genresTvUrl");
            entity.Property(a => a.GenresPath).HasColumnName("genresPath");
            entity.Property(a => a.ImageBasePath).HasColumnName("imageBasePath");
            entity.Property(a => a.PathToResults).HasColumnName("pathToResults");
            entity.Property(a => a.OtherNamesUrl).HasColumnName("otherNamesUrl");
            entity.Property(a => a.SingleSearchUrl).HasColumnName("singleSearchUrl");
            entity.Property(a => a.PathToOnResults).HasColumnName("pathToOnResults");
            entity.Property(a => a.PathInOnResObject).HasColumnName("pathInOnResObject");
            entity.Property(a => a.LogoUrl).HasColumnName("iconUrl");

            entity.Property(a => a.TableFields)
                .HasConversion(tf => JsonConvert.SerializeObject(tf), 
                    tf => JsonConvert.DeserializeObject<Dictionary<string, string>>(tf))
                .HasColumnName("tableFields")
                .IsRequired();

            entity.HasMany(a => a.Filters)
                .WithOne(f => f.ApiObject)
                .HasForeignKey(f => f.IdApiObject);
            entity.HasMany(a => a.Sorts)
                .WithOne(s => s.ApiObject)
                .HasForeignKey(s => s.IdApiObject);
        });

        modelBuilder.Entity<ApiSort>(entity =>
        {
            entity.Property(s => s.Id).HasColumnName("id");
            entity.HasKey(s => s.Id);

            entity.Property(s => s.Label).HasColumnName("label");
        });
        
        modelBuilder.Entity<ApiFilter>(entity =>
        {
            entity.Property(s => s.Id).HasColumnName("id");
            entity.HasKey(s => s.Id);

            entity.Property(s => s.Label).HasColumnName("label");
        });

        modelBuilder.Entity<ApiObjectSort>(entity =>
        {
            entity.Property(os => os.IdApiObject).HasColumnName("idObject");
            entity.Property(os => os.IdApiSort).HasColumnName("idApiSort");

            entity.HasKey(os => new {os.IdApiObject, os.IdApiSort});

            entity.Property(os => os.FieldValue).HasColumnName("value");

            entity.HasOne(os => os.ApiSort)
                .WithMany()
                .HasForeignKey(s => s.IdApiSort);
        });
        
        modelBuilder.Entity<ApiObjectFilter>(entity =>
        {
            entity.Property(of => of.IdApiObject).HasColumnName("idObject");
            entity.Property(of => of.IdApiFilter).HasColumnName("idApiFilter");

            entity.HasKey(of => new {of.IdApiObject, of.IdApiFilter});

            entity.Property(of => of.FieldValue).HasColumnName("value");

            entity.HasOne(of => of.ApiFilter)
                .WithMany()
                .HasForeignKey(f => f.IdApiFilter);
        });

        modelBuilder.Entity<Genre>(entity =>
        {
            entity.Property(e => e.Name)
                .HasColumnName("name");

            entity.Property(e => e.Id)
                .HasColumnName("idInApi");

            entity.Property(e => e.Type)
                .HasColumnName("type")
                .HasDefaultValue(SearchType.All);

            entity.HasKey(e => new {e.ApiId, e.Id});

            entity.HasOne(e => e.Api);
        });

        modelBuilder.Entity<Domains>(entity =>
        {
            entity.Property(d => d.Url)
                .HasColumnName("url");
            entity.HasKey(d => d.Url);

            entity.Property(d => d.Description)
                .HasColumnName("desc");

            entity.Property(d => d.LastSeen)
                .HasColumnName("last_seen");
        });

        modelBuilder.Entity<EpisodesUrls>(entity =>
        {
            entity.Property(e => e.ApiId).HasColumnName("id_api").IsRequired();
            entity.Property(e => e.SearchId).HasColumnName("search_id").IsRequired();
            entity.Property(e => e.SeasonNumber).HasColumnName("season").IsRequired();
            entity.Property(e => e.EpisodeNumber).HasColumnName("episode").IsRequired();
            entity.Property(e => e.Url).HasColumnName("url").IsRequired();
            entity.Property(e => e.Valid).HasColumnName("is_valid").IsRequired().HasDefaultValue(true);

            entity.HasKey(e => new { IdApi = e.ApiId, e.SearchId, e.SeasonNumber, e.EpisodeNumber, e.Url });
        });
    }

    private sealed class ResultJsonConvert : JsonConverter<ModelAPI>
    {
        public override void WriteJson(JsonWriter writer, ModelAPI value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }

        /// <summary>
        /// Code temporaire qui sert de comptabilité pour les anciennes méthodes de sauvegardes.
        /// Destiné à disparaitre dans les prochaines versions.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="objectType"></param>
        /// <param name="existingValue"></param>
        /// <param name="hasExistingValue"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public override ModelAPI ReadJson(JsonReader reader, Type objectType, ModelAPI existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            ModelAPI finalResult = new();
                
            var tmp = serializer.Deserialize<Dictionary<string, JToken>>(reader);

            finalResult.Search = tmp.GetValueOrDefault("Search").Value<string>();
            finalResult.Bande_Annone = tmp.GetValueOrDefault("Bande_Annone")!.Value<string>();
            finalResult.InfoLink = tmp.GetValueOrDefault("InfoLink")!.Value<string>();
            var searchResults = tmp.GetValueOrDefault("SearchResults")!.ToObject<Dictionary<string, ModelSearchResult>>();

            if(searchResults != null)
                foreach (var kp in searchResults)
                    finalResult.SearchResults.Add(kp.Key, kp.Value);

            try
            {
                finalResult.Result = tmp.GetValueOrDefault("Result")!.ToObject<Result>();
            }
            catch (Exception)
            {
                tmp = tmp.GetValueOrDefault("Result")!.ToObject<Dictionary<string, JToken>>();
                var result = new Result();
                var resultType = result.GetType();
                    
                foreach (var kp in tmp)
                {
                    var key = kp.Key switch
                    {
                        "OthersName" => "OtherNames",
                        "Premiered" => "ReleaseDate",
                        _ => kp.Key
                    };
                        
                    var property = resultType.GetProperty(key);

                    if (property != null)
                    {
                        try
                        {
                            property.SetValue(result, kp.Value.ToObject(property.PropertyType));
                        }
                        catch (Exception)
                        {
                            if (kp.Value.Last is { } deepToken)
                            {
                                try
                                {
                                    property.SetValue(result, deepToken.ToObject(property.PropertyType));
                                }
                                catch (Exception e)
                                {
                                    CoreUtils.AddExceptionError($"La déserialisation de Result, pour le champs '{property.Name}'", e);
                                }
                            }
                        }
                    }
                }

                finalResult.Result = result.Id > 0 && !string.IsNullOrWhiteSpace(result.Name) && result.Url != null ? result : null;
            }

            return finalResult;
        }
    }
}