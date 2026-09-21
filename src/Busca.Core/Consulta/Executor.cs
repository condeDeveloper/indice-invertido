using Busca.Core.Indice;
using Busca.Core.Pontuacao;

namespace Busca.Core.Consulta;

/// <summary>O que uma subconsulta produziu: documentos, pontuação e termos casados.</summary>
public sealed class Correspondencia
{
    private readonly Dictionary<int, double> pontuacoes = [];
    private readonly HashSet<string> termos = new(StringComparer.Ordinal);

    /// <summary>Documentos que casaram.</summary>
    public IReadOnlyCollection<int> Documentos => pontuacoes.Keys;

    /// <summary>Termos do índice que participaram.</summary>
    public IReadOnlyCollection<string> Termos => termos;

    /// <summary>Quantos documentos casaram.</summary>
    public int Quantidade => pontuacoes.Count;

    /// <summary>Pontuação de um documento, ou zero.</summary>
    public double Pontuacao(int documento) => pontuacoes.GetValueOrDefault(documento);

    /// <summary>Indica se o documento está na correspondência.</summary>
    public bool Contem(int documento) => pontuacoes.ContainsKey(documento);

    /// <summary>Soma pontuação a um documento.</summary>
    public void Somar(int documento, double pontuacao)
    {
        pontuacoes[documento] = pontuacoes.GetValueOrDefault(documento) + pontuacao;
    }

    /// <summary>Guarda a maior pontuação vista para o documento.</summary>
    public void Maior(int documento, double pontuacao)
    {
        if (pontuacao > pontuacoes.GetValueOrDefault(documento))
        {
            pontuacoes[documento] = pontuacao;
        }
    }

    /// <summary>Registra um termo que participou.</summary>
    public void RegistrarTermo(string termo) => termos.Add(termo);

    /// <summary>Junta outra correspondência somando as pontuações.</summary>
    public void Absorver(Correspondencia outra)
    {
        foreach (var documento in outra.Documentos)
        {
            Somar(documento, outra.Pontuacao(documento));
        }

        foreach (var termo in outra.Termos)
        {
            termos.Add(termo);
        }
    }

    /// <summary>Descarta os documentos que não estão no conjunto.</summary>
    public void Restringir(IReadOnlyCollection<int> permitidos)
    {
        foreach (var documento in pontuacoes.Keys.ToList())
        {
            if (!permitidos.Contains(documento))
            {
                pontuacoes.Remove(documento);
            }
        }
    }

    /// <summary>Descarta os documentos do conjunto informado.</summary>
    public void Excluir(IReadOnlyCollection<int> proibidos)
    {
        foreach (var documento in proibidos)
        {
            pontuacoes.Remove(documento);
        }
    }

}

/// <summary>
/// Executa uma consulta contra o índice.
/// </summary>
/// <remarks>
/// A composição booleana segue a regra do Lucene: as obrigatórias definem o
/// conjunto, as proibidas o reduzem, e as opcionais só somam pontuação — a não
/// ser que não haja nenhuma obrigatória, caso em que pelo menos uma opcional
/// precisa casar.
/// </remarks>
public sealed class Executor
{
    private readonly IndiceInvertido indice;
    private readonly Bm25 bm25;
    private readonly int limiteDeExpansao;

    /// <summary>Cria o executor sobre um índice.</summary>
    public Executor(IndiceInvertido indice, Bm25? bm25 = null, int limiteDeExpansao = 64)
    {
        this.indice = indice ?? throw new ArgumentNullException(nameof(indice));
        this.bm25 = bm25 ?? Bm25.Padrao;
        this.limiteDeExpansao = limiteDeExpansao;
    }

    /// <summary>Executa a consulta e devolve os documentos com pontuação.</summary>
    public Correspondencia Executar(No no)
    {
        ArgumentNullException.ThrowIfNull(no);

        return no switch
        {
            Termo termo => PorTermo(termo.Texto),
            Frase frase => PorFrase(frase),
            Prefixo prefixo => PorPrefixo(prefixo),
            Booleana booleana => PorBooleana(booleana),
            _ => new Correspondencia(),
        };
    }

    private Correspondencia PorTermo(string termo)
    {
        var correspondencia = new Correspondencia();
        var ocorrencias = indice.Ocorrencias(termo);

        if (ocorrencias is null || ocorrencias.FrequenciaNoCorpus == 0)
        {
            return correspondencia;
        }

        correspondencia.RegistrarTermo(termo);
        var peso = Bm25.PesoNoCorpus(ocorrencias.FrequenciaNoCorpus, indice.QuantidadeDeDocumentos);

        foreach (var aparicao in ocorrencias.Aparicoes)
        {
            var documento = indice.Documento(aparicao.Documento);
            if (documento is null)
            {
                continue;
            }

            correspondencia.Somar(
                aparicao.Documento,
                bm25.Pontuar(aparicao.Frequencia, documento.Comprimento, indice.ComprimentoMedio, peso));
        }

        return correspondencia;
    }

