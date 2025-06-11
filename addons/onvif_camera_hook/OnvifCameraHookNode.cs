using Godot;
using System;
using Onvif.Core;
using Onvif.Core.Client.Camera;
using Onvif.Core.Client.Ptz;
using System.Threading.Tasks;
using System.Threading;

[Tool]
public partial class OnvifCameraHookNode : Node
{
    // Connection settings
    [Export]
    public string ConnectionIP { get; set; } = "";
    [Export]
    public string ConnectionUsername { get; set; } = "";
    [Export]
    public string ConnectionPassword { get; set; } = "";
    [Export]
    public string ConnectionProfileToken { get; set; } = "";
    [Export]
    public Vector2 OriginAngle { get; set; } = new Vector2(0, 0);

    // Camera control parameters
    [Export]
    public float LerpSpeed { get; set; } = 5f;
    [Export]
    public float UpdateInterval { get; set; } = 0.1f;
    [Export]
    public float MinFOV { get; set; } = 0.1f;
    [Export]
    public float MaxFOV { get; set; } = 90f;
    [Export]
    public float ZoomToFOVRatio { get; set; } = 0.5f;

    // Internal state
    private Vector3 _targetRotation;
    private float _timeSinceLastUpdate = 0f;
    private bool _isUpdating = false;
    private bool _run = true;
    private bool _isReconnecting = false;
    private CancellationTokenSource _cts = new CancellationTokenSource();

#nullable enable
    public Camera? ONVIFCamera { get; set; } = null;
    public Camera3D? GodotCamera { get; set; } = null;

    private float? _pan_min = null;
    private float? _pan_max = null;
    private float? _tilt_min = null;
    private float? _tilt_max = null;
    private float? _zoom_min = null;
    private float? _zoom_max = null;

#nullable disable

    public override void _EnterTree()
    {
        GodotCamera = GetParentOrNull<Camera3D>();
        GD.Print(GodotCamera != null ? $"Attached to {GodotCamera.Name}" : "Attached to null");
    }

    public void Connect()
    {
        try
        {
            if (String.IsNullOrWhiteSpace(ConnectionIP))
            {
                ONVIFCamera = null;
                return;
            }
            var account = new Account(ConnectionIP, ConnectionUsername, ConnectionPassword);
            ONVIFCamera = Camera.Create(account, ex => { 
                GD.PrintErr("Camera error: " + ex); 
                CallDeferred(nameof(TryReconnect));
            });
        }
        catch (Exception ex)
        {
            GD.PrintErr("Connection failed: " + ex.Message);
            CallDeferred(nameof(TryReconnect));
        }
    }

    public async override void _Ready()
    {
        if (Engine.IsEditorHint()) return;
        if (_run)
        {
            Connect();
            if (ONVIFCamera != null)
            {
                await GetProfileToken(_cts.Token);
                if (ConnectionProfileToken.Length != 0)
                {
                    SynchronizeRotation();
                }
            }
            GD.Print("Camera plugin ready");
        }
    }

    public override void _Process(double delta)
    {
        if (GodotCamera == null) return;

        _timeSinceLastUpdate += (float)delta;
        
        GodotCamera.RotationDegrees = GodotCamera.RotationDegrees.Lerp(_targetRotation, LerpSpeed * (float)delta);
        
        if (_timeSinceLastUpdate >= UpdateInterval)
        {
            _timeSinceLastUpdate = 0f;
            _ = GetCameraRotation(_cts.Token);
        }
    }

