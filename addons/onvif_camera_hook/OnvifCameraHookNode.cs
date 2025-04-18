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
    [Export]
    public string ConnectionIP { get; set; } = "";

    [Export]
    public string ConnectionUsername { get; set; } = "";

    [Export]
    protected string ConnectionPassword { get; set; } = "";

    [Export]
    public Vector2 OriginAngle { get; set; } = new Vector2(0, 0);

    public string ConnectionProfileToken { get; set; } = "";
    public Vector2 Angle;

    public Camera? ONVIFCamera { get; set; } = null;
    public Camera3D? GodotCamera { get; set; } = null;

    public override void _EnterTree()
    {
        GodotCamera = GetParentOrNull<Camera3D>();
        GD.Print(GodotCamera != null ? $"Attached to {GodotCamera.Name}" : "Attached to null");
    }

    public void Connect()
    {
        var account = new Account(ConnectionIP, ConnectionUsername, ConnectionPassword);
        ONVIFCamera = Camera.Create(account, ex => { GD.PrintErr(ex); });
    }

    public async override void _Ready()
    {
        Connect();
        if (ONVIFCamera != null)
        {
            await GetProfileToken();
            if (ConnectionProfileToken.Length != 0)
            {
                var resolution = await GetCameraResolution();
                if (resolution != null)
                    GD.Print($"Resolution: {resolution.Value.X}x{resolution.Value.Y}");

                await PrintPtzLimits();

                SynchronizeRotation();
            }
        }
        GD.Print("Ready");
    }

    public async Task GetProfileToken()
    {
        var profiles = await ONVIFCamera.Media.GetProfilesAsync();
        if (profiles.Profiles.Length != 0)
            ConnectionProfileToken = profiles.Profiles[0].token;
        else
            ConnectionProfileToken = "";
    }

    public async Task<Vector2?> GetCameraResolution()
    {
        if (ONVIFCamera == null || ConnectionProfileToken.Length == 0) return null;

        var config = await ONVIFCamera.Media.GetVideoEncoderConfigurationAsync(ConnectionProfileToken);
        if (config != null && config.Resolution != null)
        {
            var width = config.Resolution.Width;
            var height = config.Resolution.Height;
            GD.Print($"Camera resolution: {width}x{height}");
            return new Vector2(width, height);
        }
        return null;
    }

    public async Task PrintPtzLimits()
    {
        if (ONVIFCamera == null || ConnectionProfileToken.Length == 0) return;

        var ptzConfig = await ONVIFCamera.Ptz.GetConfigurationAsync(ConnectionProfileToken);
        var options = await ONVIFCamera.Ptz.GetConfigurationOptionsAsync(ptzConfig.token);

        var panTiltLimits = options.Spaces.AbsolutePanTiltPositionSpace[0];
        var zoomLimits = options.Spaces.AbsoluteZoomPositionSpace[0];

        GD.Print("Pan limits: ", panTiltLimits.XRange.Min, " to ", panTiltLimits.XRange.Max);
        GD.Print("Tilt limits: ", panTiltLimits.YRange.Min, " to ", panTiltLimits.YRange.Max);
        GD.Print("Zoom limits: ", zoomLimits.XRange.Min, " to ", zoomLimits.XRange.Max);
    }

    public async void SynchronizeRotation()
    {
        while (!Engine.IsEditorHint())
        {
            await GetCameraRotation();
            Thread.Sleep(250);
            GD.Print("Rotation:", GodotCamera.Rotation.X, GodotCamera.Rotation.Y);
        }
        GD.Print("Exited");
    }

    public async Task GetCameraRotation()
    {
        if (!Engine.IsEditorHint())
        {
            var response = await ONVIFCamera.Ptz.GetStatusAsync(ConnectionProfileToken);
            var pantiltx = response.Position.PanTilt.x;
            var pantilty = response.Position.PanTilt.y;

            GodotCamera.SetGlobalRotationDegrees(new Vector3(OriginAngle.X - pantiltx, OriginAngle.Y - pantilty, 0));
        }
    }
}
