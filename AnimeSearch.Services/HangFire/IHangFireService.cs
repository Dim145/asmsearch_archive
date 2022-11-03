namespace AnimeSearch.Services.HangFire;

public interface IHangFireService
{
    Task Execute();
    string GetCron();
    string GetDescription();
}