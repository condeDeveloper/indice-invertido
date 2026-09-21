namespace Busca.Core.Pontuacao;

/// <summary>
/// A função de ordenação BM25.
/// </summary>
/// <remarks>
/// Ela resolve dois problemas que a contagem crua de termos tem. O primeiro é
/// a saturação: a décima aparição de uma palavra não vale o mesmo que a
/// segunda, e o parâmetro <c>k1</c> controla a velocidade com que o ganho
/// desaparece. O segundo é o tamanho: um documento longo tem mais chance de
/// conter qualquer palavra por acidente, e <c>b</c> controla o quanto o
/// comprimento é descontado.
/// </remarks>
public sealed class Bm25
{
    /// <summary>Cria a função com os parâmetros informados.</summary>
    public Bm25(double k1 = 1.2, double b = 0.75)
    {
        if (k1 < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(k1), "O k1 não pode ser negativo.");
        }

        if (b is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(b), "O b vai de 0 a 1.");
        }

        K1 = k1;
        B = b;
    }

    /// <summary>Instância com os valores mais usados na literatura.</summary>
    public static Bm25 Padrao { get; } = new();

    /// <summary>Controla a saturação da frequência do termo.</summary>
    public double K1 { get; }

    /// <summary>Controla o desconto pelo comprimento do documento.</summary>
    public double B { get; }

    /// <summary>
    /// Peso do termo no corpus. Quanto mais documentos contêm o termo, menos
    /// ele distingue um do outro; o +0,5 e o +1 mantêm o valor positivo mesmo
    /// quando o termo está em quase tudo.
    /// </summary>
    public static double PesoNoCorpus(int documentosComOTermo, int totalDeDocumentos)
    {
        if (totalDeDocumentos <= 0 || documentosComOTermo <= 0)
        {
            return 0;
        }

        var numerador = totalDeDocumentos - documentosComOTermo + 0.5;
        var denominador = documentosComOTermo + 0.5;

        return Math.Log(1 + (numerador / denominador));
    }

    /// <summary>Pontuação de um termo em um documento.</summary>
    public double Pontuar(int frequencia, int comprimento, double comprimentoMedio, double pesoNoCorpus)
    {
        if (frequencia <= 0 || pesoNoCorpus <= 0)
        {
            return 0;
        }

        var normalizacao = comprimentoMedio <= 0 ? 1 : comprimento / comprimentoMedio;
        var denominador = frequencia + (K1 * (1 - B + (B * normalizacao)));

        return pesoNoCorpus * (frequencia * (K1 + 1) / denominador);
    }
}
