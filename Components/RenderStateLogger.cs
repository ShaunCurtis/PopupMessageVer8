using Microsoft.AspNetCore.Components;

namespace PopupMessageVer8.Components;

public class RenderStateLogger : ComponentBase
{
    [Parameter, EditorRequired] public object? Parent { get; set; }
    [Inject] private IHttpContextAccessor _httpContextAccessor { get; set; } = default!;

    private string _renderState => !(_httpContextAccessor.HttpContext is not null && _httpContextAccessor.HttpContext.Response.HasStarted)
    ? "PreRender"
    : "SSR";

    public override Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);
        Console.WriteLine($"{this.Parent?.GetType().Name} lifecycle initiated. Render State: {_renderState}");
        return base.SetParametersAsync(ParameterView.Empty);
    }
}
