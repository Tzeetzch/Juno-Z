using Bunit;
using JunoBank.Domain.Entities;
using JunoBank.Domain.Enums;
using JunoBank.Web.Components.Shared;
using JunoBank.Web.Tests.Helpers;
using Moq;

namespace JunoBank.Web.Tests.Components.Shared;

public class TransactionListTests : ComponentTestBase
{
    [Fact]
    public void Render_EmptyList_ShowsEmptyMessage()
    {
        var cut = RenderComponent<TransactionList>();

        Assert.Contains("No transactions yet!", cut.Markup);
    }

    [Fact]
    public void Render_ShowsTransactionDescription()
    {
        var transactions = new List<Transaction>
        {
            new() { Description = "Weekly allowance", Amount = 5.00m, Type = TransactionType.Allowance, CreatedAt = DateTime.UtcNow }
        };

        var cut = RenderComponent<TransactionList>(p => p
            .Add(x => x.Transactions, transactions));

        Assert.Contains("Weekly allowance", cut.Markup);
    }

    [Fact]
    public void Render_Deposit_ShowsPlusPrefix()
    {
        var transactions = new List<Transaction>
        {
            new() { Amount = 5.00m, Type = TransactionType.Deposit, Description = "Test", CreatedAt = DateTime.UtcNow }
        };

        var cut = RenderComponent<TransactionList>(p => p
            .Add(x => x.Transactions, transactions));

        Assert.Contains("+€5.00", cut.Markup);
    }

    [Fact]
    public void Render_Withdrawal_ShowsMinusPrefix()
    {
        var transactions = new List<Transaction>
        {
            new() { Amount = 3.50m, Type = TransactionType.Withdrawal, Description = "Test", CreatedAt = DateTime.UtcNow }
        };

        var cut = RenderComponent<TransactionList>(p => p
            .Add(x => x.Transactions, transactions));

        Assert.Contains("-€3.50", cut.Markup);
    }

    [Fact]
    public void Render_UsesToLocalForDateDisplay()
    {
        var utcDate = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var localDate = new DateTime(2026, 1, 15, 11, 0, 0);
        MockBrowserTime.Setup(x => x.ToLocal(utcDate)).Returns(localDate);

        var transactions = new List<Transaction>
        {
            new() { Amount = 5.00m, Type = TransactionType.Deposit, Description = "Test", CreatedAt = utcDate }
        };

        var cut = RenderComponent<TransactionList>(p => p
            .Add(x => x.Transactions, transactions));

        Assert.Contains("Jan 15, 2026", cut.Markup);
        MockBrowserTime.Verify(x => x.ToLocal(utcDate), Times.Once);
    }

    [Theory]
    [InlineData(TransactionType.Deposit, "💰")]
    [InlineData(TransactionType.Withdrawal, "🛍️")]
    [InlineData(TransactionType.Allowance, "🎁")]
    public void Render_ShowsCorrectIcon(TransactionType type, string expectedIcon)
    {
        var transactions = new List<Transaction>
        {
            new() { Amount = 1.00m, Type = type, Description = "Test", CreatedAt = DateTime.UtcNow }
        };

        var cut = RenderComponent<TransactionList>(p => p
            .Add(x => x.Transactions, transactions));

        Assert.Contains(expectedIcon, cut.Markup);
    }
}
