using Busca.Core;
using Busca.Core.Persistencia;

namespace Busca.Tests;

public class VarintTests
{
    private static byte[] Gravar(Action<BinaryWriter> escrever)
    {
        using var memoria = new MemoryStream();
        using (var escritor = new BinaryWriter(memoria, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            escrever(escritor);
        }

        return memoria.ToArray();
    }

    private static T Ler<T>(byte[] bytes, Func<BinaryReader, T> ler)
    {
        using var memoria = new MemoryStream(bytes);
        using var leitor = new BinaryReader(memoria);
        return ler(leitor);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(127)]
    [InlineData(128)]
    [InlineData(16383)]
    [InlineData(16384)]
    [InlineData(int.MaxValue)]
    public void A_ida_e_volta_preserva_o_valor(int valor)
    {
        var bytes = Gravar(escritor => Varint.Escrever(escritor, valor));

        Ler(bytes, Varint.Ler).Should().Be(valor);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(127, 1)]
    [InlineData(128, 2)]
    [InlineData(16383, 2)]
    [InlineData(16384, 3)]
    public void Numero_pequeno_ocupa_menos_bytes(int valor, int bytesEsperados)
    {
        Varint.Tamanho(valor).Should().Be(bytesEsperados);
        Gravar(escritor => Varint.Escrever(escritor, valor)).Should().HaveCount(bytesEsperados);
    }

    [Fact]
    public void Numero_negativo_e_recusado()
    {
        var acao = () => Gravar(escritor => Varint.Escrever(escritor, -1));

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void A_sequencia_por_diferenca_volta_igual()
    {
        var valores = new List<int> { 0, 3, 4, 900, 901 };

        var bytes = Gravar(escritor => Varint.EscreverDiferencas(escritor, valores));

        Ler(bytes, Varint.LerDiferencas).Should().Equal(valores);
    }

    [Fact]
    public void A_diferenca_economiza_bytes_em_sequencia_densa()
    {
        var valores = Enumerable.Range(1000, 100).ToList();

        var comDiferenca = Gravar(escritor => Varint.EscreverDiferencas(escritor, valores)).Length;
        var semDiferenca = Gravar(escritor =>
        {
            Varint.Escrever(escritor, valores.Count);
            foreach (var valor in valores)
            {
                Varint.Escrever(escritor, valor);
            }
        }).Length;

        // Em sequência densa a diferença entre um valor e o anterior é 1, que
        // cabe em um byte; o valor cru, na casa do milhar, precisa de dois.
        comDiferenca.Should().BeLessThan(valores.Count + 10);
        semDiferenca.Should().BeGreaterThan(valores.Count * 2);
    }

    [Fact]
    public void Sequencia_fora_de_ordem_e_recusada()
    {
        var acao = () => Gravar(escritor => Varint.EscreverDiferencas(escritor, [5, 1]));

        acao.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Varint_corrompido_reclama()
    {
        var bytes = Enumerable.Repeat((byte)0xFF, 10).ToArray();

        var acao = () => Ler(bytes, Varint.Ler);

        acao.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Sequencia_vazia_volta_vazia()
    {
        var bytes = Gravar(escritor => Varint.EscreverDiferencas(escritor, []));

        Ler(bytes, Varint.LerDiferencas).Should().BeEmpty();
    }
}

public class ArquivoDeIndiceTests
{
    [Fact]
    public void A_ida_e_volta_preserva_o_indice()
    {
        var original = Corpus.Indice();

        using var memoria = new MemoryStream();
        ArquivoDeIndice.Gravar(original, memoria);
        memoria.Position = 0;
        var lido = ArquivoDeIndice.Ler(memoria);

        lido.QuantidadeDeDocumentos.Should().Be(original.QuantidadeDeDocumentos);
        lido.QuantidadeDeTermos.Should().Be(original.QuantidadeDeTermos);
        lido.ComprimentoMedio.Should().BeApproximately(original.ComprimentoMedio, 0.001);
    }

    [Fact]
    public void O_indice_lido_busca_igual_ao_original()
    {
        var caminho = Path.Combine(Path.GetTempPath(), $"indice-{Guid.NewGuid():N}.idx");

        try
        {
            var motor = Corpus.Motor();
            var esperado = motor.Buscar("cenoura").Referencias();

            motor.Gravar(caminho);
            var recarregado = MotorDeBusca.DeArquivo(caminho);

            recarregado.Buscar("cenoura").Referencias().Should().Equal(esperado);
        }
        finally
        {
            File.Delete(caminho);
        }
    }

    [Fact]
    public void As_posicoes_sobrevivem_para_a_busca_por_frase()
    {
        var caminho = Path.Combine(Path.GetTempPath(), $"indice-{Guid.NewGuid():N}.idx");

        try
        {
            Corpus.Motor().Gravar(caminho);

            MotorDeBusca.DeArquivo(caminho).Buscar("\"bolo de chocolate\"").Total.Should().BeGreaterThan(0);
        }
        finally
        {
            File.Delete(caminho);
        }
    }

    [Fact]
    public void Carregar_troca_o_indice_do_motor()
    {
        var caminho = Path.Combine(Path.GetTempPath(), $"indice-{Guid.NewGuid():N}.idx");

        try
        {
            Corpus.Motor().Gravar(caminho);

            var motor = new MotorDeBusca();
            motor.QuantidadeDeDocumentos.Should().Be(0);

            motor.Carregar(caminho);

            motor.QuantidadeDeDocumentos.Should().Be(5);
            motor.Buscar("cenoura").Total.Should().Be(2);
        }
        finally
        {
            File.Delete(caminho);
        }
    }

    [Fact]
    public void Arquivo_que_nao_e_indice_reclama()
    {
        using var memoria = new MemoryStream("isto nao e um indice"u8.ToArray());

        var acao = () => ArquivoDeIndice.Ler(memoria);

        acao.Should().Throw<InvalidDataException>().WithMessage("*não é um índice*");
    }

    [Fact]
    public void Indice_vazio_tambem_grava_e_le()
    {
        using var memoria = new MemoryStream();
        ArquivoDeIndice.Gravar(new Core.Indice.IndiceInvertido(), memoria);
        memoria.Position = 0;

        ArquivoDeIndice.Ler(memoria).QuantidadeDeDocumentos.Should().Be(0);
    }
}
