using FluentValidation;
using MassTransit.Mediator;

namespace PaymentService.Payment.Featrue
{
    public static class ProcessPaymentEndpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapPost("/api/payment/process", async (
                ProcessPaymentRequest request,
                IValidator<ProcessPaymentRequest> validator,
                IMediator mediator) =>
            {
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }

                var command = new ProcessPaymentCommand(request.UserId, request.Amount);
                var result = mediator.Send(command);

                return Results.Ok(new
                {
                    Success = true,
                    Message = "Payment processed successfully and event published to Redis Stream"
                });
            })
            .WithName("ProcessPayment")
            .WithOpenApi()
            .Produces(200)
            .ProducesValidationProblem();
        }
    }
}
