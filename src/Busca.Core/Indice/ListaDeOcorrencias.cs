namespace Busca.Core.Indice;

/// <summary>
/// As aparições de um termo em um documento. Guardar as posições, e não só a
/// contagem, é o que torna possível a busca por frase exata.
/// </summary>
public sealed class Aparicao
{
    private readonly List<int> posicoes = [];

    /// <summary>Cria a aparição para um documento.</summary>
    public Aparicao(int documento)
    {
        Documento = documento;
    }

    /// <summary>Cria a aparição já com as posições conhecidas.</summary>
    public Aparicao(int documento, IEnumerable<int> posicoes)
        : this(documento)
    {
        this.posicoes.AddRange(posicoes);
    }

    /// <summary>Identificador interno do documento.</summary>
    public int Documento { get; }

    /// <summary>Quantas vezes o termo aparece no documento.</summary>
    public int Frequencia => posicoes.Count;

    /// <summary>As posições em que o termo aparece, em ordem crescente.</summary>
    public IReadOnlyList<int> Posicoes => posicoes;

    /// <summary>Registra mais uma aparição.</summary>
    public void Registrar(int posicao) => posicoes.Add(posicao);
}

/// <summary>
/// Todas as aparições de um termo, ordenadas por documento. A ordem é o que
/// permite intersectar duas listas em tempo linear, sem materializar conjunto.
/// </summary>
public sealed class ListaDeOcorrencias
{
    private readonly List<Aparicao> aparicoes = [];
    private Aparicao? ultima;

    /// <summary>Em quantos documentos o termo aparece.</summary>
    public int FrequenciaNoCorpus => aparicoes.Count;

    /// <summary>Quantas vezes o termo aparece somando todos os documentos.</summary>
    public int TotalDeAparicoes => aparicoes.Sum(aparicao => aparicao.Frequencia);

    /// <summary>As aparições, em ordem de documento.</summary>
    public IReadOnlyList<Aparicao> Aparicoes => aparicoes;

    /// <summary>
    /// Registra uma aparição. Os documentos chegam em ordem crescente durante a
    /// indexação, então basta olhar para o último para saber se é o mesmo.
    /// </summary>
    public void Registrar(int documento, int posicao)
    {
        if (ultima is null || ultima.Documento != documento)
        {
            ultima = new Aparicao(documento);
            aparicoes.Add(ultima);
        }

        ultima.Registrar(posicao);
    }

    /// <summary>Adiciona uma aparição inteira, usada ao carregar do disco.</summary>
    public void Adicionar(Aparicao aparicao)
    {
        ArgumentNullException.ThrowIfNull(aparicao);

        aparicoes.Add(aparicao);
        ultima = aparicao;
    }

    /// <summary>Busca a aparição em um documento, ou nulo.</summary>
    public Aparicao? Em(int documento)
    {
        var esquerda = 0;
        var direita = aparicoes.Count - 1;

        while (esquerda <= direita)
        {
            var meio = esquerda + ((direita - esquerda) / 2);
            var atual = aparicoes[meio].Documento;

            if (atual == documento)
            {
                return aparicoes[meio];
            }

            if (atual < documento)
            {
                esquerda = meio + 1;
            }
            else
            {
                direita = meio - 1;
            }
        }

        return null;
    }

    /// <summary>Os documentos em que o termo aparece.</summary>
    public IEnumerable<int> Documentos() => aparicoes.Select(aparicao => aparicao.Documento);

    /// <summary>Remove as aparições de um documento que saiu do índice.</summary>
    public void Esquecer(int documento)
    {
        aparicoes.RemoveAll(aparicao => aparicao.Documento == documento);
        ultima = aparicoes.Count > 0 ? aparicoes[^1] : null;
    }
}
