namespace PopupMessageVer8.Components;

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
