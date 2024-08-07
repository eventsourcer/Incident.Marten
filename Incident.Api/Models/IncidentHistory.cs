using Incident.Api.Domain;
using JasperFx.Core;
using Marten.Events;
using Marten.Events.Projections;

namespace Incident.Api.Models;

public record IncidentHistory(
    Guid Id,
    Guid IncidentId,
    string Description
);

public class IncidentHistoryTransformation : EventProjection
{
    public IncidentHistory Transform(IEvent<IncidentLogged> input)
    {
        var (inId, custId, contact, desc, loggedBy, loggedAt) = input.Data;

        return new (
            CombGuidIdGeneration.NewGuid(),
            inId,
            $"{loggedAt} logged incident with id {inId} for customer {custId} and desc {desc}"
        );
    }

    public IncidentHistory Transform(IEvent<IncidentCategorised> input)
    {
        var (inId, category, categorisedBy, categorisedAt) = input.Data;

        return new (
            CombGuidIdGeneration.NewGuid(),
            inId,
            $"{categorisedAt} categorised incident with id {inId} and category {category}"
        );
    }
}