using CargaOpiniones.Data.Context;
using CargaOpiniones.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;

namespace CargaOpiniones.Data.Context.Configurations
{
    public partial class FuentesDatoConfiguration : IEntityTypeConfiguration<FuentesDato>
    {
        public void Configure(EntityTypeBuilder<FuentesDato> entity)
        {
            entity.HasKey(e => e.IdFuente).HasName("PK_FUENTESDATOS");

            entity.Property(e => e.IdFuente)
                .HasMaxLength(10)
                .IsUnicode(false);

            OnConfigurePartial(entity);
        }

        partial void OnConfigurePartial(EntityTypeBuilder<FuentesDato> entity);
    }
}
