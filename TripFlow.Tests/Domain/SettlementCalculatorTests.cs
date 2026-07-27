using FluentAssertions;
using TripFlow.Domain.Entities;
using TripFlow.Domain.Services;
using Xunit;

namespace TripFlow.Tests.Domain;

public class SettlementCalculatorTests
{
    private static Expense MakeExpense(Guid paidBy, decimal amount, params (Guid ParticipantId, decimal Share)[] splits)
    {
        var expense = new Expense
        {
            Description = "teste",
            Amount = amount,
            PaidByParticipantId = paidBy,
            ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        foreach (var (participantId, share) in splits)
        {
            expense.Splits.Add(new ExpenseSplit { ExpenseId = expense.Id, ParticipantId = participantId, ShareAmount = share });
        }

        return expense;
    }

    [Fact]
    public void CalculateBalances_DuasPessoasDividemIgual_CredorFicaComMetadeDoValor()
    {
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();

        var expense = MakeExpense(alice, 100m, (alice, 50m), (bob, 50m));

        var balances = SettlementCalculator.CalculateBalances([expense]);

        var aliceBalance = balances.Single(b => b.ParticipantId == alice);
        var bobBalance = balances.Single(b => b.ParticipantId == bob);

        aliceBalance.Net.Should().Be(50m);
        bobBalance.Net.Should().Be(-50m);
    }

    [Fact]
    public void Simplify_TresPessoas_GeraNoMinimoDeTransferencias()
    {
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();
        var carol = Guid.NewGuid();

        // Alice paga 90 dividido em 3 (30 cada); Bob paga 30 dividido em 3 (10 cada).
        var expenses = new[]
        {
            MakeExpense(alice, 90m, (alice, 30m), (bob, 30m), (carol, 30m)),
            MakeExpense(bob, 30m, (alice, 10m), (bob, 10m), (carol, 10m))
        };

        var balances = SettlementCalculator.CalculateBalances(expenses);
        var transfers = SettlementCalculator.Simplify(balances);

        // Alice pagou 90, deve 40 -> credora de 50. Bob pagou 30, deve 40 -> devedor de 10.
        // Carol nao pagou nada, deve 40 -> devedora de 40. Minimo = 2 transferencias.
        transfers.Should().HaveCount(2);
        transfers.Sum(t => t.Amount).Should().Be(50m);
        transfers.Should().OnlyContain(t => t.ToParticipantId == alice);
    }

    [Fact]
    public void Simplify_TodoMundoQuitado_NaoGeraTransferencia()
    {
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();

        var expense = MakeExpense(alice, 100m, (alice, 50m), (bob, 50m));
        var balances = SettlementCalculator.CalculateBalances([expense]);

        // Bob paga a propria parte de volta pra Alice - saldo zera.
        var settleUp = MakeExpense(bob, 50m, (alice, 50m));
        var allBalances = SettlementCalculator.CalculateBalances([expense, settleUp]);

        var transfers = SettlementCalculator.Simplify(allBalances);

        transfers.Should().BeEmpty();
    }

    [Fact]
    public void Simplify_SemDespesas_NaoGeraTransferencia()
    {
        var balances = SettlementCalculator.CalculateBalances([]);
        var transfers = SettlementCalculator.Simplify(balances);

        transfers.Should().BeEmpty();
    }
}
