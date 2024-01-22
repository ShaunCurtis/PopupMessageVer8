Based on you Net 8 Repo, this is what I've done.

My repo is here: https://github.com/ShaunCurtis/PopupMessageVer8

The first answer I gave holds good.

1. I've created a generic Bootstrap`MessageBox` to replace your `PopUpMessageBox` so there's no dependancy on DevExpress.  My trial version is long expired!

```csharp
<CascadingValue Value="_messageBoxHandle" IsFixed="true">
	@ChildContent
</CascadingValue>

<!-- Modal -->
<div class="@this.CssClass" id="staticBackdrop" data-bs-backdrop="static" data-bs-keyboard="false" tabindex="-1" aria-labelledby="staticBackdropLabel" aria-hidden="true">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title" id="staticBackdropLabel">Modal title</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close" @onclick="this.CloseAsync"></button>
            </div>
            <div class="modal-body">
                @_message
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" @onclick="this.CloseAsync" >Close</button>
                <button type="button" class="btn btn-primary" @onclick="this.CloseAsync">Understood</button>
            </div>
        </div>
    </div>
</div>
<style>
    .modal-dialog-show {
        display: block;
    }

    .modal-dialog-hide {
        display: none;
    }
</style>
@code {
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private bool _show;
    private string? _message;

    private string CssClass => _show ? "modal fade show modal-dialog-show" : "modal fade modal-dialog-hide";

    private TaskCompletionSource? _taskCompletionSource;

    private MessageBoxHandle _messageBoxHandle;

    public MessageBox()
    {
        _messageBoxHandle = new(this);
    }

    public Task ShowAsync(string message)
    {
        _message = message;
        _taskCompletionSource = new();
        _show = true;
        StateHasChanged();
        return _taskCompletionSource.Task;
    }

    private Task CloseAsync()
    {
        _show = false;
        _taskCompletionSource?.SetResult();
        return Task.CompletedTask;
    }
}
```

2. I've created a `MessageBoxHandle` to pass as the cascaded parameter.  It restricts functionality to what is necessary,

```csharp
public readonly struct MessageBoxHandle
{
    private readonly MessageBox _messageBox;

    public MessageBoxHandle(MessageBox messageBox)
    {
        _messageBox = messageBox;
    }

    public Task ShowAsync(string message)
        => _messageBox.ShowAsync(message);

    // Add anything else you need to cascade here
}
```

3. I've added `@rendermode="InteractiveServer"` to the components in `App`

```csharp
<!DOCTYPE html>
<html lang="en">

<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <base href="/" />
    <link rel="stylesheet" href="bootstrap/bootstrap.min.css" />
    <link href="_content/DevExpress.Blazor.Themes/blazing-berry.bs5.min.css"
          rel="stylesheet" asp-append-version="true" />
    <link rel="stylesheet" href="app.css" />
    <link rel="stylesheet" href="PopupMessageVer8.styles.css" />
    <link rel="icon" type="image/png" href="favicon.png" />
    <HeadOutlet @rendermode="InteractiveServer" />
</head>

<body>
    <Routes @rendermode="InteractiveServer" />
    <script src="_framework/blazor.web.js"></script>
</body>

</html>
```

4. I've added the following service to `Program` so we can detect the render mode.

```csharp
	var builder = WebApplication.CreateBuilder(args);

	// Add services to the container.
	builder.Services.AddRazorComponents()
		.AddInteractiveServerComponents();

	builder.Services.AddHttpContextAccessor();

	var app = builder.Build();
```

5. Component to write the render mode to the console:

```csharp
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
```

6.  Here's my Home:

```csharp
@page "/"

<PageTitle>Home</PageTitle>

<RenderStateLogger Parent="this" />

<h1>Hello, world!</h1>

Welcome to your new app.

<div class="m-2">
	<button class="btn btn-primary" @onclick="this.ShowMessage">Show MessageBox</button>
</div>
@code {
	[CascadingParameter] protected MessageBoxHandle MessageBoxHandle { get; set; } = default!;

	private async Task ShowMessage()
	{
		await this.MessageBoxHandle.ShowAsync("Be careful not to count over 10!!!");

	}
}
```

7. Add `<RenderStateLogger Parent="this" />` to Routes, MainLayout, Counter and Weather.

