using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using ModularMonolith.Modules.Notification.Domain.Configuration;
using ModularMonolith.Modules.Notification.Domain.Services;

namespace ModularMonolith.Modules.Notification.Infrastructure.Services;

internal sealed class EmailService(EmailSettings settings, ILogger<EmailService> logger) : IEmailService
{
    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("E-Ticaret Platformu", settings.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(settings.Host, settings.Port,
            settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
        await client.AuthenticateAsync(settings.FromEmail, settings.Password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        logger.LogInformation("Email gönderildi: {ToEmail}", toEmail);
    }

    public async Task SendPaymentConfirmationEmailAsync(
        string recipientEmail, string userId, Guid orderId, decimal amount, DateTime processedAt)
    {
        var subject = "✅ Ödeme Onayı - E-Ticaret Platformu";
        var htmlBody = $@"
<!DOCTYPE html><html><head><meta charset='UTF-8'>
<style>
  body{{font-family:'Segoe UI',sans-serif;line-height:1.6;color:#333}}
  .container{{max-width:600px;margin:0 auto;padding:20px}}
  .header{{background:linear-gradient(135deg,#667eea,#764ba2);color:white;padding:30px;border-radius:12px 12px 0 0;text-align:center}}
  .content{{background:#f9f9f9;padding:30px;border-radius:0 0 12px 12px}}
  .detail-box{{background:white;padding:20px;border-radius:8px;margin:20px 0;box-shadow:0 2px 8px rgba(0,0,0,.1)}}
  .amount{{font-size:28px;color:#667eea;font-weight:bold}}
  .highlight{{background:#e8f5e9;padding:15px;border-radius:8px;border-left:4px solid #4caf50;margin:15px 0}}
  .footer{{text-align:center;margin-top:20px;color:#666;font-size:13px;border-top:1px solid #ddd;padding-top:20px}}
</style></head>
<body><div class='container'>
  <div class='header'><h1>✅ Ödemeniz Başarılı!</h1></div>
  <div class='content'>
    <p>Merhaba <strong>{userId}</strong>,</p>
    <div class='detail-box'>
      <h3 style='color:#667eea'>📋 Sipariş Detayları</h3>
      <p><strong>Sipariş No:</strong> {orderId}</p>
      <p><strong>Ödenen Tutar:</strong> <span class='amount'>{amount:N2} ₺</span></p>
      <p><strong>İşlem Tarihi:</strong> {processedAt:dd.MM.yyyy HH:mm:ss}</p>
    </div>
    <div class='highlight'>🎁 Bu alışverişinizden sadakat puanı kazandınız!</div>
    <p>Sevgilerle,<br><strong>E-Ticaret Platformu Ekibi</strong></p>
  </div>
  <div class='footer'><p>Bu otomatik bir mesajdır. © 2024 E-Ticaret Platformu</p></div>
</div></body></html>";

        await SendEmailAsync(recipientEmail, subject, htmlBody);
    }

    public async Task SendPointsEarnedEmailAsync(
        string recipientEmail, string userId, Guid orderId,
        int pointsEarned, int totalPoints, int availablePoints, DateTime earnedAt)
    {
        var subject = "🎁 Tebrikler! Puan Kazandınız - E-Ticaret Platformu";
        var htmlBody = $@"
<!DOCTYPE html><html><head><meta charset='UTF-8'>
<style>
  body{{font-family:'Segoe UI',sans-serif;line-height:1.6;color:#333}}
  .container{{max-width:600px;margin:0 auto;padding:20px}}
  .header{{background:linear-gradient(135deg,#f093fb,#f5576c);color:white;padding:30px;border-radius:12px 12px 0 0;text-align:center}}
  .content{{background:#f9f9f9;padding:30px;border-radius:0 0 12px 12px}}
  .points-box{{background:white;padding:20px;border-radius:8px;margin:20px 0;box-shadow:0 2px 8px rgba(0,0,0,.1)}}
  .points{{font-size:36px;color:#f5576c;font-weight:bold;text-align:center}}
  .footer{{text-align:center;margin-top:20px;color:#666;font-size:13px;border-top:1px solid #ddd;padding-top:20px}}
</style></head>
<body><div class='container'>
  <div class='header'><h1>🎁 Puan Kazandınız!</h1></div>
  <div class='content'>
    <p>Merhaba <strong>{userId}</strong>,</p>
    <div class='points-box'>
      <div class='points'>+{pointsEarned} Puan</div>
      <p><strong>Sipariş No:</strong> {orderId}</p>
      <p><strong>Toplam Puan:</strong> {totalPoints}</p>
      <p><strong>Kullanılabilir Puan:</strong> {availablePoints}</p>
      <p><strong>Tarih:</strong> {earnedAt:dd.MM.yyyy HH:mm:ss}</p>
    </div>
    <p>Sevgilerle,<br><strong>E-Ticaret Platformu Ekibi</strong></p>
  </div>
  <div class='footer'><p>Bu otomatik bir mesajdır. © 2024 E-Ticaret Platformu</p></div>
</div></body></html>";

        await SendEmailAsync(recipientEmail, subject, htmlBody);
    }
}
