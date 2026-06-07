using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireWatch.DataIngestion.Application.Eventos
{
    public record EspacialDataReceivedEvento(
    Guid RecordId,
    double Latitude,
    double Longitude,
    double Brightness,
    double Frp,
    double Confidence,
    string Source,
    string DayNight,
    DateTime AcquiredAt,
    DateTime PublishedAt
);
}
