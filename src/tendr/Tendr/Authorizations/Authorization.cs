namespace Tendr.Authorizations;

public record AuthorizationRequested(Guid Id, int AmountCents, string Currency, string CardLast4);

public record CardApproved(Guid Id);

public record CardDeclined(Guid Id, string Reason);

/// <summary>
/// One card authorization, rebuilt from its own stream. The last four digits
/// are all Tendr keeps of the card.
/// </summary>
public class Authorization
{
    public Guid Id { get; set; }

    public string Status { get; set; } = "requested";

    public string? Reason { get; set; }

    public int AmountCents { get; set; }

    public string Currency { get; set; } = string.Empty;

    public string CardLast4 { get; set; } = string.Empty;

    public static Authorization Create(AuthorizationRequested requested) => new()
    {
        Id = requested.Id,
        AmountCents = requested.AmountCents,
        Currency = requested.Currency,
        CardLast4 = requested.CardLast4
    };

    public void Apply(CardApproved approved) => Status = "approved";

    public void Apply(CardDeclined declined)
    {
        Status = "declined";
        Reason = declined.Reason;
    }
}
