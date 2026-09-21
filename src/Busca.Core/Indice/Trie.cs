namespace Busca.Core.Indice;

/// <summary>
/// Árvore de prefixos sobre o vocabulário do índice.
/// </summary>
/// <remarks>
/// O dicionário de termos resolve a busca exata, mas não a busca por prefixo:
/// achar tudo que começa com "program" em um <c>Dictionary</c> obriga a varrer
/// o vocabulário inteiro. Na trie o custo é o tamanho do prefixo mais a
/// quantidade de termos que casam.
/// </remarks>
public sealed class Trie
{
    private sealed class No
    {
        public Dictionary<char, No>? Filhos { get; set; }

        public bool Final { get; set; }

        public No Obter(char c)
        {
            Filhos ??= [];

            if (!Filhos.TryGetValue(c, out var filho))
            {
                filho = new No();
                Filhos[c] = filho;
            }

            return filho;
        }
    }

    private readonly No raiz = new();

    /// <summary>Quantidade de termos guardados.</summary>
    public int Quantidade { get; private set; }

    /// <summary>Insere um termo. Inserir duas vezes não muda nada.</summary>
    public bool Inserir(string termo)
    {
        if (string.IsNullOrEmpty(termo))
        {
            return false;
        }

        var atual = raiz;
        foreach (var c in termo)
        {
            atual = atual.Obter(c);
        }

        if (atual.Final)
        {
            return false;
        }

        atual.Final = true;
        Quantidade++;
        return true;
    }

    /// <summary>Indica se o termo exato está guardado.</summary>
    public bool Contem(string termo)
    {
        var no = Descer(termo);
        return no is not null && no.Final;
    }

    /// <summary>Indica se algum termo começa com o prefixo.</summary>
    public bool TemPrefixo(string prefixo) => Descer(prefixo) is not null;

    /// <summary>
    /// Todos os termos que começam com o prefixo, em ordem alfabética. Um
    /// prefixo vazio devolve o vocabulário inteiro.
    /// </summary>
    public IEnumerable<string> ComPrefixo(string prefixo, int limite = int.MaxValue)
    {
        var inicio = Descer(prefixo);
        if (inicio is null || limite <= 0)
        {
            yield break;
        }

        var encontrados = 0;

        // A pilha guarda o caminho até cada nó para não recompor a string a
        // cada passo; os filhos entram em ordem inversa para sair ordenados.
        var pilha = new Stack<(No No, string Caminho)>();
        pilha.Push((inicio, prefixo));

        while (pilha.Count > 0)
        {
            var (no, caminho) = pilha.Pop();

            if (no.Final)
            {
                yield return caminho;
                encontrados++;

                if (encontrados >= limite)
                {
                    yield break;
                }
            }

            if (no.Filhos is null)
            {
                continue;
            }

            foreach (var (letra, filho) in no.Filhos.OrderByDescending(par => par.Key))
            {
                pilha.Push((filho, caminho + letra));
            }
        }
    }

    /// <summary>Todos os termos guardados, em ordem alfabética.</summary>
    public IEnumerable<string> Termos() => ComPrefixo(string.Empty);

    private No? Descer(string prefixo)
    {
        var atual = raiz;

        foreach (var c in prefixo ?? string.Empty)
        {
            if (atual.Filhos is null || !atual.Filhos.TryGetValue(c, out var filho))
            {
                return null;
            }

            atual = filho;
        }

        return atual;
    }
}
