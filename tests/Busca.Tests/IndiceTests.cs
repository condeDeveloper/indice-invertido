using Busca.Core.Analise;
using Busca.Core.Indice;

namespace Busca.Tests;

public class TrieTests
{
    private static Trie Com(params string[] termos)
    {
        var trie = new Trie();
        foreach (var termo in termos)
        {
            trie.Inserir(termo);
        }

        return trie;
    }

    [Fact]
    public void Guarda_e_encontra_o_termo_exato()
    {
        var trie = Com("casa", "casaco");

        trie.Contem("casa").Should().BeTrue();
        trie.Contem("cas").Should().BeFalse();
        trie.Quantidade.Should().Be(2);
    }

    [Fact]
    public void Inserir_de_novo_nao_duplica()
    {
        var trie = Com("casa", "casa");

        trie.Quantidade.Should().Be(1);
    }

    [Fact]
    public void Termo_vazio_e_recusado()
    {
        new Trie().Inserir("").Should().BeFalse();
    }

    [Fact]
    public void Encontra_tudo_que_comeca_com_o_prefixo()
    {
        var trie = Com("casa", "casaco", "caso", "carro", "bola");

        trie.ComPrefixo("cas").Should().Equal("casa", "casaco", "caso");
    }

    [Fact]
    public void O_resultado_sai_em_ordem_alfabetica()
    {
        var trie = Com("zebra", "abelha", "macaco");

        trie.Termos().Should().Equal("abelha", "macaco", "zebra");
    }

    [Fact]
    public void O_limite_corta_a_expansao()
    {
        var trie = Com("ca", "cab", "cac", "cad");

        trie.ComPrefixo("ca", limite: 2).Should().HaveCount(2);
    }

    [Fact]
    public void Prefixo_que_nao_existe_devolve_nada()
    {
        Com("casa").ComPrefixo("xyz").Should().BeEmpty();
        Com("casa").TemPrefixo("xyz").Should().BeFalse();
    }

    [Fact]
    public void Prefixo_vazio_devolve_tudo()
    {
        Com("a", "b").ComPrefixo("").Should().HaveCount(2);
    }
}

public class ListaDeOcorrenciasTests
{
    [Fact]
    public void Agrupa_as_posicoes_por_documento()
    {
        var lista = new ListaDeOcorrencias();
        lista.Registrar(1, 0);
        lista.Registrar(1, 5);
        lista.Registrar(2, 3);

        lista.FrequenciaNoCorpus.Should().Be(2);
        lista.TotalDeAparicoes.Should().Be(3);
        lista.Em(1)!.Posicoes.Should().Equal(0, 5);
        lista.Em(1)!.Frequencia.Should().Be(2);
    }

    [Fact]
    public void Busca_binaria_encontra_o_documento()
    {
        var lista = new ListaDeOcorrencias();
        for (var documento = 0; documento < 50; documento += 2)
        {
            lista.Registrar(documento, 0);
        }

        lista.Em(24).Should().NotBeNull();
        lista.Em(25).Should().BeNull();
    }

    [Fact]
    public void Lista_os_documentos_em_ordem()
    {
        var lista = new ListaDeOcorrencias();
        lista.Registrar(3, 0);
        lista.Registrar(7, 0);

        lista.Documentos().Should().Equal(3, 7);
    }

    [Fact]
    public void Esquecer_tira_o_documento()
    {
        var lista = new ListaDeOcorrencias();
        lista.Registrar(1, 0);
        lista.Registrar(2, 0);

        lista.Esquecer(1);

        lista.FrequenciaNoCorpus.Should().Be(1);
        lista.Em(1).Should().BeNull();
    }
}

public class IndiceInvertidoTests
{
    [Fact]
    public void Indexa_e_conta()
    {
        var indice = Corpus.Indice();

        indice.QuantidadeDeDocumentos.Should().Be(5);
        indice.QuantidadeDeTermos.Should().BeGreaterThan(20);
        indice.ComprimentoMedio.Should().BeGreaterThan(0);
    }

    [Fact]
    public void O_documento_guarda_o_comprimento_em_termos()
    {
        var indice = new IndiceInvertido(Analisador.Simples);

        var documento = indice.Indexar(new DocumentoParaIndexar("x", "um dois", "tres quatro"));

        documento.Comprimento.Should().Be(4);
        documento.TermosDistintos.Should().Be(4);
    }

    [Fact]
    public void O_titulo_tambem_e_indexado()
    {
        var indice = Corpus.Indice();
        var termo = indice.Analisador.Termo("plantar")!;

        indice.Ocorrencias(termo).Should().NotBeNull();
    }

    [Fact]
    public void Busca_pela_referencia_externa()
    {
        var indice = Corpus.Indice();

        indice.PorReferencia("d2")!.Titulo.Should().Be("Como plantar cenoura");
        indice.PorReferencia("inexistente").Should().BeNull();
    }

    [Fact]
    public void Reindexar_a_mesma_referencia_substitui()
    {
        var indice = new IndiceInvertido();
        indice.Indexar(new DocumentoParaIndexar("x", "Antigo", "conteudo antigo"));
        indice.Indexar(new DocumentoParaIndexar("x", "Novo", "conteudo novo"));

        indice.QuantidadeDeDocumentos.Should().Be(1);
        indice.PorReferencia("x")!.Titulo.Should().Be("Novo");
    }

    [Fact]
    public void Remover_tira_o_documento_e_as_ocorrencias()
    {
        var indice = Corpus.Indice();
        var termo = indice.Analisador.Termo("plantar")!;

        indice.Remover("d2").Should().BeTrue();

        indice.QuantidadeDeDocumentos.Should().Be(4);
        indice.PorReferencia("d2").Should().BeNull();
        indice.FrequenciaNoCorpus(termo).Should().Be(0);
    }

    [Fact]
    public void Remover_o_que_nao_existe_devolve_falso()
    {
        Corpus.Indice().Remover("nada").Should().BeFalse();
    }

    [Fact]
    public void O_comprimento_medio_acompanha_as_remocoes()
    {
        var indice = new IndiceInvertido(Analisador.Simples);
        indice.Indexar(new DocumentoParaIndexar("a", "", "um dois"));
        indice.Indexar(new DocumentoParaIndexar("b", "", "um dois tres quatro"));

        indice.ComprimentoMedio.Should().Be(3);

        indice.Remover("b");

        indice.ComprimentoMedio.Should().Be(2);
    }

    [Fact]
    public void Documento_sem_referencia_e_recusado()
    {
        var indice = new IndiceInvertido();

        var acao = () => indice.Indexar(new DocumentoParaIndexar("  ", "t", "x"));

        acao.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Expande_por_prefixo()
    {
        var indice = Corpus.Indice();

        indice.ComPrefixo("program").Should().NotBeEmpty();
    }

    [Fact]
    public void O_vocabulario_sai_ordenado()
    {
        var vocabulario = Corpus.Indice().Vocabulario().ToList();

        vocabulario.Should().BeInAscendingOrder(StringComparer.Ordinal);
    }

    [Fact]
    public void Frequencia_no_corpus_conta_documentos_e_nao_aparicoes()
    {
        var indice = Corpus.Indice();
        var termo = indice.Analisador.Termo("cenoura")!;

        // "cenoura" aparece várias vezes, mas em dois documentos.
        indice.FrequenciaNoCorpus(termo).Should().Be(2);
        indice.Ocorrencias(termo)!.TotalDeAparicoes.Should().BeGreaterThan(2);
    }
}
