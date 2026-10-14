namespace TravelAdvisor.Core.Enums;

public enum TransportMode
{
    Bus = 0,
    Train = 1,
    Car = 2,
    Van = 3,
    TukTuk = 4,
    Flight = 5,
    Ferry = 6
}

public enum PaymentMethod
{
    Card = 0,
    Cash = 1,
    BankTransfer = 2
}

public enum PaymentStatus
{
    Pending = 0,
    Completed = 1,
    Refunded = 2,
    Failed = 3
}

public enum ReviewStatus
{
    Visible = 0,
    Hidden = 1
}

public enum RecommendationType
{
    Hotel = 0,
    Package = 1,
    Activity = 2,
    Transportation = 3
}
