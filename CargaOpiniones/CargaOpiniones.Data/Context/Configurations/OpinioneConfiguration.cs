using CargaOpiniones.Data.Context;
using CargaOpiniones.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;

namespace CargaOpiniones.Data.Context.Configurations
{
    public partial class OpinioneConfiguration : IEntityTypeConfiguration<Opinione>
    {
        public void Configure(EntityTypeBuilder<Opinione> entity)
        {
            entity.HasKey(e => e.IdOpinion).HasName("PK_OPINIONES");

            entity.Property(e => e.IdOpinion).ValueGeneratedNever();
            entity.Property(e => e.Comentario)
                .IsRequired()
                .HasColumnType("text");
            entity.Property(e => e.IdFuente)
                .IsRequired()
                .HasMaxLength(10)
                .IsUnicode(false);

            OnConfigurePartial(entity);
        }

        partial void OnConfigurePartial(EntityTypeBuilder<Opinione> entity);
    }
}
