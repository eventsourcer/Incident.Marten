using COnsumer1;

namespace Consumer1;

public record IncidentLogged(
    Guid IncidentId,
    Guid CustomerId,
    Contact Contact,
    string Description,
    Guid LoggedBy,
    DateTimeOffset LoggedAt
) : IntegrationEvent (IncidentId);

public record IncidentCategorised(
    Guid IncidentId,
    IncidentCategory Category,
    Guid CategorisedBy,
    DateTimeOffset CategorisedAt
) : IntegrationEvent (IncidentId);

public record IncidentPrioritised(
    Guid IncidentId,
    IncidentPriority Priority,
    Guid PrioritisedBy,
    DateTimeOffset PrioritisedAt
) : IntegrationEvent (IncidentId);

public record AgentAssignedToIncident(
    Guid IncidentId,
    Guid AgentId,
    DateTimeOffset AssignedAt
) : IntegrationEvent (IncidentId);

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