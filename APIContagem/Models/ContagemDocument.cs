using MongoDB.Bson;

namespace APIContagem.Models;

public class ContagemDocument
{
    public ObjectId _id { get; set; }
    public int ValorAtual { get; set; }
    public string? Producer { get; set; }
    public string? Kernel { get; set; }
    public string? Framework { get; set; }
    public string? Mensagem { get; set; }
}