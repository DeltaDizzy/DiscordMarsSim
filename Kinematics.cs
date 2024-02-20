using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DiscordMarsSim;

internal sealed class Kinematics
{
    private HttpClient client;
    private Regex timeExtractor = new("(?<=A.D.)(.*)(?=TDB)");
    private Regex delayExtractor = new("(?<=LT=)(.*)(?=RG)");

    Dictionary<DateTime, TimeSpan> delayTable = [];
    public Kinematics() {
        client = new HttpClient() {
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

    public Dictionary<DateTime, TimeSpan> ReadEphemeris() {
        Stopwatch sw = Stopwatch.StartNew();
        Dictionary<DateTime, TimeSpan> table = [];
        string result = Request().Result;
        Log($"Request recieved in {sw.ElapsedMilliseconds}ms!");
        File.WriteAllText($"{Assembly.GetExecutingAssembly().Location.Replace("\\DiscordMarsSim.dll", "")}\\result.txt", result);
        var lines = result.Split('\n').ToList();
        DateTime? timeToAdd = null;
        TimeSpan? delayToAdd = null;

        // merge lines
        var dataLines = lines.SkipWhile(item => !item.Contains("$$SOE"));
        Console.WriteLine(dataLines.First());
        var timeLines = dataLines.Where(line => line.Contains("TDB"));
        var delayLines = dataLines.Where(line => line.Contains("LT"));
        var tuples = timeLines.Zip(delayLines);
        Log($"Tuples created in {sw.ElapsedMilliseconds}ms!");
        foreach (var entry in tuples)
        {
            //Console.WriteLine(entry);
            if(DateTime.TryParse(timeExtractor.Match(entry.First).Value.Trim(), out DateTime time)) {
                // have a time!
                timeToAdd = time;
            }
            if (double.TryParse(delayExtractor.Match(entry.Second).Value.Trim(), out double delay))
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
        Log($"Parsing done in {sw.ElapsedMilliseconds}ms!");
        sw.Stop();
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

    public void Log(string msg) => Console.WriteLine(msg); 
}
