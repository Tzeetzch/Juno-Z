using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor.Services;
using JunoBank.Web.Services;

namespace JunoBank.Web.Tests.Helpers;

/// <summary>
/// Base class for bUnit component tests.
/// Registers MudBlazor services and common mocks.
/// </summary>
public abstract class ComponentTestBase : TestContext
{
    protected readonly Mock<IBrowserTimeService> MockBrowserTime;

    protected ComponentTestBase()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();

        MockBrowserTime = new Mock<IBrowserTimeService>();
        MockBrowserTime
            .Setup(x => x.ToLocal(It.IsAny<DateTime>()))
            .Returns<DateTime>(dt => dt);

        Services.AddSingleton(MockBrowserTime.Object);
    }
}
