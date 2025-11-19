using FestivalTicketsApp.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FestivalTicketsApp.Infrastructure.Data.Configs;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.HasKey(u => u.Id);
        
        builder.Property(u => u.Name)
            .HasMaxLength(DataSchemeConstants.DefaultNameLength);

        builder.Property(u => u.Surname)
            .HasMaxLength(DataSchemeConstants.DefaultNameLength);

        builder.Property(u => u.Phone)
            .HasMaxLength(DataSchemeConstants.DefaultUkrPhoneNumberLength);
        builder.ToTable(u =>
        {
            u.HasCheckConstraint("CK_Phone_Length",
                $"LEN([Phone]) = {DataSchemeConstants.DefaultUkrPhoneNumberLength}");
        });
        
        builder.HasIndex(u => u.Email)
            .IsUnique();
        builder.Property(u => u.Email)
            .IsRequired();
        
        builder.HasIndex(u => u.Phone)
            .IsUnique();
        builder.Property(u => u.Phone)
            .IsRequired();
        
        builder.HasIndex(u => u.Subject)
            .IsUnique();
        builder.Property(u => u.Subject)
            .IsRequired();

        builder.HasMany(u => u.FavouriteEvents)
            .WithMany(e => e.AddedToFavouriteBy);

        builder.HasMany(u => u.PurchasedTickets)
            .WithOne(t => t.Client)
            .HasForeignKey(t => t.ClientId)
            .IsRequired(false);
    }
}