using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireWatch.DataIngestion.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync<T>(T @event, string routingKey, CancellationToken ct = default)
        where T : class;
}