using CargaOpiniones.Data.Context.Configurations;
using CargaOpiniones.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
#nullable disable

namespace CargaOpiniones.Data.Context;

public partial class AnalisisOpinionesdbContext : DbContext
{
    public AnalisisOpinionesdbContext(DbContextOptions<AnalisisOpinionesdbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Categoria> Categorias { get; set; }

    public virtual DbSet<Clasificacion> Clasificacions { get; set; }

    public virtual DbSet<Cliente> Clientes { get; set; }

    public virtual DbSet<FuentesDato> FuentesDatos { get; set; }

    public virtual DbSet<Opinione> Opiniones { get; set; }

    public virtual DbSet<Producto> Productos { get; set; }

    public virtual DbSet<SocialComment> SocialComments { get; set; }

    public virtual DbSet<Survey> Surveys { get; set; }

    public virtual DbSet<TiposFuente> TiposFuentes { get; set; }

    public virtual DbSet<WebReview> WebReviews { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new Configurations.CategoriaConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.ClasificacionConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.ClienteConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.FuentesDatoConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.OpinioneConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.ProductoConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.SocialCommentConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.SurveyConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.TiposFuenteConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.WebReviewConfiguration());

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
