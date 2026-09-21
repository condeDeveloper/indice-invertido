using Busca.Core.Analise;

namespace Busca.Core.Indice;

/// <summary>
/// O índice invertido: em vez de perguntar "quais termos este documento tem",
/// ele responde "quais documentos têm este termo", que é a pergunta que uma
/// busca realmente faz.
/// </summary>
public sealed class IndiceInvertido
{
    private readonly Dictionary<string, ListaDeOcorrencias> termos = new(StringComparer.Ordinal);
    private readonly Dictionary<int, Documento> documentos = [];
    private readonly Dictionary<string, int> porReferencia = new(StringComparer.Ordinal);
    private readonly Trie vocabulario = new();

    private int proximoId;
    private long somaDosComprimentos;

    /// <summary>Cria o índice com a esteira de análise informada.</summary>
    public IndiceInvertido(Analisador? analisador = null)
    {
        Analisador = analisador ?? Analisador.Padrao;
    }

    /// <summary>A esteira usada tanto para indexar quanto para consultar.</summary>
    public Analisador Analisador { get; }

    /// <summary>Quantidade de documentos indexados.</summary>
    public int QuantidadeDeDocumentos => documentos.Count;

    /// <summary>Tamanho do vocabulário.</summary>
    public int QuantidadeDeTermos => termos.Count;

    /// <summary>Comprimento médio dos documentos, em termos.</summary>
    public double ComprimentoMedio => documentos.Count == 0 ? 0 : (double)somaDosComprimentos / documentos.Count;

    /// <summary>Os documentos indexados, em ordem de identificador.</summary>
    public IEnumerable<Documento> Documentos => documentos.Values.OrderBy(documento => documento.Id);

    /// <summary>
    /// Indexa um documento. Reindexar a mesma referência substitui o que estava
    /// lá, o que é o comportamento esperado de quem chama isso num laço de
    /// sincronização.
    /// </summary>
    public Documento Indexar(DocumentoParaIndexar entrada)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        if (string.IsNullOrWhiteSpace(entrada.Referencia))
        {
            throw new ArgumentException("O documento precisa de uma referência.", nameof(entrada));
        }

        Remover(entrada.Referencia);

        var id = proximoId++;
        var analisados = Analisador.Analisar($"{entrada.Titulo} {entrada.Texto}").ToList();
        var distintos = new HashSet<string>(StringComparer.Ordinal);

        foreach (var ocorrencia in analisados)
        {
            if (!termos.TryGetValue(ocorrencia.Termo, out var lista))
            {
                lista = new ListaDeOcorrencias();
                termos[ocorrencia.Termo] = lista;
                vocabulario.Inserir(ocorrencia.Termo);
            }

            lista.Registrar(id, ocorrencia.Posicao);
            distintos.Add(ocorrencia.Termo);
        }

        var documento = new Documento
        {
            Id = id,
            Referencia = entrada.Referencia,
            Titulo = entrada.Titulo,
            Texto = entrada.Texto,
            Comprimento = analisados.Count,
            TermosDistintos = distintos.Count,
        };

        documentos[id] = documento;
        porReferencia[entrada.Referencia] = id;
        somaDosComprimentos += analisados.Count;

        return documento;
    }

    /// <summary>Indexa vários documentos de uma vez.</summary>
    public int IndexarVarios(IEnumerable<DocumentoParaIndexar> entradas)
    {
        ArgumentNullException.ThrowIfNull(entradas);

        var quantidade = 0;
        foreach (var entrada in entradas)
        {
            Indexar(entrada);
            quantidade++;
        }

        return quantidade;
    }

    /// <summary>Remove um documento pela referência externa.</summary>
    public bool Remover(string referencia)
    {
        if (!porReferencia.TryGetValue(referencia, out var id))
        {
            return false;
        }

        var documento = documentos[id];
        somaDosComprimentos -= documento.Comprimento;

        documentos.Remove(id);
        porReferencia.Remove(referencia);

        // O vocabulário fica como está de propósito: um termo que sumiu do
        // corpus continua na trie até o índice ser reconstruído, e isso só
        // custa uma sugestão a mais, nunca um resultado errado — a lista de
        // ocorrências dele fica vazia.
        foreach (var lista in termos.Values)
        {
            lista.Esquecer(id);
        }

        return true;
    }

    /// <summary>Busca um documento pelo identificador interno.</summary>
    public Documento? Documento(int id) => documentos.GetValueOrDefault(id);

    /// <summary>Busca um documento pela referência externa.</summary>
    public Documento? PorReferencia(string referencia)
        => porReferencia.TryGetValue(referencia, out var id) ? documentos.GetValueOrDefault(id) : null;

    /// <summary>A lista de ocorrências de um termo já analisado.</summary>
    public ListaDeOcorrencias? Ocorrencias(string termo) => termos.GetValueOrDefault(termo);

    /// <summary>Em quantos documentos o termo aparece.</summary>
    public int FrequenciaNoCorpus(string termo) => Ocorrencias(termo)?.FrequenciaNoCorpus ?? 0;

    /// <summary>Termos do vocabulário que começam com o prefixo.</summary>
    public IEnumerable<string> ComPrefixo(string prefixo, int limite = 50)
        => vocabulario.ComPrefixo(prefixo, limite);

    /// <summary>Todo o vocabulário, em ordem alfabética.</summary>
    public IEnumerable<string> Vocabulario() => vocabulario.Termos();

    /// <summary>Todos os identificadores de documento, em ordem.</summary>
    public IEnumerable<int> TodosOsDocumentos() => documentos.Keys.Order();

    /// <summary>Reconstrói um índice a partir de dados já lidos do disco.</summary>
    public static IndiceInvertido Restaurar(
        Analisador analisador,
        IEnumerable<Documento> documentos,
        IEnumerable<KeyValuePair<string, ListaDeOcorrencias>> termos)
    {
        var indice = new IndiceInvertido(analisador);

        foreach (var documento in documentos)
        {
            indice.documentos[documento.Id] = documento;
            indice.porReferencia[documento.Referencia] = documento.Id;
            indice.somaDosComprimentos += documento.Comprimento;
            indice.proximoId = Math.Max(indice.proximoId, documento.Id + 1);
        }

        foreach (var (termo, lista) in termos)
        {
            indice.termos[termo] = lista;
            indice.vocabulario.Inserir(termo);
        }

        return indice;
    }

    /// <summary>Todos os pares termo/ocorrências, para gravar em disco.</summary>
    public IEnumerable<KeyValuePair<string, ListaDeOcorrencias>> Termos()
        => termos.OrderBy(par => par.Key, StringComparer.Ordinal);
}
