using Incident.Api.Domain;

namespace Incident.Api.Application;

public record LogIncident(
    Guid IncidentId,
    Guid CustomerId,
    Contact Contact,
    string Description,
    Guid LoggedBy,
    DateTimeOffset Now
);
public record CategoriseIncident(
    Guid IncidentId,
    IncidentCategory Category,
    Guid CategorisedBy
);
public record PrioritiseIncident(
    Guid IncidentId,
    IncidentPriority Priority,
    Guid PrioritisedBy
);
public record AssignAgentToIncident(
    Guid IncidentId,
    Guid AgentId
);
public record RecordAgentResponseToIncident(
    Guid IncidentId,
    IncidentResponse.FromAgent Response
);
public record RecordCustomerResponseToIncident(
    Guid IncidentId,
    IncidentResponse.FromCustomer Response
);
public record ResolveIncident(
    Guid IncidentId,
    ResolutionType Resolution,
    Guid ResolvedBy
);
public record AcknowledgeResolution(
    Guid IncidentId,
    Guid AcknowledgedBy
);
public record CloseIncident(
    Guid IncidentId,
    Guid ClosedBy
);

public static class IncidentService
{
    public static IncidentLogged Handle(LogIncident command)
    {
        return new(
            command.IncidentId,
            command.CustomerId,
            command.Contact,
            command.Description,
            command.LoggedBy,
            DateTimeOffset.Now
        );
    }
    public static IncidentCategorised Handle(IncidentEntity current, CategoriseIncident command)
    {
        if(current.Status == IncidentStatus.Closed)
            throw new InvalidOperationException("Incident already closed");
        
        return new(
            command.IncidentId,
            command.Category,
            command.CategorisedBy,
            DateTimeOffset.Now
        );
    }
    public static IncidentPrioritised Handle(IncidentEntity current, PrioritiseIncident command)
    {
        if(current.Status == IncidentStatus.Closed)
            throw new InvalidOperationException("Incident already closed");
        
        return new(
            command.IncidentId,
            command.Priority,
            command.PrioritisedBy,
            DateTimeOffset.Now
        );
    }
    public static AgentAssignedToIncident Handle(IncidentEntity current, AssignAgentToIncident command)
    {
        if(current.Status == IncidentStatus.Closed)
            throw new InvalidOperationException("incident already cloded");
        return new(command.IncidentId, command.AgentId, DateTimeOffset.Now);
    }
    public static AgentRespondedToIncident Handle(IncidentEntity current, RecordAgentResponseToIncident command)
    {
        if(current.Status == IncidentStatus.Closed)
            throw new InvalidOperationException("incident already cloded");
        return new(command.IncidentId, command.Response, DateTimeOffset.Now);
    }
    public static CustomerRespondedToIncident Handle(IncidentEntity current, RecordCustomerResponseToIncident command)
    {
        if(current.Status == IncidentStatus.Closed)
            throw new InvalidOperationException("incident already cloded");
        return new(command.IncidentId, command.Response, DateTimeOffset.Now);
    }
    public static IncidentResolved Handle(IncidentEntity current, ResolveIncident command)
    {
        if(current.Status is IncidentStatus.Resolved or IncidentStatus.Closed)
            throw new InvalidOperationException("Can not resolve already resolved or closed incident");
        return new(command.IncidentId, command.Resolution, command.ResolvedBy, DateTimeOffset.Now);
    }
    public static ResolutionAcknowledgedByCustomer Handle(IncidentEntity current, AcknowledgeResolution command)
    {
        if(current.Status is not IncidentStatus.Resolved)
            throw new InvalidOperationException("Only resolved incidents can be acknowledged");

        if(current.HasOutstandingResponseToCustomer)
            throw new InvalidOperationException("Can  not resolve incident that has outstanding responses");
    
        return new(command.IncidentId, command.AcknowledgedBy, DateTimeOffset.Now);
    }
    public static IncidentClosed Handle(IncidentEntity current, CloseIncident command)
    {
        if(current.Status is not IncidentStatus.ResolutionAcknowledgedByCustomer)
            throw new InvalidOperationException("Only acknowledged incidents can be closed");
        return new(command.IncidentId, command.ClosedBy, DateTimeOffset.Now);
    }
}