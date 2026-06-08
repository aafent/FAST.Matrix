using FAST.SampleApplet.Models;

namespace FAST.SampleApplet.Services;

/// <summary>
/// Private data service for the SampleApplet.
/// Registered via [AppletService] — isolated to this applet's DI sandbox.
/// Other applets registering their own IOrganisationService will NOT conflict.
/// </summary>
public interface IOrganisationService
{
    IReadOnlyList<Group> GetAllGroups();
    Group? GetGroup(string id);
    Company? GetCompany(string id);
    Office? GetOffice(string id);
}
