namespace CMS.Domain.Interfaces
{
    public interface IAuditable
    {
        DateTime CreatedAtUtc { get; set; }
        string? CreatedBy { get; set; }
        DateTime? UpdatedAtUtc { get; set; }
        string? UpdatedBy { get; set; }
    }

}
