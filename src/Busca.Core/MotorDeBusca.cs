using System.Diagnostics;
using Busca.Core.Analise;
using Busca.Core.Consulta;
using Busca.Core.Indice;
using Busca.Core.Persistencia;
using Busca.Core.Pontuacao;

namespace Busca.Core;

/// <summary>
/// A fachada: indexar, buscar e sugerir. Junta a esteira de análise, o índice,
/// o executor de consultas e o destacador em uma coisa só.
/// </summary>
public sealed class MotorDeBusca
{
    private readonly AnalisadorDeConsulta leitorDeConsulta;
    private readonly Destacador destacador;
    private Executor executor;

    /// <summary>Monta o motor.</summary>
    public MotorDeBusca(
        Analisador? analisador = null,
        Bm25? bm25 = null,
        Destacador? destacador = null)
    {
        var esteira = analisador ?? Analisador.Padrao;

        Indice = new IndiceInvertido(esteira);
        leitorDeConsulta = new AnalisadorDeConsulta(esteira);
        this.destacador = destacador ?? Destacador.Padrao;
        Bm25 = bm25 ?? Bm25.Padrao;
        executor = new Executor(Indice, Bm25);
    }

    private MotorDeBusca(IndiceInvertido indice, Bm25 bm25, Destacador destacador)
    {
        Indice = indice;
        Bm25 = bm25;
        this.destacador = destacador;
        leitorDeConsulta = new AnalisadorDeConsulta(indice.Analisador);
        executor = new Executor(indice, bm25);
    }

    /// <summary>O índice por baixo, para quem precisar das estatísticas.</summary>
    public IndiceInvertido Indice { get; private set; }

    /// <summary>A função de ordenação em uso.</summary>
    public Bm25 Bm25 { get; }

    /// <summary>Quantidade de documentos indexados.</summary>
    public int QuantidadeDeDocumentos => Indice.QuantidadeDeDocumentos;

    /// <summary>Indexa um documento.</summary>
    public Documento Indexar(string referencia, string titulo, string texto)
        => Indice.Indexar(new DocumentoParaIndexar(referencia, titulo, texto));

    /// <summary>Indexa vários documentos.</summary>
    public int Indexar(IEnumerable<DocumentoParaIndexar> documentos) => Indice.IndexarVarios(documentos);

    /// <summary>Remove um documento pela referência.</summary>
    public bool Remover(string referencia) => Indice.Remover(referencia);

    /// <summary>Interpreta a consulta sem executá-la, útil para depurar.</summary>
    public Booleana Interpretar(string? consulta) => leitorDeConsulta.Analisar(consulta);

    /// <summary>Busca e devolve a página pedida, já ordenada e com os trechos.</summary>
    public Resultado Buscar(string? consulta, int quantidade = 10, int pular = 0)
    {
        var relogio = Stopwatch.StartNew();
        var texto = consulta ?? string.Empty;
        var arvore = leitorDeConsulta.Analisar(texto);

        if (arvore.Vazia)
        {
            return Resultado.Vazio(texto);
        }

        var correspondencia = executor.Executar(arvore);

        if (correspondencia.Quantidade == 0)
        {
            return new Resultado([], 0, texto, relogio.Elapsed.TotalMilliseconds);
        }

        var termos = correspondencia.Termos.ToList();

        var achados = correspondencia.Documentos
            .Select(id => (Id: id, Pontuacao: correspondencia.Pontuacao(id)))
            // O desempate pelo identificador mantém a ordem estável entre duas
            // buscas iguais, o que é o que permite testar a paginação.
            .OrderByDescending(par => par.Pontuacao)
            .ThenBy(par => par.Id)
            .Skip(Math.Max(0, pular))
            .Take(Math.Max(0, quantidade))
            .Select(par => Montar(par.Id, par.Pontuacao, termos))
            .Where(achado => achado is not null)
            .Select(achado => achado!)
            .ToList();

        return new Resultado(achados, correspondencia.Quantidade, texto, relogio.Elapsed.TotalMilliseconds);
    }

    /// <summary>Termos do vocabulário que completam o que foi digitado.</summary>
    public IReadOnlyList<string> Sugerir(string? inicio, int quantidade = 10)
    {
        var prefixo = Indice.Analisador.Prefixo(inicio);

        if (prefixo.Length == 0)
        {
            return [];
        }

        return Indice.ComPrefixo(prefixo, quantidade)
            .OrderByDescending(Indice.FrequenciaNoCorpus)
            .ThenBy(termo => termo, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Grava o índice em um arquivo.</summary>
    public void Gravar(string caminho) => ArquivoDeIndice.Gravar(Indice, caminho);

    /// <summary>Grava o índice em um fluxo.</summary>
    public void Gravar(Stream destino) => ArquivoDeIndice.Gravar(Indice, destino);

    /// <summary>Recarrega o índice deste motor a partir de um arquivo.</summary>
    public void Carregar(string caminho)
    {
        Indice = ArquivoDeIndice.Ler(caminho, Indice.Analisador);
        executor = new Executor(Indice, Bm25);
    }

    /// <summary>Monta um motor a partir de um índice gravado.</summary>
    public static MotorDeBusca DeArquivo(string caminho, Analisador? analisador = null, Bm25? bm25 = null)
        => new(ArquivoDeIndice.Ler(caminho, analisador), bm25 ?? Bm25.Padrao, Destacador.Padrao);

    private Achado? Montar(int id, double pontuacao, IReadOnlyList<string> termos)
    {
        var documento = Indice.Documento(id);
        if (documento is null)
        {
            return null;
        }

        return new Achado
        {
            Documento = documento,
            Pontuacao = Math.Round(pontuacao, 6),
            Trecho = destacador.Destacar(documento.Texto, termos),
            TermosCasados = termos,
        };
    }
}
