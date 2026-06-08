using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
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

    // The currently selected tree node — read by SampleWorkspace
    public TreeViewNode? CurrentNode { get; private set; }

    // ── Constructors ──────────────────────────────────────────────────────────

    /// <summary>Parameterless — used by server-side DI and assembly scanner.</summary>
    public SampleApplet() { }

    /// <summary>
    /// Direct constructor for WASM DI — receives dependencies synchronously.
    /// Tree and toolbar are set up immediately, ready for activation.
    /// </summary>
    public SampleApplet(IShellUiContext uiContext, IOrganisationService orgSvc)
    {
        _ui = uiContext;
        _tree = BuildTree(orgSvc);
    }

    // Pre-built tree — constructed once, reused on every Activate()
    private TreeViewNode? _tree;

    // ── IApplet lifecycle ─────────────────────────────────────────────────────

    public Task OnAppletInitAsync(IServiceProvider appletServices)
    {
        _ui = appletServices.GetRequiredService<IShellUiContext>();
        var orgSvc = appletServices.GetRequiredService<IOrganisationService>();
        _tree = BuildTree(orgSvc);
        return Task.CompletedTask;
    }

    public Task<bool> HasUnsavedChangesAsync() => Task.FromResult(_hasUnsavedChanges);
    public void MarkDirty()  => _hasUnsavedChanges = true;
    public void MarkClean()  => _hasUnsavedChanges = false;

    // ── Activation / Deactivation ─────────────────────────────────────────────

    /// <summary>
    /// Called by AppletActivationService when navigating INTO /sample.
    /// Pushes tree and toolbar into the shell — MainLayout re-renders immediately.
    /// </summary>
    public void Activate()
    {
        if (_ui is null || _tree is null) return;
        CurrentNode = null; // Reset selection on each activation
        _ui.SetCustomTree(_tree, OnNodeSelected);
        _ui.SetTopToolbar(ToolbarFragment);
    }

    /// <summary>
    /// Called by AppletActivationService when navigating AWAY from /sample.
    /// </summary>
    public void Deactivate()
    {
        CurrentNode = null;
        _hasUnsavedChanges = false;
    }

    // ── Node selection ────────────────────────────────────────────────────────

    private void OnNodeSelected(TreeViewNode node)
    {
        CurrentNode = node;
        // OnStateChanged fires from RaiseNodeSelected in WasmShellUiContext
        // which propagates to MainLayout → HandleStateChanged → InvokeAsync(StateHasChanged)
        // That cascade re-renders SampleWorkspace which reads CurrentNode directly.
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
