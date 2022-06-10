using AnimeSearch.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System;
using System.Threading.Tasks;

namespace AnimeSearch.Services
{
    public class MailService
    {
        private readonly dynamic _mailSettings;
        private readonly SmtpClient _smtp;

        public MailService(IConfiguration configRoot)
        {
            _smtp = new();

            var mailSetting = configRoot.GetSection("MailSettings");

            _mailSettings = new
            {
                Host = mailSetting["Host"],
                Port = int.TryParse(mailSetting["Port"], out int res) ? res : -1,
                Mail = mailSetting["Mail"],
                Password = mailSetting["Password"],
                To = mailSetting["Destination"]
            };
        }
        public async Task SendEmailAsync(MailRequest mailRequest)
        {
            MimeMessage email = new()
            {
                Sender = MailboxAddress.Parse(_mailSettings.Mail),
                Date = DateTime.Now
            };

            

            email.To.Add(MailboxAddress.Parse(_mailSettings.To));
            email.Subject = mailRequest.Subject;

            email.From.Add(InternetAddress.Parse(mailRequest.Email));

            var builder = new BodyBuilder
            {
                HtmlBody = $"<h2>Message de {mailRequest.Pseudo}</h2> <p>{mailRequest.Message}</p> <p>Répondre à {mailRequest.Email}</p>".Replace("\n", "<br />")
            };

            email.Body = builder.ToMessageBody();

            _smtp.Connect(_mailSettings.Host, _mailSettings.Port, SecureSocketOptions.Auto);
            _smtp.Authenticate(_mailSettings.Mail, _mailSettings.Password);
            await _smtp.SendAsync(email);
            _smtp.Disconnect(true);
        }
    }
}
