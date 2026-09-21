using Busca.Core.Indice;

namespace Busca.Core.Pontuacao;

/// <summary>Um documento encontrado, com a pontuação e o trecho destacado.</summary>
public sealed record Achado
{
    /// <summary>O documento.</summary>
    public required Documento Documento { get; init; }

    /// <summary>Pontuação BM25 somada de todas as cláusulas.</summary>
    public required double Pontuacao { get; init; }

    /// <summary>Trecho do texto com os termos encontrados destacados.</summary>
    public string Trecho { get; init; } = string.Empty;

    /// <summary>Termos do índice que casaram neste documento.</summary>
    public IReadOnlyList<string> TermosCasados { get; init; } = [];
}

/// <summary>O resultado de uma busca.</summary>
/// <param name="Achados">Os documentos da página pedida, do mais para o menos relevante.</param>
/// <param name="Total">Quantos documentos casaram no total.</param>
/// <param name="Consulta">A consulta como ela foi interpretada.</param>
/// <param name="Milissegundos">Quanto tempo a busca levou.</param>
public sealed record Resultado(
    IReadOnlyList<Achado> Achados,
    int Total,
    string Consulta,
    double Milissegundos)
{
    /// <summary>Um resultado sem nenhum documento.</summary>
    public static Resultado Vazio(string consulta) => new([], 0, consulta, 0);

    /// <summary>Indica se a busca não encontrou nada.</summary>
    public bool SemResultados => Total == 0;
}
