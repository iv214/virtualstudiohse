using Godot;
using System;
using Onvif.Core;
using Onvif.Core.Client.Camera;
using Onvif.Core.Client.Ptz;
using System.Threading.Tasks;

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
        if (String.IsNullOrWhiteSpace(ConnectionIP))
        {
            ONVIFCamera = null;
            return;
        }
        var account = new Account(ConnectionIP, ConnectionUsername, ConnectionPassword);
        ONVIFCamera = Camera.Create(account, ex => { GD.PrintErr(ex); });
    }

    public async override void _Ready()
    {
        if (Engine.IsEditorHint()) return;
        if (_run)
        {
            Connect();
            if (ONVIFCamera != null)
            {
                await GetProfileToken();
                if (ConnectionProfileToken.Length != 0)
                {
                    //await CacheCameraParameters();
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
            _ = GetCameraRotation();
        }
    }

    public async Task GetProfileToken()
    {
        try
        {
            var profiles = await ONVIFCamera.Media.GetProfilesAsync();
            ConnectionProfileToken = profiles.Profiles.Length != 0 ? profiles.Profiles[0].token : "";
        }
        catch (Exception ex)
        {
            GD.PrintErr("Failed to get profile token: " + ex.Message);
            ConnectionProfileToken = "";
        }
    }

    public async Task<Vector2?> GetCameraResolution()
    {
        if (ONVIFCamera == null || ConnectionProfileToken.Length == 0) return null;

        try
        {
            var config = await ONVIFCamera.Media.GetVideoEncoderConfigurationAsync(ConnectionProfileToken);
            if (config != null && config.Resolution != null)
            {
                return new Vector2(config.Resolution.Width, config.Resolution.Height);
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr("Failed to get camera resolution: " + ex.Message);
        }
        return null;
    }
    public async Task GetCameraParameters()
    {
        if (ONVIFCamera == null || ConnectionProfileToken.Length == 0) return;
        
        try
        {
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
        catch (Exception ex)
        {
            GD.PrintErr("Failed to cache camera parameters: " + ex.Message);
        }
    }

    public async void SynchronizeRotation()
    {
        while (!Engine.IsEditorHint() && _run) 
        {
            await GetCameraRotation();
            await Task.Delay((int)(UpdateInterval * 1000));
        }
        GD.Print("Camera synchronization stopped");
    }

    public async Task GetCameraRotation()
    {
        if (!Engine.IsEditorHint() && !_isUpdating && ONVIFCamera != null && ConnectionProfileToken.Length > 0)
        {
            try
            {
                _isUpdating = true;
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
    }

    private void UpdateCameraFOV(float zoomLevel)
    {
        if (GodotCamera == null) return;

        float targetFOV = 63f * Mathf.Pow(1 - zoomLevel, 1.5f);
        targetFOV = Math.Clamp(targetFOV, 2f, 63f);
        GodotCamera.Fov = Mathf.Lerp(GodotCamera.Fov, targetFOV, 0.1f);
    }

    private async void TryReconnect()
    {
        GD.Print("Attempting to reconnect...");
        await Task.Delay(5000);
        
        try
        {
            Connect();
            await GetProfileToken();
            if (ConnectionProfileToken.Length > 0)
            {
                //await CacheCameraParameters();
                GD.Print("Camera reconnected successfully");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr("Reconnect failed: " + ex.Message);
        }
    }

    public override void _ExitTree()
    {
        _run = false;
    }
}