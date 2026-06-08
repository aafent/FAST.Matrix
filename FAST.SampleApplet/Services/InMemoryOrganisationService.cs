using FAST.SampleApplet.Models;

namespace FAST.SampleApplet.Services;

/// <summary>
/// In-memory implementation with seeded demo data.
/// Replace with EF Core / HTTP client in a real applet.
/// </summary>
public sealed class InMemoryOrganisationService : IOrganisationService
{
    private readonly IReadOnlyList<Group> _groups;

    public InMemoryOrganisationService()
    {
        _groups = new List<Group>
        {
            new("G1", "Global Manufacturing Group", new List<Company>
            {
                new("C1", "Athens Plant Operations", "G1", new List<Office>
                {
                    new("O1", "HQ Office",        "C1", "Palaio Faliro, Athens"),
                    new("O2", "Production Floor",  "C1", "Piraeus Industrial Zone")
                }),
                new("C2", "Thessaloniki Division", "G1", new List<Office>
                {
                    new("O3", "Northern Hub", "C2", "Kalamaria, Thessaloniki")
                })
            }),
            new("G2", "FAST Technology Partners", new List<Company>
            {
                new("C3", "FAST Labs", "G2", new List<Office>
                {
                    new("O4", "Innovation Centre", "C3", "Maroussi, Athens"),
                    new("O5", "Remote Office",      "C3", "Heraklion, Crete")
                })
            })
        }.AsReadOnly();
    }

    public IReadOnlyList<Group>   GetAllGroups()        => _groups;
    public Group?   GetGroup(string id)   => _groups.FirstOrDefault(g => g.Id == id);
    public Company? GetCompany(string id) => _groups.SelectMany(g => g.Companies).FirstOrDefault(c => c.Id == id);
    public Office?  GetOffice(string id)  => _groups.SelectMany(g => g.Companies).SelectMany(c => c.Offices).FirstOrDefault(o => o.Id == id);
}
