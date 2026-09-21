namespace Busca.Core.Analise;

/// <summary>
/// Palavras que aparecem em quase todo documento e por isso quase não ajudam a
/// distinguir um do outro. Tirá-las do índice economiza espaço e evita que uma
/// busca por "casa de praia" gaste tempo com "de".
/// </summary>
public sealed class ListaDeParada
{
    private static readonly string[] Portugues =
    [
        "a", "à", "ao", "aos", "aquela", "aquelas", "aquele", "aqueles", "aquilo", "as", "às",
        "até", "com", "como", "da", "das", "de", "dela", "delas", "dele", "deles", "depois",
        "do", "dos", "e", "ela", "elas", "ele", "eles", "em", "entre", "era", "eram", "essa",
        "essas", "esse", "esses", "esta", "está", "estas", "este", "estes", "eu", "foi",
        "foram", "há", "isso", "isto", "já", "lhe", "lhes", "mais", "mas", "me", "mesmo",
        "meu", "meus", "minha", "minhas", "muito", "na", "não", "nas", "nem", "no", "nos",
        "nós", "nossa", "nossas", "nosso", "nossos", "num", "numa", "o", "os", "ou", "para",
        "pela", "pelas", "pelo", "pelos", "por", "qual", "quando", "que", "quem", "se", "sem",
        "ser", "seu", "seus", "só", "sua", "suas", "também", "te", "tem", "têm", "teu", "teus",
        "tu", "tua", "tuas", "um", "uma", "umas", "uns", "você", "vocês", "à", "é",
    ];

    private readonly HashSet<string> palavras;

    /// <summary>Monta a lista a partir das palavras informadas.</summary>
    public ListaDeParada(IEnumerable<string> palavras)
    {
        ArgumentNullException.ThrowIfNull(palavras);

        this.palavras = palavras
            .Select(Normalizador.Normalizar)
            .Where(palavra => palavra.Length > 0)
            .ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>A lista padrão do português.</summary>
    public static ListaDeParada Padrao { get; } = new(Portugues);

    /// <summary>Uma lista vazia, para quando nada deve ser descartado.</summary>
    public static ListaDeParada Vazia { get; } = new([]);

    /// <summary>Quantidade de palavras na lista.</summary>
    public int Quantidade => palavras.Count;

    /// <summary>Indica se o termo deve ser descartado.</summary>
    public bool Contem(string termo) => palavras.Contains(termo);

    /// <summary>Devolve uma lista com estas palavras somadas às informadas.</summary>
    public ListaDeParada Com(params string[] extras) => new(palavras.Concat(extras));

    /// <summary>Devolve uma lista sem as palavras informadas.</summary>
    public ListaDeParada Sem(params string[] removidas)
    {
        var fora = removidas.Select(Normalizador.Normalizar).ToHashSet(StringComparer.Ordinal);
        return new ListaDeParada(palavras.Where(palavra => !fora.Contains(palavra)));
    }
}
