using TripFlow.Domain.Entities;

namespace TripFlow.Domain.Services;

public record ParticipantBalance(Guid ParticipantId, decimal TotalPaid, decimal TotalOwed)
{
    public decimal Net => TotalPaid - TotalOwed;
}

public record SettlementTransfer(Guid FromParticipantId, Guid ToParticipantId, decimal Amount);

/// <summary>
/// Calcula quem deve quanto pra quem dentro de uma viagem e simplifica isso no menor
/// numero de transferencias possivel. Guloso (casa sempre o maior credor com o maior
/// devedor) - nao e garantido ser o otimo global (o problema e NP-dificil em geral),
/// mas e simples de entender, de testar e e a mesma abordagem que produtos reais usam.
/// </summary>
public static class SettlementCalculator
{
    private const decimal Epsilon = 0.01m;

    /// <summary>Confirmed settlements (opcional) sao quitacoes reais ja confirmadas pelo credor -
    /// entram na conta como se o devedor tivesse "pago" o valor e o credor "devesse" ele de
    /// volta, cancelando exatamente o debito original (mesma equivalencia usada no teste
    /// Simplify_TodoMundoQuitado, so que sem precisar criar um Expense fake pra isso).</summary>
    public static IReadOnlyList<ParticipantBalance> CalculateBalances(
        IEnumerable<Expense> expenses, IEnumerable<SettlementRecord>? confirmedSettlements = null)
    {
        var paid = new Dictionary<Guid, decimal>();
        var owed = new Dictionary<Guid, decimal>();

        foreach (var expense in expenses)
        {
            paid[expense.PaidByParticipantId] = paid.GetValueOrDefault(expense.PaidByParticipantId) + expense.Amount;

            foreach (var split in expense.Splits)
            {
                owed[split.ParticipantId] = owed.GetValueOrDefault(split.ParticipantId) + split.ShareAmount;
            }
        }

        foreach (var settlement in confirmedSettlements ?? [])
        {
            paid[settlement.FromParticipantId] = paid.GetValueOrDefault(settlement.FromParticipantId) + settlement.Amount;
            owed[settlement.ToParticipantId] = owed.GetValueOrDefault(settlement.ToParticipantId) + settlement.Amount;
        }

        var participantIds = paid.Keys.Union(owed.Keys).Distinct();

        return participantIds
            .Select(id => new ParticipantBalance(id, paid.GetValueOrDefault(id), owed.GetValueOrDefault(id)))
            .ToList();
    }

    public static IReadOnlyList<SettlementTransfer> Simplify(IReadOnlyList<ParticipantBalance> balances)
    {
        var creditors = new List<(Guid Id, decimal Amount)>();
        var debtors = new List<(Guid Id, decimal Amount)>();

        foreach (var balance in balances)
        {
            if (balance.Net > Epsilon)
                creditors.Add((balance.ParticipantId, balance.Net));
            else if (balance.Net < -Epsilon)
                debtors.Add((balance.ParticipantId, -balance.Net));
        }

        var transfers = new List<SettlementTransfer>();
        var creditorQueue = new List<(Guid Id, decimal Amount)>(creditors.OrderByDescending(c => c.Amount));
        var debtorQueue = new List<(Guid Id, decimal Amount)>(debtors.OrderByDescending(d => d.Amount));

        int i = 0, j = 0;
        while (i < creditorQueue.Count && j < debtorQueue.Count)
        {
            var (creditorId, creditorAmount) = creditorQueue[i];
            var (debtorId, debtorAmount) = debtorQueue[j];

            var transferAmount = Math.Min(creditorAmount, debtorAmount);
            transfers.Add(new SettlementTransfer(debtorId, creditorId, Math.Round(transferAmount, 2)));

            creditorAmount -= transferAmount;
            debtorAmount -= transferAmount;

            creditorQueue[i] = (creditorId, creditorAmount);
            debtorQueue[j] = (debtorId, debtorAmount);

            if (creditorAmount <= Epsilon) i++;
            if (debtorAmount <= Epsilon) j++;
        }

        return transfers;
    }
}
