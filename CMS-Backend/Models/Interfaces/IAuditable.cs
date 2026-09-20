namespace CMS_Backend.Models.Interfaces
{
    public interface IAuditable
    {
        DateTime CreatedAtUtc { get; set; }
        string? CreatedBy { get; set; }
        DateTime? UpdatedAtUtc { get; set; }
        string? UpdatedBy { get; set; }
    }

}
