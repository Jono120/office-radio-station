using OfficeJukebox.Application.Configuration;
using OfficeJukebox.Application.Skip.Rules;
using OfficeJukebox.Domain.Entities;
using Microsoft.Extensions.Options;

namespace OfficeJukebox.Application.Tests.Skip.Rules;

public class DefaultSkipRuleTests
{
    [Fact]
    public void Returns_configured_minimum_when_like_count_is_lower()
    {
        var rule = new DefaultSkipRule(Options.Create(new SkipRulesOptions { MinimumVetoCount = 3 }));
        var track = new TrackPlay { Likes = new List<TrackPlayLike> { new() } };

        var result = rule.GetRequiredVetoCount(track);

        Assert.Equal(3, result);
    }

    [Fact]
    public void Returns_like_count_when_higher_than_configured_minimum()
    {
        var rule = new DefaultSkipRule(Options.Create(new SkipRulesOptions { MinimumVetoCount = 3 }));
        var track = new TrackPlay
        {
            Likes = new List<TrackPlayLike> { new(), new(), new(), new() }
        };

        var result = rule.GetRequiredVetoCount(track);

        Assert.Equal(4, result);
    }
}
