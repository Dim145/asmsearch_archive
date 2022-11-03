using AnimeSearch.Core.Models.Api;

namespace AnimeSearch.Data.Models;

public class ApiObjectSort
{
    public int IdApiObject { get; set; }
    public int IdApiSort { get; set; }
    public string FieldValue { get; set; }
    
    public virtual ApiObject ApiObject { get; set; }
    public virtual ApiSort ApiSort { get; set; }
}