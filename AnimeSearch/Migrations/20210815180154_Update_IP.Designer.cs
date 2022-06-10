﻿// <auto-generated />
using System;
using AnimeSearch.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AnimeSearch.Migrations
{
    [DbContext(typeof(AsmsearchContext))]
    [Migration("20210815180154_Update_IP")]
    partial class Update_IP
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .UseIdentityColumns()
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("ProductVersion", "5.0.1");

            modelBuilder.Entity("AnimeSearch.Database.Citations", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id")
                        .UseIdentityColumn();

                    b.Property<string>("AuthorName")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)")
                        .HasColumnName("author_name");

                    b.Property<string>("Contenue")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)")
                        .HasColumnName("contenue");

                    b.Property<DateTime?>("DateAjout")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValue(new DateTime(2021, 8, 15, 20, 1, 53, 861, DateTimeKind.Local).AddTicks(9252))
                        .HasColumnName("date_ajout");

                    b.Property<bool>("IsValidated")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false)
                        .HasColumnName("is_validated");

                    b.Property<int?>("UserId")
                        .HasColumnType("int")
                        .HasColumnName("User_ID");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Citations");
                });

            modelBuilder.Entity("AnimeSearch.Database.IP", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id")
                        .UseIdentityColumn();

                    b.Property<string>("Adresse_IP")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("Adresse_IP");

                    b.Property<DateTime?>("Derniere_utilisation")
                        .HasColumnType("datetime2")
                        .HasColumnName("Derniere_utilisation");

                    b.Property<string>("Localisation")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("Localisation");

                    b.Property<int>("Users_ID")
                        .HasColumnType("int")
                        .HasColumnName("User_ID");

                    b.HasKey("Id");

                    b.HasIndex("Users_ID");

                    b.ToTable("IP");
                });

            modelBuilder.Entity("AnimeSearch.Database.Recherche", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id")
                        .UseIdentityColumn();

                    b.Property<DateTime?>("Derniere_Recherche")
                        .HasColumnType("datetime2")
                        .HasColumnName("derniere_recherche");

                    b.Property<int>("Nb_recherches")
                        .HasColumnType("int")
                        .HasColumnName("nb_recherche");

                    b.Property<int>("User_ID")
                        .HasColumnType("int")
                        .HasColumnName("User_ID");

                    b.Property<string>("recherche")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("recherche");

                    b.HasKey("Id");

                    b.HasIndex("User_ID");

                    b.ToTable("Recherche");
                });

            modelBuilder.Entity("AnimeSearch.Database.Sites", b =>
                {
                    b.Property<string>("Url")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("url");

                    b.Property<string>("CheminBaliseA")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("cheminBaliseA");

                    b.Property<string>("IdBase")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)")
                        .HasColumnName("idBase");

                    b.Property<bool>("IsValidated")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false)
                        .HasColumnName("isValidated");

                    b.Property<bool>("Is_inter")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false)
                        .HasColumnName("is_internationnal");

                    b.Property<string>("PostValues")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("postValues");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)")
                        .HasColumnName("title");

                    b.Property<string>("TypeSite")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)")
                        .HasColumnName("typeSite");

                    b.Property<string>("UrlIcon")
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)")
                        .HasColumnName("urlIcon");

                    b.Property<string>("UrlSearch")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)")
                        .HasColumnName("urlSearch");

                    b.HasKey("Url");

                    b.ToTable("Sites");

                    b.HasCheckConstraint("url_check", "([url] like 'http%.[a-z][a-z]%/')");
                });

            modelBuilder.Entity("AnimeSearch.Database.Users", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id")
                        .UseIdentityColumn();

                    b.Property<DateTime?>("Derniere_visite")
                        .HasColumnType("datetime2")
                        .HasColumnName("Derniere_visite");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("Name");

                    b.Property<string>("Navigateur")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("Navigateur");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("AnimeSearch.Database.Citations", b =>
                {
                    b.HasOne("AnimeSearch.Database.Users", "User")
                        .WithMany("Citations")
                        .HasForeignKey("UserId")
                        .HasConstraintName("FK_Citations_Users");

                    b.Navigation("User");
                });

            modelBuilder.Entity("AnimeSearch.Database.IP", b =>
                {
                    b.HasOne("AnimeSearch.Database.Users", "User")
                        .WithMany("IPs")
                        .HasForeignKey("Users_ID")
                        .HasConstraintName("FK_IP_Users")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("AnimeSearch.Database.Recherche", b =>
                {
                    b.HasOne("AnimeSearch.Database.Users", "User")
                        .WithMany("Recherches")
                        .HasForeignKey("User_ID")
                        .HasConstraintName("FK_Recherche_Users")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("AnimeSearch.Database.Users", b =>
                {
                    b.Navigation("Citations");

                    b.Navigation("IPs");

                    b.Navigation("Recherches");
                });
#pragma warning restore 612, 618
        }
    }
}
