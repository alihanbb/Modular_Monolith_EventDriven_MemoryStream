using Loyalty;
using LoyaltyService.Loyalty;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Event;
using System;
using System.Threading.Tasks;

namespace LoyaltyService.Consumer;

public class PaymentSuccessConsumer : IConsumer<PaymentSucceedEvent>
{
    private readonly ILogger<PaymentSuccessConsumer> _logger;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly LoyaltyDbContext _dbContext;

    public PaymentSuccessConsumer(
        ILogger<PaymentSuccessConsumer> logger,
        IPublishEndpoint publishEndpoint,
        LoyaltyDbContext dbContext)
    {
        _logger = logger;
        _publishEndpoint = publishEndpoint;
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<PaymentSucceedEvent> context)
    {
        var message = context.Message;
        if (message == null)
        {
            _logger.LogWarning("Received null PaymentSucceedEvent message");
            return;
        }

        _logger.LogInformation(
            "Processing payment for UserId: {UserId}, OrderId: {OrderId}, Amount: {Amount}",
            message.UserId, message.OrderId, message.Amount);

        try
        {
            var pointsToAdd = CalculatePoints(message.Amount);

            _logger.LogInformation(
                "Calculated {Points} points from amount {Amount} for UserId: {UserId}",
                pointsToAdd, message.Amount, message.UserId);

            var userPoints = await _dbContext.UserPoints
                .FirstOrDefaultAsync(up => up.UserId == message.UserId);

            if (userPoints == null)
            {
                userPoints = new UserPoints
                {
                    Id = Guid.NewGuid(),
                    UserId = message.UserId,
                    TotalPoints = pointsToAdd,
                    AvailablePoints = pointsToAdd,
                    UsedPoints = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _dbContext.UserPoints.Add(userPoints);
                _logger.LogInformation(
                    "Created new UserPoints record for UserId: {UserId}",
                    message.UserId);
            }
            else
            {
                userPoints.TotalPoints += pointsToAdd;
                userPoints.AvailablePoints += pointsToAdd;
                userPoints.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Updated UserPoints for UserId: {UserId}. New Total: {Total}, Available: {Available}",
                    message.UserId, userPoints.TotalPoints, userPoints.AvailablePoints);
            }
            await _dbContext.SaveChangesAsync();

            var pointsAddedEvent = new PointsAddedEvent
            {
                Id = Guid.NewGuid(),
                UserId = message.UserId,
                OrderId = message.OrderId,
                PointsAdded = pointsToAdd,
                TotalPoints = userPoints.TotalPoints,
                AvailablePoints = userPoints.AvailablePoints,
                CreatedAt = DateTime.UtcNow
            };

            await _publishEndpoint.Publish(pointsAddedEvent);

            _logger.LogInformation(
                "Successfully published PointsAddedEvent for UserId: {UserId}, Points: {Points}",
                message.UserId, pointsToAdd);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing points for UserId: {UserId}, OrderId: {OrderId}",
                message.UserId, message.OrderId);
            throw;
        }
    }

    
    private static int CalculatePoints(decimal amount)
    {
        var points = amount * 0.10m;
        return (int)Math.Round(points, MidpointRounding.AwayFromZero);
    }

    
}



