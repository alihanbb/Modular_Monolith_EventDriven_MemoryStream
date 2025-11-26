using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using NotificationService.Email.Configuration;

namespace NotificationService.Email.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(EmailSettings emailSettings, ILogger<EmailService> logger)
    {
        _emailSettings = emailSettings;
        _logger = logger;
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("E-Ticaret Platformu", _emailSettings.FromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            
            await client.ConnectAsync(_emailSettings.Host, _emailSettings.Port, 
                _emailSettings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
            
            // Gmail için kullanıcı adı FromEmail ile aynı
            await client.AuthenticateAsync(_emailSettings.FromEmail, _emailSettings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email başarıyla gönderildi: {ToEmail}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email gönderimi başarısız: {ToEmail}", toEmail);
            throw;
        }
    }

    public async Task SendPaymentConfirmationEmailAsync(
        string recipientEmail,
        string userId,
        Guid orderId,
        decimal amount,
        DateTime processedAt)
    {
        var subject = "✅ Ödeme Onayı - E-Ticaret Platformu";
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; border-radius: 12px 12px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 12px 12px; }}
        .order-details {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }}
        .amount {{ font-size: 28px; color: #667eea; font-weight: bold; }}
        .success-icon {{ font-size: 54px; text-align: center; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 13px; border-top: 1px solid #ddd; padding-top: 20px; }}
        .highlight {{ background: #e8f5e9; padding: 15px; border-radius: 8px; border-left: 4px solid #4caf50; margin: 15px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='success-icon'>✅</div>
            <h1 style='text-align: center; margin: 15px 0 5px 0;'>Ödemeniz Başarılı!</h1>
            <p style='text-align: center; margin: 5px 0 0 0; opacity: 0.9;'>İşleminiz güvenle tamamlandı</p>
        </div>
        <div class='content'>
            <p>Merhaba <strong>{userId}</strong>,</p>
            <p>Ödemeniz başarıyla işleme alındı. Alışverişiniz için teşekkür ederiz!</p>
            
            <div class='order-details'>
                <h3 style='margin-top: 0; color: #667eea;'>📋 Sipariş Detayları</h3>
                <p><strong>Sipariş No:</strong> {orderId}</p>
                <p><strong>Kullanıcı ID:</strong> {userId}</p>
                <p><strong>Ödenen Tutar:</strong> <span class='amount'>{amount:N2} ₺</span></p>
                <p><strong>İşlem Tarihi:</strong> {processedAt:dd.MM.yyyy HH:mm:ss}</p>
            </div>
            
            <div class='highlight'>
                <p style='margin: 0;'>🎁 <strong>Müjde!</strong> Bu alışverişinizden sadakat puanı kazandınız. Hesabınızı kontrol ederek puan bakiyenizi görebilirsiniz.</p>
            </div>
            
            <p>Herhangi bir sorunuz olması durumunda destek ekibimizle iletişime geçmekten çekinmeyin.</p>
            <p style='margin-top: 25px;'>Sevgilerle,<br><strong>E-Ticaret Platformu Ekibi</strong></p>
        </div>
        <div class='footer'>
            <p>Bu otomatik bir mesajdır. Lütfen bu e-postaya yanıt vermeyiniz.</p>
            <p style='margin-top: 10px;'>© 2024 E-Ticaret Platformu. Tüm hakları saklıdır.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(recipientEmail, subject, htmlBody);
    }

    public async Task SendPaymentFailedEmailAsync(
        string recipientEmail,
        string userId,
        Guid orderId,
        decimal amount,
        string reason,
        DateTime failedAt)
    {
        var subject = "❌ Ödeme Başarısız - E-Ticaret Platformu";
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); color: white; padding: 30px; border-radius: 12px 12px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 12px 12px; }}
        .order-details {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #f5576c; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }}
        .amount {{ font-size: 28px; color: #f5576c; font-weight: bold; }}
        .error-icon {{ font-size: 54px; text-align: center; }}
        .reason-box {{ background: #fff3cd; border: 1px solid #ffc107; padding: 20px; border-radius: 8px; margin: 20px 0; }}
        .action-button {{ display: inline-block; background: #667eea; color: white; padding: 14px 28px; text-decoration: none; border-radius: 6px; margin: 15px 0; font-weight: bold; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 13px; border-top: 1px solid #ddd; padding-top: 20px; }}
        ul {{ padding-left: 20px; }}
        ul li {{ margin: 8px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='error-icon'>❌</div>
            <h1 style='text-align: center; margin: 15px 0 5px 0;'>Ödeme Başarısız</h1>
            <p style='text-align: center; margin: 5px 0 0 0; opacity: 0.9;'>İşleminiz tamamlanamadı</p>
        </div>
        <div class='content'>
            <p>Merhaba <strong>{userId}</strong>,</p>
            <p>Maalesef ödemenizi işleme alamadık. Lütfen aşağıdaki detayları inceleyin.</p>
            
            <div class='order-details'>
                <h3 style='margin-top: 0; color: #f5576c;'>📋 İşlem Detayları</h3>
                <p><strong>Sipariş No:</strong> {orderId}</p>
                <p><strong>Kullanıcı ID:</strong> {userId}</p>
                <p><strong>Tutar:</strong> <span class='amount'>{amount:N2} ₺</span></p>
                <p><strong>Başarısız Olma Tarihi:</strong> {failedAt:dd.MM.yyyy HH:mm:ss}</p>
            </div>

            <div class='reason-box'>
                <h4 style='margin-top: 0;'>⚠️ Hata Nedeni:</h4>
                <p style='margin-bottom: 0; font-size: 15px;'>{reason}</p>
            </div>
            
            <h3>🔧 Ne Yapmalısınız?</h3>
            <ul>
                <li>Ödeme bilgilerinizin doğru olduğundan emin olun</li>
                <li>Hesabınızda yeterli bakiye bulunduğundan emin olun</li>
                <li>Bankanızla iletişime geçerek herhangi bir sorun olup olmadığını kontrol edin</li>
                <li>Farklı bir ödeme yöntemi deneyebilirsiniz</li>
            </ul>

            <p style='text-align: center;'>
                <a href='#' class='action-button'>Tekrar Dene</a>
            </p>
            
            <p>Sorun devam ederse lütfen destek ekibimizle iletişime geçin. Size yardımcı olmaktan mutluluk duyarız.</p>
            <p style='margin-top: 25px;'>Sevgilerle,<br><strong>E-Ticaret Platformu Ekibi</strong></p>
        </div>
        <div class='footer'>
            <p>Bu otomatik bir mesajdır. Lütfen bu e-postaya yanıt vermeyiniz.</p>
            <p style='margin-top: 10px;'>Destek: destek@ecommerce.com | Tel: 0850 XXX XX XX</p>
            <p style='margin-top: 5px;'>© 2024 E-Ticaret Platformu. Tüm hakları saklıdır.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(recipientEmail, subject, htmlBody);
    }

    public async Task SendPointsEarnedEmailAsync(
        string recipientEmail,
        string userId,
        Guid orderId,
        int pointsEarned,
        int totalPoints,
        int availablePoints,
        DateTime earnedAt)
    {
        var subject = "🎁 Tebrikler! Puan Kazandınız - E-Ticaret Platformu";
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); color: white; padding: 30px; border-radius: 12px 12px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 12px 12px; }}
        .points-card {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 35px; border-radius: 12px; margin: 25px 0; text-align: center; color: white; box-shadow: 0 4px 15px rgba(102, 126, 234, 0.4); }}
        .big-points {{ font-size: 56px; font-weight: bold; margin: 10px 0; text-shadow: 2px 2px 4px rgba(0,0,0,0.2); }}
        .points-label {{ font-size: 20px; letter-spacing: 1px; opacity: 0.95; }}
        .stats-row {{ display: flex; justify-content: space-around; margin: 25px 0; }}
        .stat-box {{ text-align: center; padding: 20px; background: white; border-radius: 10px; flex: 1; margin: 0 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }}
        .stat-number {{ font-size: 32px; color: #667eea; font-weight: bold; }}
        .stat-label {{ font-size: 13px; color: #666; margin-top: 8px; text-transform: uppercase; letter-spacing: 0.5px; }}
        .celebration-icon {{ font-size: 72px; text-align: center; margin: 15px 0; animation: bounce 1s infinite; }}
        @keyframes bounce {{ 0%, 100% {{ transform: translateY(0); }} 50% {{ transform: translateY(-10px); }} }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 13px; border-top: 1px solid #ddd; padding-top: 20px; }}
        .benefit-box {{ background: linear-gradient(to right, #e8f5e9, #c8e6c9); border-left: 5px solid #4caf50; padding: 20px; margin: 20px 0; border-radius: 8px; }}
        ul {{ padding-left: 20px; }}
        ul li {{ margin: 10px 0; color: #2e7d32; font-weight: 500; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='celebration-icon'>🎉</div>
            <h1 style='text-align: center; margin: 15px 0 5px 0;'>Puan Kazandınız!</h1>
            <p style='text-align: center; margin: 5px 0 0 0; opacity: 0.9;'>Sadakat programımızdan ödüllendirildiniz</p>
        </div>
        <div class='content'>
            <p>Merhaba <strong>{userId}</strong>,</p>
            <p>Harika haber! Son alışverişinizden <strong>sadakat puanı</strong> kazandınız! 🎊</p>
            
            <div class='points-card'>
                <div class='big-points'>+{pointsEarned}</div>
                <div class='points-label'>PUAN KAZANDINIZ 🎁</div>
            </div>

            <div class='stats-row'>
                <div class='stat-box'>
                    <div class='stat-number'>{totalPoints}</div>
                    <div class='stat-label'>Toplam Puan</div>
                </div>
                <div class='stat-box'>
                    <div class='stat-number'>{availablePoints}</div>
                    <div class='stat-label'>Kullanılabilir</div>
                </div>
            </div>

            <div class='benefit-box'>
                <h4 style='margin-top: 0; color: #2e7d32;'>💡 Puanlarınızı Nasıl Kullanabilirsiniz?</h4>
                <ul style='margin: 15px 0;'>
                    <li>1 puan = 0,10 ₺ indirim</li>
                    <li>Minimum 10 puan ile kullanmaya başlayabilirsiniz</li>
                    <li>Sipariş tutarınızın %50'sine kadar kullanabilirsiniz</li>
                    <li>Puanlarınız 1 yıl süreyle geçerlidir</li>
                </ul>
            </div>

            <p style='background: white; padding: 15px; border-radius: 8px; margin: 20px 0;'><strong>📦 İşlem Detayları:</strong></p>
            <ul style='background: white; padding: 20px 20px 20px 40px; border-radius: 8px; margin: 5px 0 20px 0;'>
                <li style='color: #333; font-weight: normal;'><strong>Sipariş No:</strong> {orderId}</li>
                <li style='color: #333; font-weight: normal;'><strong>Kazanma Tarihi:</strong> {earnedAt:dd.MM.yyyy HH:mm:ss}</li>
            </ul>
            
            <p style='text-align: center; margin-top: 35px; font-size: 17px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; border-radius: 8px;'>
                <strong>Alışverişe devam ederek daha fazla puan kazanın! 🛍️</strong>
            </p>

            <p style='margin-top: 25px;'>Sevgilerle,<br><strong>E-Ticaret Platformu Ekibi</strong></p>
        </div>
        <div class='footer'>
            <p>Bu otomatik bir mesajdır. Lütfen bu e-postaya yanıt vermeyiniz.</p>
            <p style='margin-top: 10px;'>© 2024 E-Ticaret Platformu. Tüm hakları saklıdır.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(recipientEmail, subject, htmlBody);
    }
}
