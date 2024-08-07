using Incident.Api.Application;
using Incident.Api.Domain;
using Incident.Api.DTOs;
using Incident.Api.Extensions;
using Incident.Api.Models;
using Incident.Api.Publish;
using JasperFx.Core;
using Marten;
using Marten.AspNetCore;
using Marten.Events.Daemon.Resiliency;
using Marten.Events.Projections;
using Marten.Pagination;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Weasel.Core;
using static Incident.Api.Application.IncidentService;
using static Incident.Api.Extensions.ETagExtensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

builder.Services.AddMarten(sp =>
{
    var options = new StoreOptions();

    var schemaName = Environment.GetEnvironmentVariable("SchemaName") ?? "Helpdesk";
    options.Events.DatabaseSchemaName = schemaName;
    options.DatabaseSchemaName = schemaName;
    options.Connection(builder.Configuration.GetConnectionString("Incidents") ?? "Incidents");

    options.UseSystemTextJsonForSerialization(EnumStorage.AsString);

    options.Projections.Add<IncidentDetailProjection>(ProjectionLifecycle.Async);
    options.Projections.Add<IncidentShortInfoProjection>(ProjectionLifecycle.Async);
    options.Projections.Add<IncidentHistoryTransformation>(ProjectionLifecycle.Inline);
    // options.Projections.Add(new KafkaProducer(builder.Configuration), ProjectionLifecycle.Async);
    options.Projections.Add(new RabbitMQProducer(builder.Configuration, 
        sp.GetRequiredService<ILogger<RabbitMQProducer>>()), ProjectionLifecycle.Async);
    options.Projections.Add(
        new SignalRProducer((IHubContext)sp.GetRequiredService<IHubContext<IncidentsHub>>()), ProjectionLifecycle.Async
    );

    return options;
})
// .AddSubscriptionWithServices<KafkaProducer>(ServiceLifetime.Singleton)
// .AddSubscriptionWithServices<SignalRProducer<IncidentsHub>>(ServiceLifetime.Singleton)
.UseLightweightSessions()
// async daemon is a hosted service running only for async projections
.AddAsyncDaemon(DaemonMode.Solo);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var customerIncidents = app.MapGroup("api/customers/{customerId:guid}/incidents/").WithTags("Customer");
var agentIncidents = app.MapGroup("api/agents/{agentId:guid}/incidents/").WithTags("Agent");
var incidents = app.MapGroup("api/incidents").WithTags("Incident");

customerIncidents.MapPost("",
    async(IDocumentSession session, Guid customerId, LogIncidentRequest request, CancellationToken ct) =>
    {
        var (contact, description) = request;
        var incidentId = CombGuidIdGeneration.NewGuid();

        await session.Add<IncidentEntity>(incidentId,
            Handle(new (incidentId, customerId, contact, description, customerId, DateTimeOffset.Now)), ct);
        
        return Results.Created($"api/incidents/{incidentId}", incidentId);
    }
)
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status201Created);

agentIncidents.MapPost("{incidentId:guid}/categorise", 
    async(
        IDocumentSession session,
        Guid incidentId,
        Guid agentId,
        [FromHeader(Name = "If-Match")] string eTag,
        CategoriseIncidentRequest request,
        CancellationToken ct
    ) =>
    await session.GetAnUpdate<IncidentEntity>(incidentId, ToExpectedVersion(eTag), 
        state => Handle(state, new CategoriseIncident(incidentId, request.Category, agentId)), ct)
);

agentIncidents.MapPost("{incidentId:guid}/prioritise", 
    async(
        IDocumentSession session,
        Guid incidentId,
        Guid agentId,
        [FromHeader(Name = "If-Match")] string eTag,
        PrioritiseIncidentRequest request,
        CancellationToken ct
    ) =>
    await session.GetAnUpdate<IncidentEntity>(incidentId, ToExpectedVersion(eTag), 
        state => Handle(state, new PrioritiseIncident(incidentId, request.Priority, agentId)), ct)
);

