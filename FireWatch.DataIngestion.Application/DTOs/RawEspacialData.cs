using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireWatch.DataIngestion.Application.DTOs;
public record RawEspacialData(
    double Latitude,
    double Longitude,
    double Brightness,
    double Frp,
    double Confidence,
    string ScanType,
    string DayNight,
    DateTime AcquiredAt,
    string SourceIdentifier
);
