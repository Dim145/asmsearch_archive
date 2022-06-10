using AnimeSearch.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace AnimeSearch.Database
{
    public class AsmsearchContext: DbContext
    {
        public AsmsearchContext(DbContextOptions<AsmsearchContext> options): base(options)
        {
        }

        public virtual DbSet<IP> IPs { get; set; }
        public virtual DbSet<Users> Users { get; set; }
        public virtual DbSet<Recherche> Recherches { get; set; }
        public virtual DbSet<Sites> Sites { get; set; }
        public virtual DbSet<Citations> Citations { get; set; }
        public virtual DbSet<TypeSite> TypeSites { get; set; }
        public virtual DbSet<Don> Dons { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Users>(entity =>
            {
                entity.HasKey(item => item.Id);

                entity.ToTable("Users");

                entity.Property(e => e.Id).ValueGeneratedOnAdd().HasColumnName("id");
                entity.Property(e => e.Name).IsRequired().HasColumnName("Name");
                entity.Property(e => e.Navigateur).HasColumnName("Navigateur");
                entity.Property(e => e.Derniere_visite).HasColumnName("Derniere_visite");
                entity.Property(e => e.Dernier_Acces_Admin).HasColumnName("Dernier_Acces_Admin");
            });

            modelBuilder.Entity<Recherche>(entity =>
            {
                entity.HasKey(item => item.Id);

                entity.ToTable("Recherche");

                entity.Property(e => e.Id).ValueGeneratedOnAdd().HasColumnName("id");
                entity.Property(e => e.User_ID).HasColumnName("User_ID");
                entity.Property(e => e.Nb_recherches).HasColumnName("nb_recherche");
                entity.Property(e => e.Derniere_Recherche).HasColumnName("derniere_recherche");

                entity.Property(e => e.recherche)
                    .IsRequired()
                    .HasColumnName("recherche");


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
                entity.Property(e => e.Derniere_utilisation).HasColumnName("Derniere_utilisation");

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
        }
    }
}
