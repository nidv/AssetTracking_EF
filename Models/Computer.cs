namespace AssetTracking_EF.Models
{
    // -------- Level 1 requirement - Computer Asset --------
    public class Computer : Asset
    {
        public Computer()
        {
            AssetType = AssetType.Computer;
        }
    }
}
