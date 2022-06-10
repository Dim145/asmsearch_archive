using AnimeSearch.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Drawing;
using AnimeSearch.Models.Results;
using AnimeSearch.Models.Search;

namespace AnimeSearch.Database
{
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
                    .HasDefaultValue(SearchSource.API);


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
                    .HasConversion(r => JsonConvert.SerializeObject(r), (s) => JsonConvert.DeserializeObject<ModelAPI>(s, new ModelAPIDeserialiser(s)));

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
                    .HasConversion(c => Utilities.ColorConverter.ConvertToInvariantString(c), c => (Utilities.ColorConverter.ConvertFromInvariantString(c) as Color?).GetValueOrDefault());
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
        }
    }
    public class ModelAPIDeserialiser : CustomCreationConverter<Result>
    {
        private readonly bool isTvMaze;

        public ModelAPIDeserialiser(string json)
        {
            isTvMaze = json.Contains("tvmaze") || !json.Contains("Adult");
        }

        public override Result Create(Type objectType)
        {
            return isTvMaze ? new TvMazeResult() : new TheMovieDbResult();
        }
    }
}
