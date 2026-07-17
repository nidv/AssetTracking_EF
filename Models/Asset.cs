using System.ComponentModel.DataAnnotations;

namespace AssetTracking_EF.Models;

// Level 1 requirement - Asset Base Class
public abstract class Asset
{
    public int Id { get; set; }
    public AssetType AssetType { get; protected set; }

    [Required]
    public string Brand { get; set; } = string.Empty;

    [Required]
    public string Model { get; set; } = string.Empty;

    [Required]
    [StringLength(8, ErrorMessage = "Serial number must be 8 characters or fewer.")]
    public string SerialNumber { get; set; } = string.Empty;

    public DateTime PurchaseDate { get; set; }

    // Level 3 requirement - Purchase Price (USD)
    public decimal PurchasePriceUsd { get; set; }

    // Level 3 requirement - Local Price (converted at view time)
    public Currency LocalCurrency { get; set; }

    // Level 4 requirement - Asset Age Calculation
    public DateTime EndOfLifeDate => PurchaseDate.AddYears(3);

    public DateTime? WarrantyExpirationDate { get; set; }

    // Level 3 requirement - Office Location
    public int OfficeId { get; set; }
    public Office? Office { get; set; }

    // Level 5 requirement - Employee assignment
    public int? EmployeeId { get; set; }
    public Employee? AssignedTo { get; set; }

    // Level 2 requirement - Asset Age Calculation
    public LifespanStatus LifeStatus
    {
        get
        {
            DateTime today = DateTime.Today;
            double daysRemaining = (EndOfLifeDate - today).TotalDays;

            // EXPIRED => end of life date has already passed
            if (daysRemaining < 0)
                return LifespanStatus.Expired;

            // YELLOW => less than 3 months remaining
            if (daysRemaining < 3 * 30.4375)
                return LifespanStatus.Yellow;

            // RED => less than 6 months remaining
            if (daysRemaining < 6 * 30.4375)
                return LifespanStatus.Red;

            return LifespanStatus.Normal;
        }
    }
}
