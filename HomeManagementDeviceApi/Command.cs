namespace HomeManagementDeviceApi;

public record Command(string FileName, IEnumerable<string> Arguments);