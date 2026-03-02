using Bunit;
using JunoBank.Application.DTOs;
using JunoBank.Web.Components.Shared;
using JunoBank.Web.Tests.Helpers;

namespace JunoBank.Web.Tests.Components.Shared;

public class ChildCardTests : ComponentTestBase
{
    private static ChildSummary CreateChild(
        string name = "Junior", decimal balance = 10.50m, int pending = 0) =>
        new() { Id = 1, Name = name, Balance = balance, PendingRequestCount = pending };

    [Fact]
    public void Render_ShowsChildName()
    {
        var cut = RenderComponent<ChildCard>(p => p
            .Add(x => x.Child, CreateChild("Junior")));

        Assert.Contains("Junior", cut.Find(".child-name").TextContent);
    }

    [Fact]
    public void Render_ShowsFormattedBalance()
    {
        var cut = RenderComponent<ChildCard>(p => p
            .Add(x => x.Child, CreateChild(balance: 10.50m)));

        Assert.Contains("10.50", cut.Find(".balance-value").TextContent);
    }

    [Fact]
    public void Render_ShowsFirstLetterAsAvatar()
    {
        var cut = RenderComponent<ChildCard>(p => p
            .Add(x => x.Child, CreateChild("Sophie")));

        Assert.Equal("S", cut.Find(".child-avatar").TextContent);
    }

    [Fact]
    public void Render_WithPendingRequests_ShowsPendingBadge()
    {
        var cut = RenderComponent<ChildCard>(p => p
            .Add(x => x.Child, CreateChild(pending: 3)));

        var badge = cut.Find(".pending-badge");
        Assert.Contains("3 pending", badge.TextContent);
    }

    [Fact]
    public void Render_WithNoPendingRequests_HidesPendingBadge()
    {
        var cut = RenderComponent<ChildCard>(p => p
            .Add(x => x.Child, CreateChild(pending: 0)));

        Assert.Empty(cut.FindAll(".pending-badge"));
    }

    [Fact]
    public void Click_InvokesOnClickCallback()
    {
        ChildSummary? clicked = null;
        var child = CreateChild();

        var cut = RenderComponent<ChildCard>(p => p
            .Add(x => x.Child, child)
            .Add(x => x.OnClick, c => clicked = c));

        cut.Find(".child-card").Click();

        Assert.Equal(child, clicked);
    }
}
