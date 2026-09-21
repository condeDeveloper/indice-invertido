using Busca.Core.Indice;
using Busca.Core.Pontuacao;

namespace Busca.Api;

/// <summary>Documento enviado para indexação.</summary>
/// <param name="Referencia">Identificador externo do documento.</param>
/// <param name="Titulo">Título do documento.</param>
/// <param name="Texto">Conteúdo a indexar.</param>
public sealed record PedidoDeIndexacao(string Referencia, string Titulo, string Texto)
{
    /// <summary>Converte para o modelo do núcleo.</summary>
    public DocumentoParaIndexar ParaNucleo() => new(Referencia, Titulo, Texto);
}

/// <summary>Lote de documentos enviados de uma vez.</summary>
/// <param name="Documentos">Os documentos do lote.</param>
public sealed record PedidoDeLote(IReadOnlyList<PedidoDeIndexacao> Documentos);

/// <summary>Um documento no resultado da busca.</summary>
public sealed record AchadoEmResposta(
    string Referencia,
    string Titulo,
    double Pontuacao,
    string Trecho)
{
    /// <summary>Monta a resposta a partir do modelo.</summary>
    public static AchadoEmResposta De(Achado achado) => new(
        achado.Documento.Referencia,
        achado.Documento.Titulo,
        achado.Pontuacao,
        achado.Trecho);
}

/// <summary>A resposta de uma busca.</summary>
public sealed record RespostaDaBusca(
    string Consulta,
    string Interpretacao,
    int Total,
    double Milissegundos,
    IReadOnlyList<AchadoEmResposta> Achados)
{
    /// <summary>Monta a resposta a partir do resultado do motor.</summary>
    public static RespostaDaBusca De(Resultado resultado, string interpretacao) => new(
        resultado.Consulta,
        interpretacao,
        resultado.Total,
        Math.Round(resultado.Milissegundos, 3),
        resultado.Achados.Select(AchadoEmResposta.De).ToList());
}

/// <summary>Estatísticas do índice.</summary>
public sealed record Estatisticas(int Documentos, int Termos, double ComprimentoMedio, double K1, double B);