    public async Task GetProfileToken(CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            var profiles = await ONVIFCamera.Media.GetProfilesAsync();
            ConnectionProfileToken = profiles.Profiles.Length != 0 ? profiles.Profiles[0].token : "";
        }
        catch (OperationCanceledException)
        {
            GD.Print("Profile token request cancelled");
        }
        catch (Exception ex)
        {
            GD.PrintErr("Failed to get profile token: " + ex.Message);
            ConnectionProfileToken = "";
        }
    }

    public async Task<Vector2?> GetCameraResolution(CancellationToken ct = default)
    {
        if (ONVIFCamera == null || ConnectionProfileToken.Length == 0) return null;

        try
        {
            ct.ThrowIfCancellationRequested();
            var config = await ONVIFCamera.Media.GetVideoEncoderConfigurationAsync(ConnectionProfileToken);
            if (config != null && config.Resolution != null)
            {
                return new Vector2(config.Resolution.Width, config.Resolution.Height);
            }
        }
        catch (OperationCanceledException)
        {
            GD.Print("Camera resolution request cancelled");
        }
        catch (Exception ex)
        {
            GD.PrintErr("Failed to get camera resolution: " + ex.Message);
        }
        return null;
    }

    public async Task GetCameraParameters(CancellationToken ct = default)
    {
        if (ONVIFCamera == null || ConnectionProfileToken.Length == 0) return;
        
        try
        {
            ct.ThrowIfCancellationRequested();
            var ptzConfig = await ONVIFCamera.Ptz.GetConfigurationAsync(ConnectionProfileToken);
            var options = await ONVIFCamera.Ptz.GetConfigurationOptionsAsync(ptzConfig.token);
            
            GD.Print("Camera PTZ limits:");
            GD.Print("Pan limits: ", options.Spaces.AbsolutePanTiltPositionSpace[0].XRange.Min, 
                    " to ", options.Spaces.AbsolutePanTiltPositionSpace[0].XRange.Max);
            GD.Print("Tilt limits: ", options.Spaces.AbsolutePanTiltPositionSpace[0].YRange.Min, 
                    " to ", options.Spaces.AbsolutePanTiltPositionSpace[0].YRange.Max);
            GD.Print("Zoom limits: ", options.Spaces.AbsoluteZoomPositionSpace[0].XRange.Min, 
                    " to ", options.Spaces.AbsoluteZoomPositionSpace[0].XRange.Max);
        }
        catch (OperationCanceledException)
        {
            GD.Print("Camera parameters request cancelled");
        }
        catch (Exception ex)
        {
            GD.PrintErr("Failed to cache camera parameters: " + ex.Message);
        }
    }

    public async void SynchronizeRotation()
    {
        while (!Engine.IsEditorHint() && _run) 
        {
            await GetCameraRotation(_cts.Token);
            await ToSignal(GetTree().CreateTimer(UpdateInterval), "timeout");
        }
        GD.Print("Camera synchronization stopped");
    }

    public async Task GetCameraRotation(CancellationToken ct = default)
    {
        if (Engine.IsEditorHint() || _isUpdating || ONVIFCamera == null || ConnectionProfileToken.Length == 0)
            return;

        try
        {
            _isUpdating = true;
            ct.ThrowIfCancellationRequested();
            var response = await ONVIFCamera.Ptz.GetStatusAsync(ConnectionProfileToken);
            
            float pan = (float)(response.Position.PanTilt.x * 180);
            float tilt = (float)(response.Position.PanTilt.y * 90 / Math.PI);

            _targetRotation = new Vector3(
                OriginAngle.X + tilt,
                OriginAngle.Y - pan,
                0
            );

            if (response.Position.Zoom != null)
            {
                UpdateCameraFOV((float)response.Position.Zoom.x);
            }
        }
        catch (OperationCanceledException)
        {
            GD.Print("Camera rotation update cancelled");
        }
        catch (Exception ex)
        {
            GD.PrintErr("Failed to get camera rotation: " + ex.Message);
            TryReconnect();
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void UpdateCameraFOV(float zoomLevel)
    {
        if (GodotCamera == null) return;

        float targetFOV = 63f / 2f * Mathf.Pow(1 - zoomLevel, 1.5f);
        targetFOV = Math.Clamp(targetFOV, 1f, 32f);
        GodotCamera.Fov = Mathf.Lerp(GodotCamera.Fov, targetFOV, 0.1f);
    }

    private async void TryReconnect()
    {
        if (_isReconnecting) return;
        
        _isReconnecting = true;
        GD.Print("Attempting to reconnect...");
        
        try
        {
            await ToSignal(GetTree().CreateTimer(5.0), "timeout");
            Connect();
            await GetProfileToken(_cts.Token);
            if (ConnectionProfileToken.Length > 0)
            {
                GD.Print("Camera reconnected successfully");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr("Reconnect failed: " + ex.Message);
        }
        finally
        {
            _isReconnecting = false;
        }
    }

    public override void _ExitTree()
    {
        _run = false;
        _cts.Cancel();
        _cts.Dispose();
    }
}