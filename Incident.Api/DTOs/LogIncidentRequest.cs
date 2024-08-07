using Incident.Api.Domain;

namespace Incident.Api.DTOs;

public record LogIncidentRequest(
    Contact Contact,
    string Description
);