using System.Security.Cryptography;
using System.Text;

namespace ZedPad.Core.Pairing;

public enum PairingResult
{
    Success,
    Invalid,
    Expired,
}

public readonly record struct PairingCode(string Token, DateTimeOffset ExpiresAt);

/// <summary>
/// Issues one pairing token at a time. A token is random, expires after <see cref="Lifetime"/>
/// and can be used once.
/// </summary>
public sealed class PairingTokenService
{
    public const int TokenLength = 8;
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(2);

    // No 0/O/1/I so a code typed by hand is not ambiguous. 32 symbols x 8 characters = 40 bits.
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private readonly TimeProvider _time;
    private readonly object _gate = new();
    private string? _token;
    private DateTimeOffset _expiresAt;

    public PairingTokenService(TimeProvider? time = null)
    {
        _time = time ?? TimeProvider.System;
    }

    /// <summary>Replaces any outstanding token with a new one.</summary>
    public PairingCode Issue()
    {
        var token = RandomNumberGenerator.GetString(Alphabet, TokenLength);
        lock (_gate)
        {
            _token = token;
            _expiresAt = _time.GetUtcNow() + Lifetime;
            return new PairingCode(token, _expiresAt);
        }
    }

    /// <summary>Checks a candidate and, on success, consumes the token.</summary>
    public PairingResult TryConsume(string? candidate)
    {
        var normalized = Normalize(candidate);

        lock (_gate)
        {
            if (_token is null || !Matches(_token, normalized))
            {
                return PairingResult.Invalid;
            }

            if (_time.GetUtcNow() >= _expiresAt)
            {
                _token = null;
                return PairingResult.Expired;
            }

            _token = null;
            return PairingResult.Success;
        }
    }

    /// <summary>Formats a token for display as ABCD-EFGH.</summary>
    public static string Format(string token) =>
        token.Length == TokenLength ? $"{token[..4]}-{token[4..]}" : token;

    private static string Normalize(string? candidate) =>
        string.Concat((candidate ?? string.Empty).Where(c => c is not ('-' or ' ')))
            .ToUpperInvariant();

    private static bool Matches(string expected, string candidate) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(candidate));
}
