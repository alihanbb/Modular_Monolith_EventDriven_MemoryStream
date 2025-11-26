using FluentValidation;
using MassTransit;
using MediatR;
using Shared.Event;

namespace PaymentService.Payment.Featrue;

public sealed record ProcessPaymentCommand(string UserId, decimal Amount) : IRequest<ProcessPaymentResult>;
public sealed record ProcessPaymentRequest(string UserId, decimal Amount);


public class ProcessPaymentRequestValidator : AbstractValidator<ProcessPaymentRequest>
{
    public ProcessPaymentRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required")
            .MaximumLength(100).WithMessage("UserId must not exceed 100 characters");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0")
            .LessThanOrEqualTo(1_000_000).WithMessage("Amount must not exceed 1,000,000");
    }
}
public sealed record ProcessPaymentResult(Guid OrderId);

public class ProcessPaymentHandler(IPublishEndpoint publishEndpoint, ILogger<ProcessPaymentHandler> logger) : IRequestHandler<ProcessPaymentCommand, ProcessPaymentResult>
{
  
    public async Task<ProcessPaymentResult> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        
        await Task.Delay(100, cancellationToken);

        var orderId = Guid.NewGuid();

 

        var paymentEvent = new PaymentSucceedEvent
        {
            OrderId = orderId,
            UserId = request.UserId,
            Amount = request.Amount,
            CreatedAt = DateTime.UtcNow
        };

        await publishEndpoint.Publish(paymentEvent, cancellationToken);
       
        logger.LogInformation(
            "Payment processed and event published. OrderId: {OrderId}, UserId: {UserId}, Amount: {Amount}",
            orderId, request.UserId, request.Amount);

        return new ProcessPaymentResult(orderId);
    }
}