    private Correspondencia PorFrase(Frase frase)
    {
        var correspondencia = new Correspondencia();

        var listas = frase.Termos.Select(indice.Ocorrencias).ToList();
        if (listas.Any(lista => lista is null || lista.FrequenciaNoCorpus == 0))
        {
            return correspondencia;
        }

        foreach (var termo in frase.Termos)
        {
            correspondencia.RegistrarTermo(termo);
        }

        // Começar pelo termo mais raro reduz o número de documentos a conferir.
        var maisRara = listas.OrderBy(lista => lista!.FrequenciaNoCorpus).First()!;
        var candidatos = maisRara.Documentos().Where(documento => listas.All(lista => lista!.Em(documento) is not null));

        var frequencias = new Dictionary<int, int>();

        foreach (var documento in candidatos)
        {
            var quantidade = ContarFrase(listas!, documento, frase.Folga);
            if (quantidade > 0)
            {
                frequencias[documento] = quantidade;
            }
        }

        if (frequencias.Count == 0)
        {
            return correspondencia;
        }

        // A frase inteira é tratada como um termo só: a raridade dela é em
        // quantos documentos a sequência aparece, não cada palavra sozinha.
        var peso = Bm25.PesoNoCorpus(frequencias.Count, indice.QuantidadeDeDocumentos);

        foreach (var (documento, frequencia) in frequencias)
        {
            var dados = indice.Documento(documento);
            if (dados is null)
            {
                continue;
            }

            correspondencia.Somar(
                documento,
                bm25.Pontuar(frequencia, dados.Comprimento, indice.ComprimentoMedio, peso));
        }

        return correspondencia;
    }

    private static int ContarFrase(List<ListaDeOcorrencias> listas, int documento, int folga)
    {
        var primeira = listas[0].Em(documento);
        if (primeira is null)
        {
            return 0;
        }

        var posicoesSeguintes = new List<HashSet<int>>();
        for (var i = 1; i < listas.Count; i++)
        {
            var aparicao = listas[i].Em(documento);
            if (aparicao is null)
            {
                return 0;
            }

            posicoesSeguintes.Add([.. aparicao.Posicoes]);
        }

        var encontradas = 0;

        foreach (var inicio in primeira.Posicoes)
        {
            if (CasaAPartirDe(posicoesSeguintes, inicio, folga))
            {
                encontradas++;
            }
        }

        return encontradas;
    }

    private static bool CasaAPartirDe(List<HashSet<int>> seguintes, int inicio, int folga)
    {
        var anterior = inicio;

        foreach (var posicoes in seguintes)
        {
            var achou = false;

            // A folga cobre as palavras de parada que foram tiradas do índice
            // mas continuam ocupando posição no documento.
            for (var salto = 1; salto <= 1 + folga; salto++)
            {
                if (posicoes.Contains(anterior + salto))
                {
                    anterior += salto;
                    achou = true;
                    break;
                }
            }

            if (!achou)
            {
                return false;
            }
        }

        return true;
    }

    private Correspondencia PorPrefixo(Prefixo prefixo)
    {
        var correspondencia = new Correspondencia();
        var expandidos = indice.ComPrefixo(prefixo.Inicio, limiteDeExpansao).ToList();

        foreach (var termo in expandidos)
        {
            var parcial = PorTermo(termo);

            foreach (var documento in parcial.Documentos)
            {
                // Entre as variações do prefixo vale a melhor, e não a soma:
                // um documento que usa "programa" e "programador" não deve
                // ganhar o dobro só por isso.
                correspondencia.Maior(documento, parcial.Pontuacao(documento));
            }

            foreach (var casado in parcial.Termos)
            {
                correspondencia.RegistrarTermo(casado);
            }
        }

        return correspondencia;
    }

    private Correspondencia PorBooleana(Booleana booleana)
    {
        var resultado = new Correspondencia();

        if (booleana.Vazia)
        {
            return resultado;
        }

        var obrigatorias = booleana.Do(Papel.Deve).Select(Executar).ToList();
        var opcionais = booleana.Do(Papel.Pode).Select(Executar).ToList();
        var proibidas = booleana.Do(Papel.NaoPode).Select(Executar).ToList();

        foreach (var parcial in obrigatorias.Concat(opcionais))
        {
            resultado.Absorver(parcial);
        }

        // Cada obrigatória corta o resultado; o que sobra é a interseção. Um
        // documento que só casou em cláusula opcional cai fora aqui.
        foreach (var obrigatoria in obrigatorias)
        {
            resultado.Restringir(obrigatoria.Documentos.ToHashSet());
        }

        foreach (var proibida in proibidas)
        {
            resultado.Excluir(proibida.Documentos.ToList());
        }

        return resultado;
    }
}
