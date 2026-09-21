using System.Text;
using Busca.Core.Analise;
using Busca.Core.Indice;

namespace Busca.Core.Persistencia;

/// <summary>
/// Grava e lê o índice em um arquivo binário próprio.
/// </summary>
/// <remarks>
/// O formato é simples de propósito: cabeçalho com assinatura e versão, os
/// documentos e depois o dicionário de termos, cada um com a lista de
/// ocorrências codificada por diferença. Reconstruir o índice a partir daqui é
/// muito mais barato do que reanalisar o corpus inteiro.
/// </remarks>
public static class ArquivoDeIndice
{
    private static readonly byte[] Assinatura = "IDXINV"u8.ToArray();

    /// <summary>Versão do formato gravada no cabeçalho.</summary>
    public const int Versao = 1;

    /// <summary>Grava o índice em um fluxo.</summary>
    public static void Gravar(IndiceInvertido indice, Stream destino)
    {
        ArgumentNullException.ThrowIfNull(indice);
        ArgumentNullException.ThrowIfNull(destino);

        using var escritor = new BinaryWriter(destino, Encoding.UTF8, leaveOpen: true);

        escritor.Write(Assinatura);
        Varint.Escrever(escritor, Versao);

        var documentos = indice.Documentos.ToList();
        Varint.Escrever(escritor, documentos.Count);

        foreach (var documento in documentos)
        {
            Varint.Escrever(escritor, documento.Id);
            escritor.Write(documento.Referencia);
            escritor.Write(documento.Titulo);
            escritor.Write(documento.Texto);
            Varint.Escrever(escritor, documento.Comprimento);
            Varint.Escrever(escritor, documento.TermosDistintos);
        }

        var termos = indice.Termos().Where(par => par.Value.FrequenciaNoCorpus > 0).ToList();
        Varint.Escrever(escritor, termos.Count);

        foreach (var (termo, lista) in termos)
        {
            escritor.Write(termo);
            Varint.Escrever(escritor, lista.FrequenciaNoCorpus);

            var anterior = 0;
            foreach (var aparicao in lista.Aparicoes)
            {
                // O identificador do documento também vai por diferença: as
                // aparições já estão em ordem crescente.
                Varint.Escrever(escritor, aparicao.Documento - anterior);
                anterior = aparicao.Documento;

                Varint.EscreverDiferencas(escritor, aparicao.Posicoes);
            }
        }
    }

    /// <summary>Lê um índice de um fluxo.</summary>
    public static IndiceInvertido Ler(Stream origem, Analisador? analisador = null)
    {
        ArgumentNullException.ThrowIfNull(origem);

        using var leitor = new BinaryReader(origem, Encoding.UTF8, leaveOpen: true);

        var assinatura = leitor.ReadBytes(Assinatura.Length);
        if (!assinatura.SequenceEqual(Assinatura))
        {
            throw new InvalidDataException("O arquivo não é um índice.");
        }

        var versao = Varint.Ler(leitor);
        if (versao != Versao)
        {
            throw new InvalidDataException($"Versão de índice não suportada: {versao}.");
        }

        var quantidadeDeDocumentos = Varint.Ler(leitor);
        var documentos = new List<Documento>(quantidadeDeDocumentos);

        for (var i = 0; i < quantidadeDeDocumentos; i++)
        {
            documentos.Add(new Documento
            {
                Id = Varint.Ler(leitor),
                Referencia = leitor.ReadString(),
                Titulo = leitor.ReadString(),
                Texto = leitor.ReadString(),
                Comprimento = Varint.Ler(leitor),
                TermosDistintos = Varint.Ler(leitor),
            });
        }

        var quantidadeDeTermos = Varint.Ler(leitor);
        var termos = new List<KeyValuePair<string, ListaDeOcorrencias>>(quantidadeDeTermos);

        for (var i = 0; i < quantidadeDeTermos; i++)
        {
            var termo = leitor.ReadString();
            var quantidadeDeAparicoes = Varint.Ler(leitor);
            var lista = new ListaDeOcorrencias();

            var anterior = 0;
            for (var j = 0; j < quantidadeDeAparicoes; j++)
            {
                anterior += Varint.Ler(leitor);
                lista.Adicionar(new Aparicao(anterior, Varint.LerDiferencas(leitor)));
            }

            termos.Add(new KeyValuePair<string, ListaDeOcorrencias>(termo, lista));
        }

        return IndiceInvertido.Restaurar(analisador ?? Analisador.Padrao, documentos, termos);
    }

    /// <summary>Grava o índice em um arquivo.</summary>
    public static void Gravar(IndiceInvertido indice, string caminho)
    {
        using var arquivo = File.Create(caminho);
        Gravar(indice, arquivo);
    }

    /// <summary>Lê um índice de um arquivo.</summary>
    public static IndiceInvertido Ler(string caminho, Analisador? analisador = null)
    {
        using var arquivo = File.OpenRead(caminho);
        return Ler(arquivo, analisador);
    }
}
