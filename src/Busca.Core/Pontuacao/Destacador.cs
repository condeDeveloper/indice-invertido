using System.Text;
using Busca.Core.Analise;

namespace Busca.Core.Pontuacao;

/// <summary>
/// Monta o trecho que aparece embaixo do título no resultado.
/// </summary>
/// <remarks>
/// O trecho é escolhido em volta da maior concentração de termos casados, e não
/// simplesmente no começo do texto: é o pedaço que mostra por que aquele
/// documento apareceu.
/// </remarks>
public sealed class Destacador
{
    private readonly Analisador analisador;
    private readonly int tamanho;
    private readonly string abertura;
    private readonly string fechamento;

    /// <summary>Cria o destacador com as marcas e o tamanho do trecho.</summary>
    public Destacador(
        Analisador? analisador = null,
        int tamanho = 180,
        string abertura = "<mark>",
        string fechamento = "</mark>")
    {
        this.analisador = analisador ?? Analisador.Padrao;
        this.tamanho = Math.Max(40, tamanho);
        this.abertura = abertura;
        this.fechamento = fechamento;
    }

    /// <summary>Instância padrão, que marca com a tag HTML.</summary>
    public static Destacador Padrao { get; } = new();

    /// <summary>Instância que marca com asteriscos, para terminal e teste.</summary>
    public static Destacador Texto { get; } = new(abertura: "*", fechamento: "*");

    /// <summary>Monta o trecho destacado do texto para os termos informados.</summary>
    public string Destacar(string? texto, IReadOnlyCollection<string> termos)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return string.Empty;
        }

        if (termos.Count == 0)
        {
            return Cortar(texto, 0);
        }

        var procurados = termos.ToHashSet(StringComparer.Ordinal);
        var casadas = analisador.Analisar(texto)
            .Where(ocorrencia => procurados.Contains(ocorrencia.Termo))
            .ToList();

        if (casadas.Count == 0)
        {
            return Cortar(texto, 0);
        }

        var janela = MelhorJanela(casadas, texto.Length);
        var recorte = Recortar(texto, janela);

        return Marcar(texto, casadas, recorte);
    }

    private (int Inicio, int Fim) MelhorJanela(List<Ocorrencia> casadas, int comprimento)
    {
        var melhorInicio = casadas[0].Inicio;
        var melhorQuantidade = 0;

        // Duas pontas correndo pela lista de ocorrências: para cada início,
        // conta quantas cabem na janela. É linear porque nenhuma ponta volta.
        var fim = 0;

        for (var inicio = 0; inicio < casadas.Count; inicio++)
        {
            while (fim < casadas.Count && casadas[fim].Fim - casadas[inicio].Inicio <= tamanho)
            {
                fim++;
            }

            var quantidade = fim - inicio;
            if (quantidade > melhorQuantidade)
            {
                melhorQuantidade = quantidade;
                melhorInicio = casadas[inicio].Inicio;
            }
        }

        var comeco = Math.Max(0, melhorInicio - 30);
        return (comeco, Math.Min(comprimento, comeco + tamanho));
    }

    private static (int Inicio, int Fim) Recortar(string texto, (int Inicio, int Fim) janela)
    {
        var inicio = janela.Inicio;
        var fim = janela.Fim;

        // Empurra as bordas até o espaço mais próximo para não cortar palavra.
        while (inicio > 0 && !char.IsWhiteSpace(texto[inicio - 1]))
        {
            inicio--;
        }

        while (fim < texto.Length && !char.IsWhiteSpace(texto[fim]))
        {
            fim++;
        }

        return (inicio, fim);
    }

    private string Marcar(string texto, List<Ocorrencia> casadas, (int Inicio, int Fim) recorte)
    {
        var construtor = new StringBuilder();

        if (recorte.Inicio > 0)
        {
            construtor.Append("… ");
        }

        var cursor = recorte.Inicio;

        foreach (var ocorrencia in casadas)
        {
            if (ocorrencia.Inicio < cursor || ocorrencia.Fim > recorte.Fim)
            {
                continue;
            }

            construtor.Append(texto, cursor, ocorrencia.Inicio - cursor);
            construtor.Append(abertura);
            construtor.Append(texto, ocorrencia.Inicio, ocorrencia.Tamanho);
            construtor.Append(fechamento);
            cursor = ocorrencia.Fim;
        }

        construtor.Append(texto, cursor, recorte.Fim - cursor);

        if (recorte.Fim < texto.Length)
        {
            construtor.Append(" …");
        }

        return construtor.ToString().Trim();
    }

    private string Cortar(string texto, int inicio)
    {
        if (texto.Length - inicio <= tamanho)
        {
            return texto[inicio..].Trim();
        }

        var recorte = Recortar(texto, (inicio, inicio + tamanho));
        return texto[recorte.Inicio..recorte.Fim].Trim() + " …";
    }
}
