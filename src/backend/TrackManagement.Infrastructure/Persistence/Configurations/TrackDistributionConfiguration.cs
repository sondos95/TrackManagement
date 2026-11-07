using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrackManagement.Domain.Entities;

namespace TrackManagement.Infrastructure.Persistence.Configurations;

internal sealed class TrackDistributionConfiguration : IEntityTypeConfiguration<TrackDistribution>
{
    public void Configure(EntityTypeBuilder<TrackDistribution> builder)
    {
        builder.HasKey(distribution => distribution.Id);
        builder.Property(distribution => distribution.SubmittedAt).IsRequired();
        builder.Property(distribution => distribution.Status).HasConversion<string>().IsRequired().HasMaxLength(16);
        builder.HasIndex(distribution => new { distribution.TrackId, distribution.DspId }).IsUnique();

        builder.HasOne<Dsp>()
            .WithMany()
            .HasForeignKey(distribution => distribution.DspId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
