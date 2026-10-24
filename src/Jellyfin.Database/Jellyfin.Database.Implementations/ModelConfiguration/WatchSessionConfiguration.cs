using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration;

/// <summary>
/// WatchSession Configuration.
/// </summary>
public class WatchSessionConfiguration : IEntityTypeConfiguration<WatchSession>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<WatchSession> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.UserId, e.ItemId, e.DeviceId });
        builder.HasIndex(e => new { e.UserId, e.DateLastPlayed });
        builder.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Item).WithMany().HasForeignKey(e => e.ItemId).OnDelete(DeleteBehavior.Cascade);
    }
}
