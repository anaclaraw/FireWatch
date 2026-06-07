using FireWatch.DataIngestion.Application.DTOs;
using FireWatch.DataIngestion.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FireWatch.DataIngestion.Application.Interfaces;

namespace FireWatch.DataIngestion.Infrastructure.Clients;

public class FirmsHttpClient : IDataSourceClient
{
    private readonly HttpClient _http;
    private readonly ILogger<FirmsHttpClient> _logger;
    private readonly string _apiKey;

    public string SourceName => "NasaFirms";

    private const string BrazilBbox = "-73.99,-33.75,-28.85,5.27";

    public FirmsHttpClient(
        HttpClient http,
        IConfiguration config,
        ILogger<FirmsHttpClient> logger)
    {
        _http = http;
        _logger = logger;
        _apiKey = config["ExternalSources:NasaFirms:ApiKey"]
                   ?? throw new InvalidOperationException(
                       "API Key da NASA FIRMS não configurada em ExternalSources:NasaFirms:ApiKey");
    }

    public async Task<IReadOnlyList<RawEspacialData>> FetchAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        var dayRange = Math.Clamp((int)(to - from).TotalDays + 1, 1, 10);

        var url = $"https://firms.modaps.eosdis.nasa.gov/api/area/csv" +
                  $"/{_apiKey}/VIIRS_SNPP_NRT/{BrazilBbox}/{dayRange}";

        _logger.LogInformation(
            "Buscando dados NASA FIRMS | sensor: VIIRS_SNPP_NRT | dias: {Days} | bbox: {Bbox}",
            dayRange, BrazilBbox);

        try
        {
            var csv = await _http.GetStringAsync(url, ct);
            var parsed = ParseCsv(csv);

            _logger.LogInformation(
                "NASA FIRMS retornou {Count} focos de calor.", parsed.Count);

            return parsed;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Falha ao acessar API da NASA FIRMS.");
            throw;
        }
    }

    private static List<RawEspacialData> ParseCsv(string csv)
    {
       
        var results = new List<RawEspacialData>();
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length <= 1) return results; 

        foreach (var line in lines.Skip(1))
        {
            var cols = line.Trim().Split(',');
            if (cols.Length < 14) continue;

            if (!TryParseDouble(cols[0], out var lat)) continue;
            if (!TryParseDouble(cols[1], out var lon)) continue;

            TryParseDouble(cols[2], out var brightness);
            TryParseDouble(cols[12], out var frp);

            var confidence = cols[9].Trim().ToLower() switch
            {
                "high" => 90.0,
                "nominal" => 70.0,
                "low" => 40.0,
                _ => double.TryParse(cols[9], out var c) ? c : 70.0
            };

            var acquiredAt = ParseAcquisitionDate(cols[5].Trim(), cols[6].Trim());
            var dayNight = cols[13].Trim().ToUpper();

            results.Add(new RawEspacialData(
                Latitude: lat,
                Longitude: lon,
                Brightness: brightness,
                Frp: frp,
                Confidence: confidence,
                ScanType: "VIIRS_SNPP",
                DayNight: dayNight is "D" or "N" ? dayNight : "D",
                AcquiredAt: acquiredAt,
                SourceIdentifier: "NasaFirms_VIIRS"
            ));
        }

        return results;
    }

    private static bool TryParseDouble(string s, out double result)
        => double.TryParse(s.Trim(),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out result);

    private static DateTime ParseAcquisitionDate(string date, string time)
    {
        try
        {
            var hours = int.Parse(time.PadLeft(4, '0')[..2]);
            var minutes = int.Parse(time.PadLeft(4, '0')[2..]);
            return DateTime.ParseExact(date, "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture)
                .AddHours(hours).AddMinutes(minutes);
        }
        catch
        {
            return DateTime.UtcNow;
        }
    }
}
