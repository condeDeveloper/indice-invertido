namespace Busca.Core.Analise;

/// <summary>
/// Um termo encontrado no texto, com a posição na sequência de termos e o
/// trecho original que o gerou.
/// </summary>
/// <param name="Termo">O termo já normalizado.</param>
/// <param name="Posicao">Índice do termo no documento, contando de zero.</param>
/// <param name="Inicio">Onde o trecho começa no texto original.</param>
/// <param name="Tamanho">Quantos caracteres o trecho ocupa no texto original.</param>
public readonly record struct Ocorrencia(string Termo, int Posicao, int Inicio, int Tamanho)
{
    /// <summary>Posição logo depois do trecho no texto original.</summary>
    public int Fim => Inicio + Tamanho;
}

/// <summary>
/// Quebra o texto em termos. Guardar o deslocamento de cada termo no texto
/// original é o que permite destacar o trecho encontrado depois, sem ter que
/// procurar de novo.
/// </summary>
public static class Tokenizador
{
    /// <summary>Percorre o texto devolvendo um termo por vez.</summary>
    public static IEnumerable<Ocorrencia> Separar(string? texto)
    {
        if (string.IsNullOrEmpty(texto))
        {
            yield break;
        }

        var posicao = 0;
        var i = 0;

        while (i < texto.Length)
        {
            if (!Normalizador.EhDePalavra(texto[i]))
            {
                i++;
                continue;
            }

            var inicio = i;
            while (i < texto.Length && Normalizador.EhDePalavra(texto[i]))
            {
                i++;
            }

            var bruto = texto[inicio..i];
            var normalizado = Normalizador.Normalizar(bruto);

            if (normalizado.Length > 0)
            {
                yield return new Ocorrencia(normalizado, posicao, inicio, i - inicio);
                posicao++;
            }
        }
    }
}
