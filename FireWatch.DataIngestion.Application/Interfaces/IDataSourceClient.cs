using FireWatch.DataIngestion.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireWatch.DataIngestion.Application.Interfaces;

public interface IDataSourceClient
{
    string SourceName { get; }
    Task<IReadOnlyList<RawEspacialData>> FetchAsync(
        DateTime from, DateTime to, CancellationToken ct = default);
}
