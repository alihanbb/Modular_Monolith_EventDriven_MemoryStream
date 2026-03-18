#region usings
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModularMonolith.Modules.Payment.Domain.Enums;
using ModularMonolith.Modules.Payment.Infrastructure.Persistence;
using ModularMonolith.Shared.EventBus;
using ModularMonolith.Shared.Events;
using SharedKernel;
#endregion

namespace ModularMonolith.Modules.Payment.Application.Commands;

public sealed record ProcessPaymentCommand(string UserId, string UserEmail, decimal Amount) : IRequest<ServiceResult<ProcessPaymentResult>>;

public sealed record ProcessPaymentResult(Guid OrderId);

public sealed record ProcessPaymentRequest(string UserId, string UserEmail, decimal Amount);


internal class ProcessPaymentRequestValidator : AbstractValidator<ProcessPaymentRequest>
{
    public ProcessPaymentRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId zorunludur.")
            .MaximumLength(100)
            .WithMessage("UserId en fazla 100 karakter olabilir.");

        RuleFor(x => x.UserEmail)
            .NotEmpty()
            .WithMessage("UserEmail zorunludur.")
            .EmailAddress()
            .WithMessage("Geçerli bir e-posta adresi giriniz.")
            .MaximumLength(256)
            .WithMessage("E-posta en fazla 256 karakter olabilir.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Tutar 0'dan büyük olmalıdır.")
            .LessThanOrEqualTo(1_000_000)
            .WithMessage("Tutar 1.000.000'ı geçemez.");
    }
}

internal class ProcessPaymentHandler(
    [FromKeyedServices(nameof(PaymentDbContext))] IOutboxPublisher outboxPublisher,
    PaymentDbContext dbContext,
    ILogger<ProcessPaymentHandler> logger) : IRequestHandler<ProcessPaymentCommand, ServiceResult<ProcessPaymentResult>>
{
    public async Task<ServiceResult<ProcessPaymentResult>> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        logger.LogInformation(
            "Ödeme işleme başlıyor. UserId: {UserId}, Amount: {Amount}",
            request.UserId, request.Amount);

        try
        {
            var orderId = Guid.NewGuid();

            var payment = new Domain.Entities.Payment
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                UserId = request.UserId,
                Amount = request.Amount,
                Status = PaymentStatus.Confirmed,
                ProcessedAt = DateTime.UtcNow
            };

            dbContext.Payments.Add(payment);


            await outboxPublisher.PublishAsync(new PaymentSucceededEvent
            {
                OrderId = orderId,
                UserId = request.UserId,
                UserEmail = request.UserEmail,
                Amount = request.Amount
            }, cancellationToken);
 
            await dbContext.SaveChangesAsync(cancellationToken);

              
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Ödeme kaydedildi ve outbox'a alındı. OrderId: {OrderId}, UserId: {UserId}, Amount: {Amount}",
                orderId, request.UserId, request.Amount);



            return ServiceResult<ProcessPaymentResult>.SuccessAsOk(new ProcessPaymentResult(orderId));
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogError(exception,
                "Ödeme işlenirken hata oluştu. İşlem geri alındı. UserId: {UserId}, Amount: {Amount}",
                request.UserId, request.Amount);
            throw;
        }
    }
}
