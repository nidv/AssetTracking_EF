using System.ComponentModel.DataAnnotations;

namespace AssetTracking_EF.Models;

// Level 5 requirement - Employees with roles
public class Employee
{
    public int EmployeeId { get; set; }

    [Required]
    [StringLength(60)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(60)]
    public string Department { get; set; } = string.Empty;

    public Role Role { get; set; } = Role.Employee;

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    public ICollection<Asset> AssignedAssets { get; set; } = new List<Asset>();
}
