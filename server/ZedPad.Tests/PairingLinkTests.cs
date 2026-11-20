using System.Text.Json;
using ZedPad.Core.Pairing;

namespace ZedPad.Tests;

public class PairingLinkTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private sealed record Vector(string Name, List<string> Hosts, int Port, string Token, string Uri)
    {
        public override string ToString() => Name;
    }

    public static TheoryData<string> VectorNames()
    {
        var data = new TheoryData<string>();
        foreach (var vector in LoadVectors())
        {
            data.Add(vector.Name);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(VectorNames))]
    public void CreateMatchesSharedVector(string name)
    {
        var vector = LoadVectors().Single(v => v.Name == name);

        var link = PairingLink.Create(vector.Hosts, vector.Port, vector.Token);

        Assert.Equal(vector.Uri, link);
    }

    private static List<Vector> LoadVectors()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "pairing-link-vectors.json");
        return JsonSerializer.Deserialize<List<Vector>>(File.ReadAllText(path), JsonOptions)!;
    }
}
