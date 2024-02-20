using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DiscordMarsSim;

internal sealed partial class Kinematics
{
    private readonly HttpClient client;
    [GeneratedRegex("(?<=LT=)(.*)(?=RG)")]
    private static partial Regex DelayRegex();
    [GeneratedRegex("(?<=A.D.)(.*)(?=TDB)")]
    private static partial Regex TimeRegex();
    private readonly Regex timeExtractor = TimeRegex();
    private readonly Regex delayExtractor = DelayRegex();

    Dictionary<DateTime, TimeSpan> delayTable = [];
    public Kinematics()
    {
        client = new HttpClient()
        {
            BaseAddress = new Uri("https://ssd.jpl.nasa.gov/api/horizons.api")
        };
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        delayTable = ReadEphemeris();
    }

    private async Task<string> Request()
    {
        string request = $"{client.BaseAddress}?format=text&COMMAND='499'&EPHEM_TYPE='VECTORS'&START_TIME='{DateTime.Now.ToString("u")[..^1]}'&STOP_TIME='{DateTime.Now.AddDays(30).ToString("u")[..^1]}'&VEC_TABLE='6'";
        return await client.GetStringAsync(request);
    }

    public Dictionary<DateTime, TimeSpan> ReadEphemeris()
    {
        Dictionary<DateTime, TimeSpan> table = [];
        string result = Request().Result;
        //File.WriteAllText($"{Assembly.GetExecutingAssembly().Location.Replace("\\DiscordMarsSim.dll", "")}\\result.txt", result);
        var lines = result.Split('\n').ToList();
        DateTime? timeToAdd = null;
        TimeSpan? delayToAdd = null;

        // merge lines
        var dataLines = lines.SkipWhile(item => !item.Contains("$$SOE"));
        Console.WriteLine(dataLines.First());
        var timeLines = dataLines.Where(line => line.Contains("TDB"));
        var delayLines = dataLines.Where(line => line.Contains("LT"));
        var tuples = timeLines.Zip(delayLines);
        foreach (var (First, Second) in tuples)
        {
            //Console.WriteLine(entry);
            if (DateTime.TryParse(timeExtractor.Match(First).Value.Trim(), out DateTime time))
            {
                // have a time!
                timeToAdd = time;
            }
            if (double.TryParse(delayExtractor.Match(Second).Value.Trim(), out double delay))
            {
                delayToAdd = new TimeSpan(0, 0, (int)Math.Round(delay));
            }
            if (timeToAdd is not null && delayToAdd is not null)
            {
                table.Add(timeToAdd.Value, delayToAdd.Value);
                timeToAdd = null;
                delayToAdd = null;
            }
        }
        return table;
    }

    public int GetEntryCount() => delayTable.Count;

    public TimeSpan GetTimeDelay()
    {
        if (delayTable.Last().Key.Ticks - DateTime.Now.Ticks < 0)
        {
            // ran out of data!
            delayTable = ReadEphemeris();
        }
        return delayTable.OrderBy(pair => Math.Abs(pair.Key.Subtract(DateTime.Now).Seconds)).First().Value;
    }

    public static void Log(string msg) => Console.WriteLine(msg);
}
