using Microsoft.EntityFrameworkCore;
using TripFlow.Domain.Entities;

namespace TripFlow.Application.Abstractions;

/// <summary>
/// Superficie minima do DbContext que a Application enxerga - mantem a camada de casos de
/// uso livre de uma dependencia direta do EF Core/Npgsql, sem precisar de um repositorio
/// por entidade (que no fim das contas so vira um wrapper fino em cima do DbSet).
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<RevokedAccessToken> RevokedAccessTokens { get; }
    DbSet<Trip> Trips { get; }
    DbSet<TripParticipant> TripParticipants { get; }
    DbSet<Expense> Expenses { get; }
    DbSet<ExpenseSplit> ExpenseSplits { get; }
    DbSet<Budget> Budgets { get; }
    DbSet<ChecklistItem> ChecklistItems { get; }
    DbSet<ItineraryItem> ItineraryItems { get; }
    DbSet<Reservation> Reservations { get; }
    DbSet<Document> Documents { get; }
    DbSet<TwoFactorRecoveryCode> TwoFactorRecoveryCodes { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
