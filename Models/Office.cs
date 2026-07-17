using System.ComponentModel.DataAnnotations;

namespace AssetTracking_EF.Models;

// Level 3 requirement - Global Office Management
public class Office
{
    public int Id { get; set; }

    [Required]
    [StringLength(60)]
    public string Name { get; set; } = string.Empty; // e.g. "Sweden Office"

    [Required]
    [StringLength(60)]
    public string Country { get; set; } = string.Empty; // e.g. "Sweden"

    // Level 3 requirement - Office Currency
    public Currency Currency { get; set; }

    public ICollection<Asset> Assets { get; set; } = new List<Asset>();

    public override string ToString() => $"{Name} ({Country}, {Currency})";
}
