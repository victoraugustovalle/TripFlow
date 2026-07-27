using Microsoft.EntityFrameworkCore;
using TripFlow.Application.Abstractions;
using TripFlow.Domain.Entities;

namespace TripFlow.Infrastructure.Data;

public class TripFlowDbContext : DbContext, IAppDbContext
{
    public TripFlowDbContext(DbContextOptions<TripFlowDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<RevokedAccessToken> RevokedAccessTokens => Set<RevokedAccessToken>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TripParticipant> TripParticipants => Set<TripParticipant>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<ExpenseSplit> ExpenseSplits => Set<ExpenseSplit>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();
    public DbSet<ItineraryItem> ItineraryItems => Set<ItineraryItem>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Document> Documents => Set<Document>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TripFlowDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
