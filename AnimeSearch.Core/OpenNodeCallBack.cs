namespace AnimeSearch.Core;

public class OpenNodeCallBack
{
    public Guid Id { get; set; }
    public string Status { get; set; }
    public string Description { get; set; }
    public string Hashed_order { get; set; }
    public int Auto_settle { get; set; }
    public float Fee { get; set; }
    public float Price { get; set; }
    public string Order_id { get; set; }
}