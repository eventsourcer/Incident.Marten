using Incident.Api.Domain;

namespace Incident.Api.DTOs;

public record CategoriseIncidentRequest(
    IncidentCategory Category
);