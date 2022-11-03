namespace AnimeSearch.Data.Models;

public class ApiObjectFilter
{
    public int IdApiObject { get; set; }
    public int IdApiFilter { get; set; }
    public string FieldValue { get; set; }
    
    public virtual ApiObject ApiObject { get; set; }
    public virtual ApiFilter ApiFilter { get; set; }
}