using System.Net;

public class PurchaseNotFoundException : DomainException
{
    public PurchaseNotFoundException(int purchaseId) : base($"Purchase with id {purchaseId} not found.", HttpStatusCode.NotFound)
    { }
}