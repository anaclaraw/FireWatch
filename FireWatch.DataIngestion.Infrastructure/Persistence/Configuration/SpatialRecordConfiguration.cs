using FireWatch.DataIngestion.Application.Eventos;
using FireWatch.DataIngestion.Application.Interfaces;
using FireWatch.DataIngestion.Domain.Enums;
using FireWatch.DataIngestion.Domain.Exceptions;
using FireWatch.DataIngestion.Domain.Interfaces;
using FireWatch.DataIngestion.Domain.ValueObjects;
using FireWatch.DataIngestion.Domain.Entities;
using Microsoft.EntityFrameworkCore;


using FireWatch.DataIngestion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace FireWatch.DataIngestion.Infrastructure.Persistence.Configurations;

public class EspacialRecordConfiguration : IEntityTypeConfiguration<EspacialRecord>
{
    public void Configure(EntityTypeBuilder<EspacialRecord> builder)
    {
        builder.ToTable("spatial_records");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.OwnsOne(x => x.Coordinates, coords =>
        {
            coords.Property(c => c.Latitude)
                .HasColumnName("latitude")
                .HasPrecision(10, 7)
                .IsRequired();

            coords.Property(c => c.Longitude)
                .HasColumnName("longitude")
                .HasPrecision(10, 7)
                .IsRequired();
        });

        builder.Property(x => x.Source)
            .HasColumnName("source")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.SourceIdentifier)
            .HasColumnName("source_identifier")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Brightness)
            .HasColumnName("brightness")
            .HasPrecision(10, 4);

        builder.Property(x => x.Frp)
            .HasColumnName("frp")
            .HasPrecision(10, 4);

        builder.Property(x => x.Confidence)
            .HasColumnName("confidence")
            .HasPrecision(5, 2);

        builder.Property(x => x.ScanType)
            .HasColumnName("scan_type")
            .HasMaxLength(30);

        builder.Property(x => x.DayNight)
            .HasColumnName("day_night")
            .HasMaxLength(1);

        builder.Property(x => x.AcquiredAt)
            .HasColumnName("acquired_at")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.FailureReason)
            .HasColumnName("failure_reason")
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(x => x.Source).HasDatabaseName("idx_spatial_records_source");
        builder.HasIndex(x => x.AcquiredAt).HasDatabaseName("idx_spatial_records_acquired_at");
        builder.HasIndex(x => x.Status).HasDatabaseName("idx_spatial_records_status");
    }
}