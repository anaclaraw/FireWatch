using FireWatch.DataIngestion.Domain.Entities;
using FireWatch.DataIngestion.Domain.Enums;
using FireWatch.DataIngestion.Domain.Interfaces;
using FireWatch.DataIngestion.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireWatch.DataIngestion.Infrastructure.Repositories;

public class EspacialRecordRepository : IEspacialRecordRepository
{
    private readonly AppDbContext _context;

    public EspacialRecordRepository(AppDbContext context)
        => _context = context;

    public async Task<EspacialRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.EspacialRecords
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<EspacialRecord>> GetPendingAsync(CancellationToken ct = default)
        => await _context.EspacialRecords
            .Where(x => x.Status == ProcessingStatus.Pending)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<EspacialRecord>> GetBySourceAsync(
        DataSourceType source, DateTime from, DateTime to, CancellationToken ct = default)
        => await _context.EspacialRecords
            .Where(x => x.Source == source
                     && x.AcquiredAt >= from
                     && x.AcquiredAt <= to)
            .OrderByDescending(x => x.AcquiredAt)
            .ToListAsync(ct);

    public async Task AddAsync(EspacialRecord record, CancellationToken ct = default)
        => await _context.EspacialRecords.AddAsync(record, ct);

    public async Task AddRangeAsync(
        IEnumerable<EspacialRecord> records, CancellationToken ct = default)
        => await _context.EspacialRecords.AddRangeAsync(records, ct);

    public Task UpdateAsync(EspacialRecord record, CancellationToken ct = default)
    {
        _context.EspacialRecords.Update(record);
        return Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