agentIncidents.MapPost("{incidentId:guid}/assign", 
    async(
        IDocumentSession session,
        Guid incidentId,
        Guid agentId,
        [FromHeader(Name = "If-Match")] string eTag,
        CancellationToken ct
    ) =>
    await session.GetAnUpdate<IncidentEntity>(incidentId, ToExpectedVersion(eTag), 
        state => Handle(state, new AssignAgentToIncident(incidentId, agentId)), ct)
);

customerIncidents.MapPost("{incidentId:guid}/responses", 
    (
        IDocumentSession session,
        Guid incidentId,
        Guid customerId,
        [FromHeader(Name = "If-Match")] string eTag,
        RecordCustomerResponseToIncidentRequest request,
        CancellationToken ct
    ) =>
    session.GetAnUpdate<IncidentEntity>(
        incidentId, ToExpectedVersion(eTag), state => Handle(state,
            new RecordCustomerResponseToIncident(incidentId,
                new IncidentResponse.FromCustomer(customerId, request.Content))), ct)
);

agentIncidents.MapPost("{incidentId:guid}/responses", 
    async(
        IDocumentSession session,
        Guid incidentId,
        Guid agentId,
        [FromHeader(Name = "If-Match")] string eTag,
        RecordAgentResponseToIncidentRequest request,
        CancellationToken ct
    ) =>
    {
        await session.GetAnUpdate<IncidentEntity>(
        incidentId, ToExpectedVersion(eTag), state => Handle(state,
            new RecordAgentResponseToIncident(incidentId,
                new IncidentResponse.FromAgent(agentId, request.Content))), ct);

        return await Task.FromResult(Results.Ok());
    }
);

agentIncidents.MapPost("{incidentId:guid}/resolve", 
    async(
        IDocumentSession session,
        Guid incidentId,
        Guid agentId,
        [FromHeader(Name = "If-Match")] string eTag,
        ResolveIncidentRequest request,
        CancellationToken ct
    ) =>
    await session.GetAnUpdate<IncidentEntity>(incidentId, ToExpectedVersion(eTag), 
        state => Handle(state, new ResolveIncident(incidentId, request.Resolution, agentId)), ct)
);

customerIncidents.MapPost("{incidentId:guid}/acknowledge", 
    async(
        IDocumentSession session,
        Guid incidentId,
        Guid customerId,
        [FromHeader(Name = "If-Match")] string eTag,
        CancellationToken ct
    ) =>
    await session.GetAnUpdate<IncidentEntity>(incidentId, ToExpectedVersion(eTag), 
        state => Handle(state, new AcknowledgeResolution(incidentId, customerId)), ct)
);

agentIncidents.MapPost("{incidentId:guid}/close", 
    async(
        IDocumentSession session,
        Guid incidentId,
        Guid agentId,
        [FromHeader(Name = "If-Match")] string eTag,
        CancellationToken ct
    ) =>
    await Task.FromResult(Results.Ok(
            session.GetAnUpdate<IncidentEntity>(incidentId, ToExpectedVersion(eTag), 
            state => Handle(state, new CloseIncident(incidentId, agentId)), ct)
    ))
);

incidents.MapGet("", 
    async(
        IQuerySession query, 
        Guid customerId, 
        [FromQuery] int? pageNo, 
        [FromQuery] int? pageSize, 
        CancellationToken ct) =>
    await query.Query<IncidentShortInfo>().Where(x => x.CustomerId == customerId)
    .ToPagedListAsync(pageNo ?? 1, pageSize ?? 10, ct)
)
.Produces(StatusCodes.Status200OK)
.Produces<IncidentShortInfo>();

incidents.MapGet("{incidentId:guid}",
    async(HttpContext context, IQuerySession query, Guid incidentId, CancellationToken ct) =>
    await query.Json.WriteById<IncidentDetail>(incidentId, context)
);

app.Run();
