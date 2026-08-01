using Microsoft.EntityFrameworkCore;
using TripFlow.Application.Abstractions;
using TripFlow.Domain.Entities;

namespace TripFlow.Infrastructure.Data;

public class TripFlowDbContext : DbContext, IAppDbContext
{
    private readonly ISecretProtector _secretProtector;

    public TripFlowDbContext(DbContextOptions<TripFlowDbContext> options, ISecretProtector secretProtector) : base(options)
    {
        _secretProtector = secretProtector;
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
    public DbSet<TwoFactorRecoveryCode> TwoFactorRecoveryCodes => Set<TwoFactorRecoveryCode>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationMute> NotificationMutes => Set<NotificationMute>();
    public DbSet<ActivityLogEntry> ActivityLogEntries => Set<ActivityLogEntry>();
    public DbSet<SettlementRecord> SettlementRecords => Set<SettlementRecord>();
    public DbSet<TripMemory> TripMemories => Set<TripMemory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TripFlowDbContext).Assembly);

        // Criptografado em repouso, nao hasheado - o Otp.NET precisa do valor em claro de
        // volta pra calcular o codigo TOTP. A conversao fica aqui (nao numa IEntityTypeConfiguration
        // separada) porque precisa do ISecretProtector injetado, e configuration classes
        // sao instanciadas sem DI pelo ApplyConfigurationsFromAssembly.
        modelBuilder.Entity<User>().Property(u => u.TwoFactorSecret).HasConversion(
            plain => plain == null ? null : _secretProtector.Protect(plain),
            stored => stored == null ? null : _secretProtector.Unprotect(stored));

        base.OnModelCreating(modelBuilder);
    }
}
