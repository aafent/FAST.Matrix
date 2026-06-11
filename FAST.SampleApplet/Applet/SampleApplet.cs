using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using FAST.Matrix.Contracts.Applets;
using FAST.Matrix.Contracts.Attributes;
using FAST.Matrix.Contracts.UI;
using FAST.SampleApplet.Services;

namespace FAST.SampleApplet.Applet;

[AppletService(typeof(IOrganisationService), typeof(InMemoryOrganisationService))]
public sealed class SampleApplet : IApplet
{
    public string AppletId  => "fast.sample.applet";
    public string Name      => "Sample Applet";
    public string BaseRoute => "/sample";

    private IShellUiContext? _ui;
    private bool _hasUnsavedChanges;
    private TreeViewNode? _tree;

    // ── Constructors ──────────────────────────────────────────────────────────

    public SampleApplet() { }

    public SampleApplet(IShellUiContext uiContext, IOrganisationService orgSvc)
    {
        _ui   = uiContext;
        _tree = BuildTree(orgSvc);
    }

    // ── IApplet ───────────────────────────────────────────────────────────────

    public Task OnAppletInitAsync(IServiceProvider appletServices)
    {
        _ui   = appletServices.GetRequiredService<IShellUiContext>();
        _tree = BuildTree(appletServices.GetRequiredService<IOrganisationService>());
        return Task.CompletedTask;
    }

    public Task<bool> HasUnsavedChangesAsync() => Task.FromResult(_hasUnsavedChanges);
    public void MarkDirty()  => _hasUnsavedChanges = true;
    public void MarkClean()  => _hasUnsavedChanges = false;

    // ── Activation ────────────────────────────────────────────────────────────

    public void Activate()
    {
        if (_ui is null || _tree is null) return;
        _ui.SetCustomTree(_tree);
        _ui.SetTopToolbar(ToolbarFragment);
    }

    public void Deactivate()
    {
        _hasUnsavedChanges = false;
    }

    // ── Tree ──────────────────────────────────────────────────────────────────

    private static TreeViewNode BuildTree(IOrganisationService orgSvc)
    {
        var root = new TreeViewNode
        {
            Id = "root", Text = "Organisation",
            NodeType = "Root", IconClass = "fas fa-sitemap", IsExpanded = true
        };
        foreach (var group in orgSvc.GetAllGroups())
        {
            var gNode = new TreeViewNode
            {
                Id = group.Id, Text = group.Name,
                NodeType = "Group", IconClass = "fas fa-layer-group", IsExpanded = true,
                Metadata = { ["entityId"] = group.Id }
            };
            foreach (var company in group.Companies)
            {
                var cNode = new TreeViewNode
                {
                    Id = company.Id, Text = company.Name,
                    NodeType = "Company", IconClass = "fas fa-building",
                    Metadata = { ["entityId"] = company.Id, ["groupId"] = company.GroupId }
                };
                foreach (var office in company.Offices)
                {
                    cNode.Children.Add(new TreeViewNode
                    {
                        Id = office.Id, Text = office.Name,
                        NodeType = "Office", IconClass = "fas fa-map-marker-alt",
                        Metadata =
                        {
                            ["entityId"]  = office.Id,
                            ["companyId"] = office.CompanyId,
                            ["address"]   = office.Address
                        }
                    });
                }
                gNode.Children.Add(cNode);
            }
            root.Children.Add(gNode);
        }
        return root;
    }

    // ── Toolbar ───────────────────────────────────────────────────────────────

    private RenderFragment ToolbarFragment => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "btn-group");
        builder.OpenElement(2, "button");
        builder.AddAttribute(3, "class", "btn btn-sm btn-flat btn-success");
        builder.AddAttribute(4, "onclick", EventCallback.Factory.Create(this, () => MarkClean()));
        builder.OpenElement(5, "i");
        builder.AddAttribute(6, "class", "fas fa-save mr-1");
        builder.CloseElement();
        builder.AddContent(7, " Save");
        builder.CloseElement();
        builder.CloseElement();
    };
}
