public record PurchaseTicketsResponseDTO(decimal Total, PurchaseStatus Status)
{
    public static PurchaseTicketsResponseDTO FromDomain(Purchase purchase)
    {
        return new PurchaseTicketsResponseDTO(purchase.Total, purchase.Status);
    }
}