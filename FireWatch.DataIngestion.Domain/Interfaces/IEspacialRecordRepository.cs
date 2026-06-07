using FireWatch.DataIngestion.Domain.Entities;
using FireWatch.DataIngestion.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireWatch.DataIngestion.Domain.Interfaces;

public interface IEspacialRecordRepository
{
    Task<EspacialRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<EspacialRecord>> GetPendingAsync(CancellationToken ct = default);
    Task<IReadOnlyList<EspacialRecord>> GetBySourceAsync(
        DataSourceType source, DateTime from, DateTime to, CancellationToken ct = default);
    Task AddAsync(EspacialRecord record, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<EspacialRecord> records, CancellationToken ct = default);
    Task UpdateAsync(EspacialRecord record, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
