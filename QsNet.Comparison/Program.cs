using System.Diagnostics.CodeAnalysis;
using System.Text;
using STJ = System.Text.Json;
using NJ = Newtonsoft.Json;

namespace QsNet.Comparison;

[ExcludeFromCodeCoverage]
internal abstract class Program
{
    private static void Main()
    {
        Console.OutputEncoding = new UTF8Encoding(false);

        var jsonPath = Path.Combine(AppContext.BaseDirectory, "js", "test_cases.json");
        if (!File.Exists(jsonPath))
        {
            Console.Error.WriteLine($"Missing test_cases.json at {jsonPath}");
            Environment.Exit(1);
        }

        var json = File.ReadAllText(jsonPath);

        var cases = STJ.JsonSerializer.Deserialize<List<TestCase>>(json, new STJ.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (cases is null)
        {
            Console.Error.WriteLine("No test cases loaded.");
            Environment.Exit(1);
        }

        var percentEncodeBrackets = true;

        foreach (var c in cases)
        {
            // Convert JSON element -> CLR object graph
            var dataObj = c.Data.HasValue ? FromJsonElement(c.Data.Value) : null;

            // Encode (optionally percent-encode '[' and ']')
            var encodedOut = Qs.Encode(dataObj ?? new Dictionary<string, object?>());
            if (percentEncodeBrackets)
                encodedOut = encodedOut.Replace("[", "%5B").Replace("]", "%5D");
            Console.WriteLine($"Encoded: {encodedOut}");

            // Decode and JSON-serialize with Newtonsoft (keeps emojis as real characters)
            var decodedOut = Qs.Decode(c.Encoded!);
            Console.WriteLine($"Decoded: {CanonJson(decodedOut)}");
        }
    }

    // Use Newtonsoft so emojis aren’t turned into surrogate-pair escapes
    private static string CanonJson(object? v)
    {
        return NJ.JsonConvert.SerializeObject(
            v,
            NJ.Formatting.None,
            new NJ.JsonSerializerSettings
            {
                // Default doesn't escape non-ASCII; keep it explicit in case of future changes
                StringEscapeHandling = NJ.StringEscapeHandling.Default
            }
        );
    }

    private static object? FromJsonElement(STJ.JsonElement e)
    {
        switch (e.ValueKind)
        {
            case STJ.JsonValueKind.Null:
            case STJ.JsonValueKind.Undefined:
                return null;

            case STJ.JsonValueKind.String:
                return e.GetString();

            case STJ.JsonValueKind.Number:
                // keep numbers as strings to match qs parse/stringify behavior
                return e.GetRawText().Trim('"');

            case STJ.JsonValueKind.True:
            case STJ.JsonValueKind.False:
                return e.GetBoolean();

            case STJ.JsonValueKind.Array:
            {
                var list = new List<object?>();
                foreach (var item in e.EnumerateArray())
                    list.Add(FromJsonElement(item));
                return list;
            }

            case STJ.JsonValueKind.Object:
            {
                var dict = new Dictionary<string, object?>();
                foreach (var prop in e.EnumerateObject())
                    dict[prop.Name] = FromJsonElement(prop.Value);
                return dict;
            }

            default:
                return e.GetRawText();
        }
    }

    private class TestCase
    {
        public string? Encoded { get; set; }
        public STJ.JsonElement? Data { get; set; }
    }
}