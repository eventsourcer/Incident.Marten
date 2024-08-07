using Incident.Api.Domain;

namespace Incident.Api.DTOs;

public record ResolveIncidentRequest(
    ResolutionType Resolution
);