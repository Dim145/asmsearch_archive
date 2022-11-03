using System.Dynamic;
using System.Reflection;
using System.Runtime.CompilerServices;
using AnimeSearch.Core.ViewsModel;
using FluentEmail.Core;
using Microsoft.Extensions.Configuration;

namespace AnimeSearch.Services.Mails;

public class MailService
{
    private IFluentEmail Mails { get; }

    public string DestMail { get; }

    public MailService(IConfiguration configRoot, IFluentEmail mails)
    {
        DestMail = configRoot.GetSection("MailSettings")?["Destination"];
        Mails = mails;
    }
    public async Task SendEmailAsync(MailRequest mailRequest)
    {
        mailRequest.Message = mailRequest.Message
            .Replace("<", "inférieur")
            .Replace(">", "supérieur")
            .Replace("\n", "<br/>");
        
        var mail = Mails.To(DestMail)
            .Subject(mailRequest.Subject)
            .SetFrom(mailRequest.Email)
            .UsingTemplateFromEmbedded("AnimeSearch.Services.Mails.Contact.cshtml", mailRequest, Assembly.GetAssembly(GetType()));
        
        await mail.SendAsync();
    }
}