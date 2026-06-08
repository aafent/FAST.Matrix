namespace FAST.SampleApplet.Models;

public sealed record Group(string Id, string Name, IReadOnlyList<Company> Companies);

public sealed record Company(string Id, string Name, string GroupId, IReadOnlyList<Office> Offices);

public sealed record Office(string Id, string Name, string CompanyId, string Address);

/// <summary>
/// Simulates a workspace document with an edit state — used to demonstrate
/// the unsaved-changes navigation guard.
/// </summary>
public sealed class WorkspaceDocument
{
    public string OfficeId   { get; set; } = string.Empty;
    public string OfficeName { get; set; } = string.Empty;
    public string Address    { get; set; } = string.Empty;
    public string Notes      { get; set; } = string.Empty;
    public bool   IsDirty    { get; set; }
}
