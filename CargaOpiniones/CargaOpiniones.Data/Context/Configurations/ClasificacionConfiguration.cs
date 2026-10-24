using CargaOpiniones.Data.Context;
using CargaOpiniones.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;

namespace CargaOpiniones.Data.Context.Configurations
{
    public partial class ClasificacionConfiguration : IEntityTypeConfiguration<Clasificacion>
    {
        public void Configure(EntityTypeBuilder<Clasificacion> entity)
        {
            entity.HasKey(e => e.IdClasificacion).HasName("PK_CLASIFICACION");

            entity.ToTable("Clasificacion");

            entity.Property(e => e.IdClasificacion).ValueGeneratedNever();
            entity.Property(e => e.Nombre)
                .HasMaxLength(80)
                .IsUnicode(false);

            OnConfigurePartial(entity);
        }

        partial void OnConfigurePartial(EntityTypeBuilder<Clasificacion> entity);
    }
}
