using System.Text;
using Busca.Core.Analise;

namespace Busca.Core.Consulta;

/// <summary>
/// Lê a consulta que o usuário digitou.
/// </summary>
/// <remarks>
/// A sintaxe é a que as pessoas já conhecem de caixa de busca: aspas para frase
/// exata, <c>+</c> para obrigatório, <c>-</c> para excluir e <c>*</c> no fim
/// para prefixo. Nada aqui lança exceção — consulta malformada é entrada de
/// usuário, não erro de programa, e o pior que pode acontecer é o asterisco
/// solto virar texto.
/// </remarks>
public sealed class AnalisadorDeConsulta
{
    private readonly Analisador analisador;

    /// <summary>Cria o leitor com a mesma esteira usada na indexação.</summary>
    public AnalisadorDeConsulta(Analisador? analisador = null)
    {
        this.analisador = analisador ?? Analisador.Padrao;
    }

    /// <summary>Transforma o texto digitado em uma consulta.</summary>
    public Booleana Analisar(string? entrada)
    {
        var clausulas = new List<Clausula>();

        foreach (var pedaco in Separar(entrada))
        {
            var clausula = Montar(pedaco);
            if (clausula is not null)
            {
                clausulas.Add(clausula);
            }
        }

        return new Booleana(clausulas);
    }

    private Clausula? Montar(string pedaco)
    {
        var papel = Papel.Pode;
        var texto = pedaco;

        if (texto.StartsWith('+'))
        {
            papel = Papel.Deve;
            texto = texto[1..];
        }
        else if (texto.StartsWith('-'))
        {
            papel = Papel.NaoPode;
            texto = texto[1..];
        }

        if (texto.Length == 0)
        {
            return null;
        }

        if (texto.StartsWith('"'))
        {
            var frase = MontarFrase(texto.Trim('"'));
            return frase is null ? null : new Clausula(papel, frase);
        }

        if (texto.Length > 1 && texto.EndsWith('*'))
        {
            var prefixo = analisador.Prefixo(texto[..^1]);
            return prefixo.Length == 0 ? null : new Clausula(papel, new Prefixo(prefixo));
        }

        var termo = analisador.Termo(texto);
        return termo is null ? null : new Clausula(papel, new Termo(termo));
    }

    private No? MontarFrase(string conteudo)
    {
        // A frase precisa das posições originais, inclusive as das palavras de
        // parada, senão "casa de praia" casaria com "casa na praia".
        var termos = Tokenizador.Separar(conteudo)
            .Select(ocorrencia => analisador.Termo(ocorrencia.Termo))
            .ToList();

        if (termos.Count == 0 || termos.All(termo => termo is null))
        {
            return null;
        }

        if (termos.Count == 1)
        {
            return termos[0] is null ? null : new Termo(termos[0]!);
        }

        // Uma palavra de parada no meio da frase não some: ela vira um buraco
        // de uma posição, que a checagem de posições atravessa.
        var compactada = new List<string>();
        var folga = 0;

        foreach (var termo in termos)
        {
            if (termo is null)
            {
                folga++;
                continue;
            }

            compactada.Add(termo);
        }

        return compactada.Count switch
        {
            0 => null,
            1 => new Termo(compactada[0]),
            _ => new Frase(compactada, folga),
        };
    }

    private static IEnumerable<string> Separar(string? entrada)
    {
        if (string.IsNullOrWhiteSpace(entrada))
        {
            yield break;
        }

        var atual = new StringBuilder();
        var dentroDeAspas = false;

        foreach (var c in entrada)
        {
            if (c == '"')
            {
                dentroDeAspas = !dentroDeAspas;
                atual.Append(c);
                continue;
            }

            if (char.IsWhiteSpace(c) && !dentroDeAspas)
            {
                if (atual.Length > 0)
                {
                    yield return atual.ToString();
                    atual.Clear();
                }

                continue;
            }

            atual.Append(c);
        }

        if (atual.Length > 0)
        {
            yield return atual.ToString();
        }
    }
}
