using ZedPad.Core.Pairing;

namespace ZedPad.Tests;

public class PairingTokenServiceTests
{
    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }

    [Fact]
    public void IssuedTokenIsEightUnambiguousCharacters()
    {
        var token = new PairingTokenService().Issue().Token;

        Assert.Equal(PairingTokenService.TokenLength, token.Length);
        Assert.All(token, c => Assert.DoesNotContain(c, "01OI"));
        Assert.Equal(token.ToUpperInvariant(), token);
    }

    [Fact]
    public void TokensDiffer()
    {
        var service = new PairingTokenService();

        Assert.NotEqual(service.Issue().Token, service.Issue().Token);
    }

    [Fact]
    public void TokenIsAcceptedOnceOnly()
    {
        var service = new PairingTokenService();
        var token = service.Issue().Token;

        Assert.Equal(PairingResult.Success, service.TryConsume(token));
        Assert.Equal(PairingResult.Invalid, service.TryConsume(token));
    }

    [Fact]
    public void WrongTokenIsInvalidAndDoesNotConsumeTheRealOne()
    {
        var service = new PairingTokenService();
        var token = service.Issue().Token;

        Assert.Equal(PairingResult.Invalid, service.TryConsume("AAAAAAAA"));
        Assert.Equal(PairingResult.Invalid, service.TryConsume(null));
        Assert.Equal(PairingResult.Invalid, service.TryConsume(string.Empty));
        Assert.Equal(PairingResult.Success, service.TryConsume(token));
    }

    [Fact]
    public void CodeIsAcceptedWhenTypedWithDashAndLowercase()
    {
        var service = new PairingTokenService();
        var token = service.Issue().Token;
        var typed = PairingTokenService.Format(token).ToLowerInvariant();

        Assert.Equal(PairingResult.Success, service.TryConsume(typed));
    }

    [Fact]
    public void TokenExpiresAfterLifetime()
    {
        var time = new ManualTimeProvider();
        var service = new PairingTokenService(time);
        var token = service.Issue().Token;

        time.Advance(PairingTokenService.Lifetime + TimeSpan.FromSeconds(1));

        Assert.Equal(PairingResult.Expired, service.TryConsume(token));
        Assert.Equal(PairingResult.Invalid, service.TryConsume(token));
    }

    [Fact]
    public void TokenStillValidJustBeforeExpiry()
    {
        var time = new ManualTimeProvider();
        var service = new PairingTokenService(time);
        var token = service.Issue().Token;

        time.Advance(PairingTokenService.Lifetime - TimeSpan.FromSeconds(1));

        Assert.Equal(PairingResult.Success, service.TryConsume(token));
    }

    [Fact]
    public void IssuingANewTokenInvalidatesThePreviousOne()
    {
        var service = new PairingTokenService();
        var first = service.Issue().Token;
        var second = service.Issue().Token;

        Assert.Equal(PairingResult.Invalid, service.TryConsume(first));
        Assert.Equal(PairingResult.Success, service.TryConsume(second));
    }
}
