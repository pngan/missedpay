using Microsoft.EntityFrameworkCore;
using missedpay.ApiService.Models;

public class MissedPayDbContext(DbContextOptions<MissedPayDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts { get; set; } = default!;
    public DbSet<Transaction> Transactions { get; set; } = default!;
}
