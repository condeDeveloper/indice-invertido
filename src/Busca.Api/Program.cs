using Busca.Api;
using Busca.Core;
using Microsoft.AspNetCore.Mvc;

var construtor = WebApplication.CreateBuilder(args);

construtor.Services.AddEndpointsApiExplorer();
construtor.Services.AddSwaggerGen();
construtor.Services.AddSingleton(_ => new MotorDeBusca());

var aplicacao = construtor.Build();

if (aplicacao.Environment.IsDevelopment())
{
    aplicacao.UseSwagger();
    aplicacao.UseSwaggerUI();
}

aplicacao.MapGet("/saude", () => Results.Ok(new { estado = "ok" }))
    .WithName("Saude")
    .WithTags("Serviço");

aplicacao.MapGet("/estatisticas", (MotorDeBusca motor) => Results.Ok(new Estatisticas(
        motor.Indice.QuantidadeDeDocumentos,
        motor.Indice.QuantidadeDeTermos,
        Math.Round(motor.Indice.ComprimentoMedio, 2),
        motor.Bm25.K1,
        motor.Bm25.B)))
    .WithName("Estatisticas")
    .WithTags("Índice");

aplicacao.MapPost("/documentos", ([FromBody] PedidoDeIndexacao pedido, MotorDeBusca motor) =>
    {
        if (string.IsNullOrWhiteSpace(pedido.Referencia))
        {
            return Problema("Documento inválido", "A referência é obrigatória.");
        }

        var documento = motor.Indice.Indexar(pedido.ParaNucleo());

        return Results.Created($"/documentos/{Uri.EscapeDataString(documento.Referencia)}", new
        {
            documento.Referencia,
            documento.Titulo,
            documento.Comprimento,
            documento.TermosDistintos,
        });
    })
    .WithName("Indexar")
    .WithTags("Índice");

aplicacao.MapPost("/documentos/lote", ([FromBody] PedidoDeLote pedido, MotorDeBusca motor) =>
    {
        if (pedido.Documentos.Any(documento => string.IsNullOrWhiteSpace(documento.Referencia)))
        {
            return Problema("Lote inválido", "Todo documento precisa de uma referência.");
        }

        var quantidade = motor.Indexar(pedido.Documentos.Select(documento => documento.ParaNucleo()));

        return Results.Ok(new { indexados = quantidade, total = motor.QuantidadeDeDocumentos });
    })
    .WithName("IndexarLote")
    .WithTags("Índice");

aplicacao.MapGet("/documentos/{referencia}", (string referencia, MotorDeBusca motor) =>
    {
        var documento = motor.Indice.PorReferencia(referencia);
        return documento is null ? Results.NotFound() : Results.Ok(documento);
    })
    .WithName("BuscarDocumento")
    .WithTags("Índice");

aplicacao.MapDelete("/documentos/{referencia}", (string referencia, MotorDeBusca motor) =>
        motor.Remover(referencia) ? Results.NoContent() : Results.NotFound())
    .WithName("RemoverDocumento")
    .WithTags("Índice");

aplicacao.MapGet("/busca", (string? q, int? quantidade, int? pular, MotorDeBusca motor) =>
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Problema("Consulta vazia", "Informe o parâmetro 'q'.");
        }

        var resultado = motor.Buscar(q, Math.Clamp(quantidade ?? 10, 1, 100), Math.Max(0, pular ?? 0));

        return Results.Ok(RespostaDaBusca.De(resultado, motor.Interpretar(q).ToString()));
    })
    .WithName("Buscar")
    .WithTags("Busca");

aplicacao.MapGet("/sugestoes", (string? q, int? quantidade, MotorDeBusca motor) =>
        Results.Ok(motor.Sugerir(q, Math.Clamp(quantidade ?? 10, 1, 50))))
    .WithName("Sugerir")
    .WithTags("Busca");

aplicacao.MapGet("/vocabulario", (int? quantidade, MotorDeBusca motor) =>
        Results.Ok(motor.Indice.Vocabulario().Take(Math.Clamp(quantidade ?? 100, 1, 1000))))
    .WithName("Vocabulario")
    .WithTags("Índice");

aplicacao.Run();

static IResult Problema(string titulo, string detalhe) => Results.BadRequest(new ProblemDetails
{
    Title = titulo,
    Detail = detalhe,
    Status = StatusCodes.Status400BadRequest,
});

/// <summary>Exposta para que os testes de integração possam subir a API.</summary>
public partial class Program;
