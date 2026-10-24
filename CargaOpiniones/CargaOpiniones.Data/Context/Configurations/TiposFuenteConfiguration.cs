using CargaOpiniones.Data.Context;
using CargaOpiniones.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;

namespace CargaOpiniones.Data.Context.Configurations
{
    public partial class TiposFuenteConfiguration : IEntityTypeConfiguration<TiposFuente>
    {
        public void Configure(EntityTypeBuilder<TiposFuente> entity)
        {
            entity.HasKey(e => e.IdTipoFuente).HasName("PK_TIPOSFUENTES");

            entity.Property(e => e.IdTipoFuente).ValueGeneratedNever();
            entity.Property(e => e.Nombre)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false);

            OnConfigurePartial(entity);
        }

        partial void OnConfigurePartial(EntityTypeBuilder<TiposFuente> entity);
    }
}
