
using Microsoft.AspNetCore.SignalR.Client;

namespace SmartInventoryPredictor.Client.Services;

public class SignalRService : IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private readonly ILogger<SignalRService> _logger;

    public SignalRService(ILogger<SignalRService> logger)
    {
        _logger = logger;
    }

    public async Task StartAsync(string hubUrl)
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .Build();

        await _hubConnection.StartAsync();
        _logger.LogInformation("SignalR connection started");
    }

    public async Task StopAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }

    public void OnStockUpdated(Action<object> handler)
    {
        _hubConnection?.On("StockUpdated", handler);
    }

    public void OnLowStockAlert(Action<object> handler)
    {
        _hubConnection?.On("LowStockAlert", handler);
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}