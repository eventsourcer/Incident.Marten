namespace Incident.Api.Domain;

public record IncidentEntity(
    Guid Id,
    IncidentStatus Status = IncidentStatus.Pending,
    bool HasOutstandingResponseToCustomer = false
)
{
    public static IncidentEntity Create(IncidentLogged incident) =>
        new(incident.IncidentId);

    public IncidentEntity Apply(AgentRespondedToIncident incident) =>
        this with { HasOutstandingResponseToCustomer = false };

    public IncidentEntity Apply(CustomerRespondedToIncident incident) =>
        this with { HasOutstandingResponseToCustomer = false };

    public IncidentEntity Apply(IncidentResolved incident) =>
        this with { Status = IncidentStatus.Resolved };

    public IncidentEntity Apply(ResolutionAcknowledgedByCustomer incident) =>
        this with { Status = IncidentStatus.ResolutionAcknowledgedByCustomer };
    
    public IncidentEntity Apply(IncidentClosed incident) =>
        this with { Status = IncidentStatus.Closed };
}

public enum IncidentStatus{
    Pending = 1,
    Resolved = 8,
    ResolutionAcknowledgedByCustomer = 16,
    Closed = 32
};