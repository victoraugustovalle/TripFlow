namespace TripFlow.Domain.Enums;

/// <summary>Confirmed = 0 de proposito: a coluna foi adicionada numa tabela que ja tinha linhas,
/// e todo item que ja existia sempre foi "normal" - o valor padrao 0 do banco cobre isso sem
/// precisar de um default explicito na migration.</summary>
public enum ItineraryItemStatus
{
    Confirmed = 0,
    Proposed = 1,
}
