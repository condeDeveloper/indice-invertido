# indice-invertido

Motor de busca textual escrito do zero em C# e .NET 8, sem Lucene e sem
Elasticsearch. Analisador com stemmer de português, índice invertido com
postings posicionais, ordenação por BM25, busca por frase e por prefixo, trecho
destacado e índice persistido em formato binário próprio.

```csharp
var motor = new MotorDeBusca();
motor.Indexar("d1", "Receita de bolo de cenoura", "O bolo de cenoura leva cenoura, ovo e açúcar…");
motor.Indexar("d2", "Como plantar cenoura", "A cenoura gosta de solo fofo e profundo…");

var resultado = motor.Buscar("cenouras");   // acha os dois, apesar do plural

foreach (var achado in resultado.Achados)
{
    Console.WriteLine($"{achado.Pontuacao:F3}  {achado.Documento.Titulo}");
    Console.WriteLine(achado.Trecho);        // …leva <mark>cenoura</mark>, ovo e açúcar…
}
```

## Por que existe

Buscar texto parece trivial até a primeira tentativa com `Contains`. Aí aparecem
as perguntas de verdade: como "cenouras" encontra "cenoura", como ordenar dez mil
resultados por relevância, como fazer `"bolo de chocolate"` entre aspas não casar
com "chocolate de bolo", e como nada disso pode custar uma varredura no corpus
inteiro. Cada uma dessas perguntas é uma estrutura de dados diferente, e o
objetivo aqui foi escrever todas elas.

## Como funciona

**A esteira de análise** normaliza (caixa baixa, sem acento), separa em termos,
descarta palavra de parada e radicaliza. O mesmo analisador roda na indexação e
na consulta — se um lado radicalizar e o outro não, a busca nunca acha nada.

**O radicalizador** é um subconjunto do RSLP, o algoritmo da Viviane Orengo para
o português: plural, feminino, advérbio, aumentativo, sufixo de substantivo,
sufixo de verbo e vogal final, nessa ordem. A ordem importa — "balões" precisa
virar "balão" antes da etapa que corta o "ão", senão o plural para em um radical
diferente do singular.

**O índice** guarda, para cada termo, em que documentos ele aparece e **em quais
posições**. É a posição que torna a busca por frase possível. O vocabulário
também vai para uma trie, para que a busca por prefixo custe o tamanho do
prefixo em vez de uma varredura.

**O BM25** resolve dois problemas da contagem crua: a décima aparição de uma
palavra não vale o mesmo que a segunda (saturação, parâmetro `k1`), e um texto
longo tem mais chance de conter qualquer palavra por acidente (normalização por
comprimento, parâmetro `b`).

**A persistência** grava tudo com inteiros de tamanho variável e codificação por
diferença. Em uma lista de documentos densa, a diferença entre um e o anterior é
1, que cabe em um byte, contra os quatro de um `int` cru.

## Sintaxe de busca

| Escreva | Significa |
| --- | --- |
| `cenoura bolo` | qualquer um dos termos; quem tem os dois pontua mais |
| `+cenoura bolo` | precisa ter "cenoura" |
| `bolo -cenoura` | não pode ter "cenoura" |
| `"bolo de chocolate"` | a sequência exata, nessa ordem |
| `program*` | tudo que começa com "program" |

Os três podem se misturar: `+bolo -cenoura choc*`. A frase entre aspas atravessa
as palavras de parada — `"bolo de chocolate"` casa mesmo com o "de" fora do
índice, porque a posição dele foi preservada.

Consulta malformada não lança exceção: aspas sem fechar e asterisco solto são
entrada de usuário, não erro de programa.

## Estrutura

```
src/Busca.Core/Analise/        normalizador, tokenizador, lista de parada, stemmer
src/Busca.Core/Indice/         postings posicionais, trie do vocabulário, o índice
src/Busca.Core/Consulta/       árvore da consulta, leitor da sintaxe, executor
src/Busca.Core/Pontuacao/      BM25 e o destacador de trechos
src/Busca.Core/Persistencia/   varint com delta e o formato de arquivo
src/Busca.Core/MotorDeBusca.cs a fachada
src/Busca.Api/                 API mínima
tests/Busca.Tests/             168 testes
```

O núcleo não depende de nada além da BCL.

## Como rodar

```bash
dotnet test                         # roda a suíte inteira
dotnet run --project src/Busca.Api  # sobe a API em http://localhost:5000
```

Em desenvolvimento a API publica o Swagger em `/swagger`.

## Endpoints

| Método | Rota | O que faz |
| --- | --- | --- |
| `GET` | `/saude` | verificação de disponibilidade |
| `GET` | `/estatisticas` | documentos, tamanho do vocabulário e parâmetros do BM25 |
| `POST` | `/documentos` | indexa um documento |
| `POST` | `/documentos/lote` | indexa vários de uma vez |
| `GET` | `/documentos/{referencia}` | detalha um documento |
| `DELETE` | `/documentos/{referencia}` | tira o documento do índice |
| `GET` | `/busca?q=&quantidade=&pular=` | busca, com paginação |
| `GET` | `/sugestoes?q=` | completa o que está sendo digitado |
| `GET` | `/vocabulario` | os termos do índice |

A resposta da busca traz, além dos resultados, como a consulta foi interpretada
— útil para entender por que algo apareceu ou não:

```json
{
  "consulta": "+bolo -cenoura",
  "interpretacao": "+bol -cen",
  "total": 1,
  "milissegundos": 0.184,
  "achados": [
    {
      "referencia": "d3",
      "titulo": "Bolo de chocolate simples",
      "pontuacao": 1.043,
      "trecho": "Um <mark>bolo</mark> de chocolate rápido, com chocolate em pó…"
    }
  ]
}
```

## Gravando e recarregando

```csharp
motor.Gravar("corpus.idx");

var recarregado = MotorDeBusca.DeArquivo("corpus.idx");
recarregado.Buscar("\"bolo de chocolate\"");   // as posições sobrevivem
```

## Ajustando a relevância

```csharp
// b = 0 ignora o comprimento do documento; k1 alto retarda a saturação.
var motor = new MotorDeBusca(bm25: new Bm25(k1: 1.6, b: 0.3));

// Sem stemmer e sem lista de parada, para comparar os dois comportamentos.
var literal = new MotorDeBusca(Analisador.Simples);
```

## Limites conhecidos

- Índice em memória, com persistência para arquivo — não há segmentos, merge
  nem escrita incremental em disco.
- Remover um documento limpa as listas de ocorrências, mas não tira o termo do
  vocabulário; ele só some ao reconstruir o índice.
- O stemmer é um subconjunto do RSLP, então erra em palavras fora das regras
  mais comuns. `Analisador.Simples` desliga a radicalização.
- Sem sinônimos, sem correção ortográfica e sem busca por similaridade.

## Licença

MIT.
