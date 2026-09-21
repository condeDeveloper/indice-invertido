namespace Busca.Core.Consulta;

/// <summary>Como uma cláusula participa do resultado.</summary>
public enum Papel
{
    /// <summary>Contribui para a pontuação, mas não é obrigatória.</summary>
    Pode,

    /// <summary>O documento precisa atender a ela.</summary>
    Deve,

    /// <summary>O documento é descartado se atender a ela.</summary>
    NaoPode,
}

/// <summary>Raiz da árvore de uma consulta.</summary>
public abstract record No;

/// <summary>Um termo simples, já analisado.</summary>
/// <param name="Texto">O termo como está no índice.</param>
public sealed record Termo(string Texto) : No
{
    /// <inheritdoc />
    public override string ToString() => Texto;
}

/// <summary>Uma sequência exata de termos.</summary>
/// <param name="Termos">Os termos, na ordem em que precisam aparecer.</param>
/// <param name="Folga">Quantas posições de distância a mais são toleradas.</param>
public sealed record Frase(IReadOnlyList<string> Termos, int Folga = 0) : No
{
    /// <inheritdoc />
    public override string ToString() => $"\"{string.Join(' ', Termos)}\"";
}

/// <summary>Tudo que começa com um prefixo.</summary>
/// <param name="Inicio">O começo do termo, sem o asterisco.</param>
public sealed record Prefixo(string Inicio) : No
{
    /// <inheritdoc />
    public override string ToString() => $"{Inicio}*";
}

/// <summary>Uma cláusula dentro de uma consulta booleana.</summary>
/// <param name="Papel">Se ela é obrigatória, opcional ou proibida.</param>
/// <param name="No">A subconsulta.</param>
public sealed record Clausula(Papel Papel, No No)
{
    /// <inheritdoc />
    public override string ToString() => Papel switch
    {
        Papel.Deve => $"+{No}",
        Papel.NaoPode => $"-{No}",
        _ => No.ToString() ?? string.Empty,
    };
}

/// <summary>
/// Um conjunto de cláusulas. É o mesmo modelo do Lucene: em vez de uma árvore
/// só de E e OU, cada cláusula diz o papel que cumpre, o que descreve
/// diretamente o que se digita — <c>+java -script web</c>.
/// </summary>
/// <param name="Clausulas">As cláusulas que compõem a consulta.</param>
public sealed record Booleana(IReadOnlyList<Clausula> Clausulas) : No
{
    /// <summary>Monta uma consulta em que todas as cláusulas são obrigatórias.</summary>
    public static Booleana Todas(params No[] nos)
        => new(nos.Select(no => new Clausula(Papel.Deve, no)).ToList());

    /// <summary>Monta uma consulta em que qualquer cláusula serve.</summary>
    public static Booleana Qualquer(params No[] nos)
        => new(nos.Select(no => new Clausula(Papel.Pode, no)).ToList());

    /// <summary>As cláusulas de um papel específico.</summary>
    public IEnumerable<No> Do(Papel papel)
        => Clausulas.Where(clausula => clausula.Papel == papel).Select(clausula => clausula.No);

    /// <summary>Indica se a consulta não tem nenhuma cláusula.</summary>
    public bool Vazia => Clausulas.Count == 0;

    /// <inheritdoc />
    public override string ToString() => string.Join(' ', Clausulas);
}
