namespace Busca.Core.Analise;

/// <summary>
/// Reduz a palavra ao radical, para que "carro" e "carros", "trabalhar" e
/// "trabalhou" caiam no mesmo termo do índice.
/// </summary>
/// <remarks>
/// É um subconjunto do RSLP, o algoritmo de Viviane Orengo para o português.
/// A ordem das etapas é a do artigo original — plural, feminino, advérbio,
/// aumentativo, substantivo, verbo e vogal final — e ela importa: "balões"
/// precisa virar "balão" antes da etapa que corta o "ão", senão a palavra no
/// plural para em um radical diferente do da palavra no singular. Cada regra
/// só se aplica se o que sobra tiver tamanho mínimo, que é o que evita
/// transformar "mesa" em "m". Não é a tabela completa do RSLP: as regras aqui
/// são as de maior cobertura.
/// </remarks>
public sealed class Radicalizador
{
    private sealed record Regra(string Sufixo, int TamanhoMinimo, string Substituto = "", string[]? Excecoes = null)
    {
        public bool Casa(string palavra)
        {
            if (!palavra.EndsWith(Sufixo, StringComparison.Ordinal))
            {
                return false;
            }

            if (palavra.Length - Sufixo.Length < TamanhoMinimo)
            {
                return false;
            }

            return Excecoes is null || !Excecoes.Contains(palavra, StringComparer.Ordinal);
        }

        public string Aplicar(string palavra) => palavra[..^Sufixo.Length] + Substituto;
    }

    private static readonly Regra[] Adverbio =
    [
        new("mente", 4, Excecoes: ["experimente"]),
    ];

    private static readonly Regra[] Aumentativo =
    [
        new("issimo", 3), new("issima", 3), new("zinho", 3), new("zinha", 3),
        new("quinho", 4, "c"), new("quinha", 4, "c"),
        new("inho", 3, Excecoes: ["caminho", "cominho", "vizinho"]),
        new("inha", 3, Excecoes: ["rainha", "linha", "minha"]),
        new("zao", 3), new("zona", 3),
        new("ao", 3, Excecoes: ["aviao", "camarao", "chimarrao", "cidadao", "coracao", "irmao", "leao", "orgao", "pao", "razao", "sao", "verao"]),
        new("ona", 3, Excecoes: ["abandona", "lona", "iona", "cortisona", "monotona", "maratona", "persona"]),
        new("arao", 3, Excecoes: ["camarao"]),
        new("alhao", 4), new("eirao", 4), new("aca", 3), new("aco", 3),
    ];

    private static readonly Regra[] Feminino =
    [
        new("ona", 3, "ao"), new("ora", 3, "or"), new("eira", 3, "eiro"),
        new("esa", 3, "es"), new("osa", 3, "oso"), new("ica", 3, "ico"),
        new("ida", 3, "ido"), new("ada", 3, "ado", ["pitada"]),
        new("iva", 3, "ivo"), new("inha", 3, "inho"),
    ];

    private static readonly Regra[] Plural =
    [
        new("ns", 1, "m"), new("oes", 3, "ao"), new("aes", 1, "ao", ["maes"]),
        new("aos", 1, "ao"), new("ais", 1, "al", ["cais", "mais"]),
        new("eis", 2, "el"), new("ies", 2, "il"), new("is", 2, "il", ["lapis", "cais", "mais", "crucifis", "tenis"]),
        new("les", 2, "l"), new("res", 3, "r", ["ingles", "pires"]),
        new("s", 2, Excecoes: ["aliais", "atras", "atraves", "gas", "gras", "lapis", "pires", "virus"]),
    ];

