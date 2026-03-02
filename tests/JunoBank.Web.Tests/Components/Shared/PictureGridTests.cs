using Bunit;
using JunoBank.Web.Components.Shared;
using JunoBank.Web.Constants;
using JunoBank.Web.Tests.Helpers;

namespace JunoBank.Web.Tests.Components.Shared;

public class PictureGridTests : ComponentTestBase
{
    [Fact]
    public void Render_ShowsCorrectNumberOfButtons()
    {
        var cut = RenderComponent<PictureGrid>();

        Assert.Equal(PicturePasswordImages.GridDisplayCount, cut.FindAll(".picture-btn").Count);
    }

    [Fact]
    public void Render_ShowsEmptySequenceDots()
    {
        var cut = RenderComponent<PictureGrid>();

        var dots = cut.FindAll(".sequence-dot");
        Assert.Equal(PicturePasswordImages.DefaultSequenceLength, dots.Count);
        Assert.Empty(cut.FindAll(".sequence-dot.filled"));
    }

    [Fact]
    public void SelectImage_FillsSequenceDot()
    {
        var cut = RenderComponent<PictureGrid>();

        cut.FindAll(".picture-btn")[0].Click();

        Assert.Single(cut.FindAll(".sequence-dot.filled"));
    }

    [Fact]
    public void SelectFourImages_FiresOnSequenceComplete()
    {
        string[]? completedSequence = null;
        var cut = RenderComponent<PictureGrid>(p => p
            .Add(x => x.OnSequenceComplete, seq => completedSequence = seq));

        for (int i = 0; i < PicturePasswordImages.DefaultSequenceLength; i++)
            cut.FindAll(".picture-btn")[i].Click();

        Assert.NotNull(completedSequence);
        Assert.Equal(PicturePasswordImages.DefaultSequenceLength, completedSequence.Length);
    }

    [Fact]
    public void SelectFourImages_DisablesButtons()
    {
        var cut = RenderComponent<PictureGrid>(p => p
            .Add(x => x.OnSequenceComplete, _ => { }));

        for (int i = 0; i < PicturePasswordImages.DefaultSequenceLength; i++)
            cut.FindAll(".picture-btn")[i].Click();

        // Re-query after state change
        var updatedButtons = cut.FindAll(".picture-btn");
        Assert.All(updatedButtons, btn => Assert.True(btn.HasAttribute("disabled")));
    }

    [Fact]
    public void Reset_ClearsSelection()
    {
        var cut = RenderComponent<PictureGrid>();

        cut.FindAll(".picture-btn")[0].Click();
        Assert.Single(cut.FindAll(".sequence-dot.filled"));

        cut.InvokeAsync(() => cut.Instance.Reset());

        Assert.Empty(cut.FindAll(".sequence-dot.filled"));
    }

    [Fact]
    public void ShowError_DisplaysErrorMessage()
    {
        var cut = RenderComponent<PictureGrid>();

        cut.InvokeAsync(() => cut.Instance.ShowError("Wrong password!"));

        Assert.Contains("Wrong password!", cut.Markup);
    }
}
