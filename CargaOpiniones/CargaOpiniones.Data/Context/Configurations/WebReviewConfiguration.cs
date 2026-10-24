using CargaOpiniones.Data.Context;
using CargaOpiniones.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;

namespace CargaOpiniones.Data.Context.Configurations
{
    public partial class WebReviewConfiguration : IEntityTypeConfiguration<WebReview>
    {
        public void Configure(EntityTypeBuilder<WebReview> entity)
        {
            entity.HasKey(e => e.IdOpinion).HasName("PK_WEBREVIEWS");

            entity.Property(e => e.IdOpinion).ValueGeneratedNever();

            OnConfigurePartial(entity);
        }

        partial void OnConfigurePartial(EntityTypeBuilder<WebReview> entity);
    }
}