    private static readonly Regra[] Substantivo =
    [
        new("amentos", 3), new("imentos", 3), new("amento", 3), new("imento", 3),
        new("adores", 3), new("adoras", 3), new("ancas", 3), new("encias", 4),
        new("dades", 3), new("ismos", 3), new("istas", 3),
        new("agem", 3), new("ador", 3), new("adora", 3), new("aria", 3),
        new("edor", 3), new("eiro", 3), new("eira", 3),
        new("ancia", 3), new("encia", 4), new("dade", 3), new("eza", 3),
        new("ismo", 3), new("ista", 3), new("ivo", 4), new("iva", 4),
        new("oso", 3), new("osa", 3), new("ura", 4), new("ncia", 3),
        new("acao", 3, Excecoes: ["macao", "nacao"]), new("icao", 3),
        new("cao", 4), new("ez", 4), new("al", 4, Excecoes: ["animal", "manual", "canal", "natural", "fiscal", "total", "local", "legal"]),
    ];

    private static readonly Regra[] Verbo =
    [
        new("ariamos", 2), new("eriamos", 2), new("iriamos", 3),
        new("assemos", 2), new("essemos", 2), new("issemos", 3),
        new("aremos", 2), new("eremos", 2), new("iremos", 3),
        new("avamos", 2), new("iramos", 3), new("aramos", 2),
        new("ariam", 2), new("eriam", 2), new("iriam", 3),
        new("assem", 2), new("essem", 2), new("issem", 3),
        new("ando", 2), new("endo", 2), new("indo", 3),
        new("arao", 2), new("erao", 2), new("irao", 3),
        new("adas", 2), new("idas", 3), new("aram", 2), new("eram", 2), new("iram", 3),
        new("avam", 2), new("arem", 2), new("erem", 2), new("irem", 3),
        new("amos", 2), new("emos", 2), new("imos", 3),
        new("aria", 2), new("eria", 2), new("iria", 3),
        new("asse", 2), new("esse", 2), new("isse", 3),
        new("aste", 2), new("este", 3), new("iste", 3),
        new("arei", 2), new("erei", 2), new("irei", 3),
        new("ava", 2), new("ara", 2), new("era", 3), new("ira", 3),
        new("ear", 4), new("ar", 2, Excecoes: ["lugar"]), new("er", 2), new("ir", 3),
        new("am", 2), new("em", 2), new("ei", 3), new("eu", 3), new("iu", 3), new("ou", 3),

        // "ada" e "ida" ficam de fora: são particípios femininos, já tratados
        // na etapa do feminino, e aqui só serviriam para comer palavra viva —
        // "lápis" virava "lap" por causa de uma regra dessas.
    ];

    private readonly bool ativo;

    /// <summary>Cria o radicalizador, que pode ser desligado para comparação.</summary>
    public Radicalizador(bool ativo = true)
    {
        this.ativo = ativo;
    }

    /// <summary>Instância padrão, com todas as etapas ligadas.</summary>
    public static Radicalizador Padrao { get; } = new();

    /// <summary>Instância que devolve a palavra como veio.</summary>
    public static Radicalizador Desligado { get; } = new(ativo: false);

    /// <summary>Reduz a palavra ao radical.</summary>
    public string Radicalizar(string palavra)
    {
        if (!ativo || string.IsNullOrEmpty(palavra) || palavra.Length <= 2)
        {
            return palavra ?? string.Empty;
        }

        if (palavra.Any(char.IsDigit))
        {
            return palavra;
        }

        var atual = palavra;

        if (atual.EndsWith('s'))
        {
            Tentar(Plural, ref atual);
        }

        Tentar(Feminino, ref atual);
        Tentar(Adverbio, ref atual);
        Tentar(Aumentativo, ref atual);

        if (!Tentar(Substantivo, ref atual))
        {
            Tentar(Verbo, ref atual);
        }

        return RemoverVogalFinal(atual);
    }

    private static bool Tentar(Regra[] regras, ref string palavra)
    {
        foreach (var regra in regras)
        {
            if (regra.Casa(palavra))
            {
                palavra = regra.Aplicar(palavra);
                return true;
            }
        }

        return false;
    }

    private static string RemoverVogalFinal(string palavra)
    {
        if (palavra.Length > 3 && palavra[^1] is 'a' or 'e' or 'o')
        {
            return palavra[..^1];
        }

        return palavra;
    }
}
