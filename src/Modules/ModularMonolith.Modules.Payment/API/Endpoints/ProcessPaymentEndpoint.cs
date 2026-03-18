using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ModularMonolith.Modules.Payment.Application.Commands;

namespace ModularMonolith.Modules.Payment.API.Endpoints;

internal static class ProcessPaymentEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/payment/process", async (
        ProcessPaymentRequest request,
        [FromServices]IValidator<ProcessPaymentRequest> validator,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken) =>
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
                return Results.ValidationProblem(validationResult.ToDictionary());

            var command = new ProcessPaymentCommand(request.UserId, request.UserEmail, request.Amount);
            var result = await mediator.Send(command, cancellationToken);

            return Results.NoContent();
        })
        .WithName("ProcessPayment")
        .WithTags("Payment")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
    }
}
