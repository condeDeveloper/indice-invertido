namespace Busca.Core.Indice;

/// <summary>Um documento como ele entra no índice.</summary>
/// <param name="Referencia">Identificador externo, escolhido por quem indexa.</param>
/// <param name="Titulo">Título mostrado no resultado.</param>
/// <param name="Texto">Conteúdo que será analisado.</param>
public sealed record DocumentoParaIndexar(string Referencia, string Titulo, string Texto);

/// <summary>
/// Um documento já indexado. O comprimento em termos fica guardado porque o
/// BM25 precisa dele para não favorecer textos longos.
/// </summary>
public sealed record Documento
{
    /// <summary>Identificador interno, sequencial.</summary>
    public required int Id { get; init; }

    /// <summary>Identificador externo informado por quem indexou.</summary>
    public required string Referencia { get; init; }

    /// <summary>Título do documento.</summary>
    public required string Titulo { get; init; }

    /// <summary>Texto original, preservado para destacar o trecho encontrado.</summary>
    public required string Texto { get; init; }

    /// <summary>Quantidade de termos indexados.</summary>
    public int Comprimento { get; init; }

    /// <summary>Quantidade de termos distintos.</summary>
    public int TermosDistintos { get; init; }
}
