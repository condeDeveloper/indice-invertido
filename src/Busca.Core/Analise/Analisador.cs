namespace Busca.Core.Analise;

/// <summary>
/// A esteira que transforma texto em termos de índice: separa, descarta
/// palavra de parada e radicaliza.
/// </summary>
/// <remarks>
/// O mesmo analisador precisa ser usado na indexação e na consulta. Se um lado
/// radicalizar e o outro não, a busca por "trabalhando" nunca acha o documento
/// que foi indexado como "trabalh".
/// </remarks>
public sealed class Analisador
{
    private readonly ListaDeParada paradas;
    private readonly Radicalizador radicalizador;
    private readonly int tamanhoMinimo;

    /// <summary>Monta a esteira com as peças informadas.</summary>
    public Analisador(
        ListaDeParada? paradas = null,
        Radicalizador? radicalizador = null,
        int tamanhoMinimo = 1)
    {
        this.paradas = paradas ?? ListaDeParada.Padrao;
        this.radicalizador = radicalizador ?? Radicalizador.Padrao;
        this.tamanhoMinimo = tamanhoMinimo;
    }

    /// <summary>Esteira padrão: paradas do português e radicalização ligada.</summary>
    public static Analisador Padrao { get; } = new();

    /// <summary>Esteira que só normaliza, útil para comparar resultados.</summary>
    public static Analisador Simples { get; } = new(ListaDeParada.Vazia, Radicalizador.Desligado);

    /// <summary>
    /// Analisa o texto preservando a posição de cada termo. A posição conta os
    /// termos descartados, senão a busca por frase casaria "casa praia" com
    /// "casa de praia" só porque o "de" sumiu.
    /// </summary>
    public IEnumerable<Ocorrencia> Analisar(string? texto)
    {
        foreach (var bruta in Tokenizador.Separar(texto))
        {
            if (bruta.Termo.Length < tamanhoMinimo || paradas.Contem(bruta.Termo))
            {
                continue;
            }

            yield return bruta with { Termo = radicalizador.Radicalizar(bruta.Termo) };
        }
    }

    /// <summary>Só os termos, sem posição.</summary>
    public List<string> Termos(string? texto) => Analisar(texto).Select(o => o.Termo).ToList();

    /// <summary>
    /// Analisa um termo isolado, como o que veio de uma consulta. Passar pelo
    /// tokenizador aqui também é o que faz pontuação solta — um "*" perdido,
    /// por exemplo — não virar termo de busca.
    /// </summary>
    public string? Termo(string? palavra)
    {
        var primeira = Tokenizador.Separar(palavra).FirstOrDefault().Termo;

        if (string.IsNullOrEmpty(primeira) || primeira.Length < tamanhoMinimo || paradas.Contem(primeira))
        {
            return null;
        }

        return radicalizador.Radicalizar(primeira);
    }

    /// <summary>
    /// Normaliza um prefixo de busca sem radicalizar. Radicalizar prefixo não
    /// faz sentido: "program*" viraria "program" ou coisa pior, e deixaria de
    /// casar com os termos que realmente começam assim.
    /// </summary>
    public string Prefixo(string? palavra)
        => Tokenizador.Separar(palavra).FirstOrDefault().Termo ?? string.Empty;
}
