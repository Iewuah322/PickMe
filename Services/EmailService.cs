using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;
using System.Windows;
using MailKit.Security;

namespace TaxiWPF.Services
{
    public class EmailService
    {
        // Учетные данные для подключения к SMTP-серверу
        private const string FromEmail = "tyttagame@gmail.com";
        private const string FromName = "TaxiWPF Support";
        private const string AppPassword = "diyu ukmt ccfr iugs"; // 16-значный пароль приложений
        private const string SmtpServer = "smtp.gmail.com";
        private const int SmtpPort = 587;
        public async Task SendPasswordResetEmailAsync(string toEmail, string token)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(FromName, FromEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = "Восстановление пароля в TaxiWPF";
            message.Body = new TextPart("plain")
            {
                Text = $"Здравствуйте!\n\nВаш 6-значный код для сброса пароля:\n\n{token}\n\nЕсли вы не запрашивали сброс пароля, просто проигнорируйте это письмо."
            };

            try
            {
                using (var client = new SmtpClient())
                {
                    // Подключение, аутентификация и отправка
                    await client.ConnectAsync(SmtpServer, SmtpPort, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(FromEmail, AppPassword);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                    MessageBox.Show("Письмо для сброса пароля успешно отправлено!", "Успех");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при отправке письма: " + ex.Message, "Ошибка Email");
            }
        }
    }
}
