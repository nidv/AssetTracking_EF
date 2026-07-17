// Level 1 requirement - Asset Types

namespace AssetTracking_EF.Models;

public enum AssetType
{ 
    Computer,
    Laptop,
    Phone,
    Tablet
}

public enum Currency
{
    USD,
    EUR,
    TRY,
    SEK
}

public enum LifespanStatus
{
    Normal,  // More than 6 months remaining
    Red,     // Less than 6 months remaining
    Yellow,  // Less than 3 months remaining 
    Expired   // End of life has passed
}



