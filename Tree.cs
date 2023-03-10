<ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.Server.Session" Version="VERSION" />
</ItemGroup>

services.AddSession();
app.UseSession();
protected override async Task OnInitializedAsync()
{
    if (Session.TryGetValue("TreeViewData", out byte[] treeViewData))
    {
        var treeViewState = await JsonSerializer.DeserializeAsync<TelerikTreeViewState>(new MemoryStream(treeViewData));
        // Restore the state of the TelerikTreeView control from the session
        this.treeViewState = treeViewState;
    }
}
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender && this.treeViewRef != null)
    {
        // Get the state of the TelerikTreeView control and serialize it
        var treeViewState = await this.treeViewRef.GetStateAsync();
        var treeViewData = await JsonSerializer.SerializeAsync(treeViewState);

        // Store the serialized state in the session
        Session.Set("TreeViewData", treeViewData.ToArray());
    }
}
