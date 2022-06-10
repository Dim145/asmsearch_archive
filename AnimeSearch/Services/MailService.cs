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
        private readonly MailSettings _mailSettings;
        private readonly SmtpClient _smtp;

        public string DestMail { get => _mailSettings.To.Address; }

        public MailService(IConfiguration configRoot)
        {
            _smtp = new();

            var mailSetting = configRoot.GetSection("MailSettings");

            _mailSettings = new()
            {
                Host = mailSetting["Host"],
                Port = int.TryParse(mailSetting["Port"], out int res) ? res : -1,
                Mail = MailboxAddress.Parse(mailSetting["Mail"]),
                Password = mailSetting["Password"],
                To = MailboxAddress.Parse(mailSetting["Destination"])
            };
        }
        public async Task SendEmailAsync(MailRequest mailRequest)
        {
            MimeMessage email = new()
            {
                Sender = _mailSettings.Mail,
                Date = DateTime.Now
            };

            

            email.To.Add(_mailSettings.To);
            email.Subject = mailRequest.Subject;

            email.From.Add(InternetAddress.Parse(mailRequest.Email));

            var builder = new BodyBuilder
            {
                HtmlBody = $"<h2>Message de {mailRequest.Pseudo}</h2> <p>{mailRequest.Message}</p> <p>Répondre à {mailRequest.Email}</p>".Replace("\n", "<br />")
            };

            email.Body = builder.ToMessageBody();

            _smtp.Connect(_mailSettings.Host, _mailSettings.Port, SecureSocketOptions.Auto);
            _smtp.Authenticate(_mailSettings.Mail.Address, _mailSettings.Password);
            await _smtp.SendAsync(email);
            _smtp.Disconnect(true);
        }
    }

    public class MailSettings
    {
        public string Host { get; set; } 
        public int Port { get; set; }
        public MailboxAddress Mail { get; set; }
        public string Password { get; set; }
        public MailboxAddress To { get; set; }
    }
}
