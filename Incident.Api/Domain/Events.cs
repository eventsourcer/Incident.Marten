namespace Incident.Api.Domain;

public record IncidentLogged(
    Guid IncidentId,
    Guid CustomerId,
    Contact Contact,
    string Description,
    Guid LoggedBy,
    DateTimeOffset LoggedAt
);

public record IncidentCategorised(
    Guid IncidentId,
    IncidentCategory Category,
    Guid CategorisedBy,
    DateTimeOffset CategorisedAt
);

public record IncidentPrioritised(
    Guid IncidentId,
    IncidentPriority Priority,
    Guid PrioritisedBy,
    DateTimeOffset PrioritisedAt
);

public record AgentAssignedToIncident(
    Guid IncidentId,
    Guid AgentId,
    DateTimeOffset AssignedAt
);

public record AgentRespondedToIncident(
    Guid IncidentId,
    IncidentResponse.FromAgent Response,
    DateTimeOffset RespondedAt
);

public record CustomerRespondedToIncident(
    Guid IncidentId,
    IncidentResponse.FromCustomer Response,
    DateTimeOffset RespondedAt
);

public record IncidentResolved(
    Guid IncidentId,
    ResolutionType Resolution,
    Guid ResolvedBy,
    DateTimeOffset ResolvedAt
);

public record ResolutionAcknowledgedByCustomer(
    Guid IncidentId,
    Guid AcknowledgedBy,
    DateTimeOffset AcknowledgedAt
);
public record IncidentClosed(
    Guid IncidentId,
    Guid ClosedBy,
    DateTimeOffset ClosedAt
);

public record Contact(
    ContactChannel ContactChannel,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNo
);
public enum ContactChannel
{
    Email,
    Phone,
    InPerson
}
public enum IncidentCategory{
    Software,
    Hardware,
    Network,
    Database
};

public enum IncidentPriority{
    Critical,
    High,
    Medium,
    Low
};
public abstract record IncidentResponse
{
    public record FromAgent(
        Guid AgentId,
        string Content,
        bool VisibleToCustomer = false
    ) : IncidentResponse(Content);
    public record FromCustomer(
        Guid CustomerId,
        string Content,
        bool VisibleToCustomer = false
    ) : IncidentResponse(Content);

    public string Content { get; init; }
    public IncidentResponse(string content)
    {
        Content = content;
    }
}

public enum ResolutionType{
    Temporary,
    Permanent,
    NotAnIncident
}