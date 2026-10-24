using CargaOpiniones.Data.Context;
using CargaOpiniones.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;

namespace CargaOpiniones.Data.Context.Configurations
{
    public partial class SocialCommentConfiguration : IEntityTypeConfiguration<SocialComment>
    {
        public void Configure(EntityTypeBuilder<SocialComment> entity)
        {
            entity.HasKey(e => e.IdOpinion).HasName("PK_SOCIALCOMMENTS");

            entity.Property(e => e.IdOpinion).ValueGeneratedNever();

            OnConfigurePartial(entity);
        }

        partial void OnConfigurePartial(EntityTypeBuilder<SocialComment> entity);
    }
}
