using CargaOpiniones.Data.Context;
using CargaOpiniones.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;

namespace CargaOpiniones.Data.Context.Configurations
{
    public partial class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
    {
        public void Configure(EntityTypeBuilder<Cliente> entity)
        {
            entity.HasKey(e => e.IdCliente).HasName("PK_CLIENTES");

            entity.HasIndex(e => e.Email, "UQ__Clientes__A9D1053439DD80ED").IsUnique();

            entity.Property(e => e.IdCliente).ValueGeneratedNever();
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .IsRequired()
                .HasMaxLength(100)
                .IsUnicode(false);

            OnConfigurePartial(entity);
        }

        partial void OnConfigurePartial(EntityTypeBuilder<Cliente> entity);
    }
}
