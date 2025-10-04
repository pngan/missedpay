using Microsoft.EntityFrameworkCore;

public class MissedPayDbContext(DbContextOptions<MissedPayDbContext> options) : DbContext(options)
{
    public DbSet<missedpay.ApiService.Models.Account> Account { get; set; } = default!;
}
