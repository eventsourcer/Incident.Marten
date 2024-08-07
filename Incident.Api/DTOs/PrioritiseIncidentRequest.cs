using Incident.Api.Domain;

namespace Incident.Api.DTOs;

public record PrioritiseIncidentRequest(
    IncidentPriority Priority
);