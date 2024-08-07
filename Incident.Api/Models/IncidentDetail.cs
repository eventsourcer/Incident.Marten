using Incident.Api.Domain;
using Marten.Events.Aggregation;


namespace Incident.Api.Models;

public record IncidentDetail(
    Guid Id,
    Guid CustomerId,
    IncidentStatus Status,
    IncidentNote[] Notes,
    Guid? AgentId,
    IncidentCategory? Category = null,
    IncidentPriority? Priority = null,
    int Version = 1
);

public class IncidentDetailProjection : SingleStreamProjection<IncidentDetail>
{
    public static IncidentDetail Create(IncidentLogged logged) =>
        new (logged.IncidentId, logged.CustomerId, IncidentStatus.Pending, [], logged.LoggedBy);

    public IncidentDetail Apply(IncidentCategorised categorised, IncidentDetail current) =>
        current with { Category = categorised.Category };

    public IncidentDetail Apply(IncidentPrioritised prioritised, IncidentDetail current) =>
        current with { Priority = prioritised.Priority };

    public IncidentDetail Apply(AgentAssignedToIncident agentAssigned, IncidentDetail current) =>
        current with { AgentId = agentAssigned.AgentId };

    public IncidentDetail Apply(AgentRespondedToIncident responded, IncidentDetail current) =>
        current with
        {
            Notes = current.Notes.Union(
                [
                    new (
                    IncidentNoteType.FromAgent, 
                    responded.Response.AgentId, 
                    responded.Response.Content,
                    false
                    )
                ]
            ).ToArray()
        };

    public IncidentDetail Apply(CustomerRespondedToIncident responded, IncidentDetail current) =>
        current with
        {
            Notes = current.Notes.AsEnumerable().Append(
                new (
                IncidentNoteType.FromCustomer, 
                responded.Response.CustomerId, 
                responded.Response.Content,
                false
                )
            ).ToArray()
        };

    public IncidentDetail Apply(IncidentResolved resolved, IncidentDetail current) =>
        current with { Status = IncidentStatus.Resolved };

    public IncidentDetail Apply(ResolutionAcknowledgedByCustomer acknowledged, IncidentDetail current) =>
        current with { Status = IncidentStatus.ResolutionAcknowledgedByCustomer };

    public IncidentDetail Apply(IncidentClosed closed, IncidentDetail current) =>
        current with { Status = IncidentStatus.Closed };
}


public record IncidentNote(
    IncidentNoteType Type,
    Guid From,
    string Content,
    bool VisibleToCustomer
);
public enum IncidentNoteType
{
    FromAgent,
    FromCustomer
};