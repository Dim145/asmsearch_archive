using System.Linq.Expressions;
using System.Net.Mail;
using System.Reflection;
using AnimeSearch.Core;
using AnimeSearch.Core.Models.Api;
using AnimeSearch.Data;
using AnimeSearch.Data.Models;
using Microsoft.EntityFrameworkCore;
using Expression = AnimeSearch.Core.Extensions.Expression;

namespace AnimeSearch.Site;

public static class SiteUtils
{
    public static string[] ImgExt { get; } = { "png", "jpg", "svg", "ico", "webp", "jpeg" };
    public static int[] SUPPORTED_ERROR_CODE { get; } = { 400, 401, 403, 404, 405, 500, 4040 };
    
    public static string[] SupportedCultures { get; } = { "fr", "en" };

    public static string GetHtmlType(object type) => type switch
    {
        double or long => "number",
        TimeSpan => "time",
        string s when MailAddress.TryCreate(s, out _) => "email",
        _ => "text"
    };
    
    public static MemberInfo GetPropertyMemberInfo<T>(Expression<Func<T, object>> expression) => Expression.GetPropertyMemberInfo(expression);
}