Run the application and navigate between the pages.  The console log is exactly as you would expect:  prerender on the first page loaded and then SPA navigation after that: Routes is running in the SPA.

```text
Routes lifecycle initiated. Render State: PreRender
MainLayout lifecycle initiated. Render State: PreRender
Home lifecycle initiated. Render State: PreRender
Routes lifecycle initiated. Render State: SSR
MainLayout lifecycle initiated. Render State: SSR
Home lifecycle initiated. Render State: SSR
MainLayout lifecycle initiated. Render State: SSR
Counter lifecycle initiated. Render State: SSR
MainLayout lifecycle initiated. Render State: SSR
Weather lifecycle initiated. Render State: SSR
Weather lifecycle initiated. Render State: SSR
MainLayout lifecycle initiated. Render State: SSR
Home lifecycle initiated. Render State: SSR
```


Now change `App` to:

```csharp
<body>
    <Routes/>
    <script src="_framework/blazor.web.js"></script>
</body>
```

And run the application.  A very different story.  `Routes` and `MainLayout` is only statically rendered.  Every navigation is a server trip.  Double loading of `OnInitialized` every time!

```text
Routes lifecycle initiated. Render State: PreRender
MainLayout lifecycle initiated. Render State: PreRender
Home lifecycle initiated. Render State: PreRender
Routes lifecycle initiated. Render State: PreRender
MainLayout lifecycle initiated. Render State: PreRender
Counter lifecycle initiated. Render State: PreRender
Counter lifecycle initiated. Render State: SSR
Routes lifecycle initiated. Render State: PreRender
MainLayout lifecycle initiated. Render State: PreRender
Weather lifecycle initiated. Render State: PreRender
Weather lifecycle initiated. Render State: SSR
Routes lifecycle initiated. Render State: PreRender
MainLayout lifecycle initiated. Render State: PreRender
Counter lifecycle initiated. Render State: PreRender
Counter lifecycle initiated. Render State: SSR
Routes lifecycle initiated. Render State: PreRender
MainLayout lifecycle initiated. Render State: PreRender
Home lifecycle initiated. Render State: PreRender
Routes lifecycle initiated. Render State: PreRender
MainLayout lifecycle initiated. Render State: PreRender
Weather lifecycle initiated. Render State: PreRender
Weather lifecycle initiated. Render State: SSR
```

## Addressing Pre-Rendering

There are many ways to deal with pre-rendering.

A simple `PreRenderSplash` component

```csharp

@if (_preRendering)
{
    @this.PreRenderContent
    return;
}

@this.ChildContent

@code {
    [Inject] private IHttpContextAccessor _httpContextAccessor { get; set; } = default!;

    [Parameter] public RenderFragment? ChildContent { get; set; }

    [Parameter] public RenderFragment? PreRenderContent { get; set; }

    private bool _preRendering => !(_httpContextAccessor.HttpContext is not null && _httpContextAccessor.HttpContext.Response.HasStarted);
}
```

I've added it to `MainLayout`

```csharp
@inherits LayoutComponentBase

<RenderStateLogger Parent="this" />

<div class="page">
	<div class="sidebar">
		<NavMenu />
	</div>

	<main>
		<MessageBox>
			<div class="top-row px-4">
				<a href="https://learn.microsoft.com/aspnet/core/" target="_blank">About</a>
			</div>

			<article class="content px-4">
				<PreRenderingSplash>
					@Body
				</PreRenderingSplash>
			</article>
		</MessageBox>
	</main>
</div>

<div id="blazor-error-ui">
	An unhandled error has occurred.
	<a href="" class="reload">Reload</a>
	<a class="dismiss">🗙</a>
</div>
```

You could also add it to `Routes`:

```csharp
<RenderStateLogger Parent="this" />
<Router AppAssembly="@typeof(Program).Assembly">
    <Found Context="routeData">
        <PreRenderingSplash>
            <ChildContent>
                <RouteView RouteData="@routeData" DefaultLayout="@typeof(Layout.MainLayout)" />
                <FocusOnNavigate RouteData="@routeData" Selector="h1" />
            </ChildContent>
            <PreRenderContent>
                @* Content Here *@
            </PreRenderContent>
        </PreRenderingSplash>
    </Found>
</Router>
```
