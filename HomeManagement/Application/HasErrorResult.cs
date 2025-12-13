namespace HomeManagement.Application;

public class HasErrorResult
{
    public bool IsSuccessful => Error is null;
    public string? Error { get; set; }
}