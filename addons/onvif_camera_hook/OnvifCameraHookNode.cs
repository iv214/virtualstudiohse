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
	public string ConnectionPassword { get; set; } = "";
	
	public string ConnectionProfileToken { get; set; } = "";
	[Export]
	public Vector2 OriginAngle { get; set; } = new Vector2(0, 0);
	public Vector2 Angle;
	public float Width;
	public float Height;
	bool run = true;
#nullable enable
	public Camera? ONVIFCamera { get; set; } = null;
	public Camera3D? GodotCamera { get; set; } = null;
#nullable disable
	//private CancellationTokenSource _cts;
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
		if (run)
		{
			Connect();
			if (ONVIFCamera != null)
			{
				await GetProfileToken();
				if (ConnectionProfileToken.Length != 0)
				{
					//var resolution = await GetCameraResolution();
					//if (resolution != null)
					//	GD.Print($"Resolution: {resolution.Value.X}x{resolution.Value.Y}");
					//await PrintPtzLimits();
					SynchronizeRotation();
				}
			}
			GD.Print("Ready");
		}
	}
	public async Task GetProfileToken()
	{
		var profiles = await ONVIFCamera.Media.GetProfilesAsync();
		if (profiles.Profiles.Length != 0)
			ConnectionProfileToken = profiles.Profiles[0].token;
		else ConnectionProfileToken = "";
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
			//await Task.Delay(200);
			GD.Print("Rotation:", GodotCamera.Rotation.X, GodotCamera.Rotation.Y);
		}
		GD.Print("Exited");
	}
	public async Task GetCameraRotation()
	{
		if (!Engine.IsEditorHint())
		{
			
			var response = await ONVIFCamera.Ptz.GetStatusAsync(ConnectionProfileToken);
			float pantilty = (float)(response.Position.PanTilt.x * 360 / Math.PI);
			float pantiltx = (float)(response.Position.PanTilt.y * 90 / Math.PI);
			//var pantilts = response.Position.PanTilt.space;
			//var zoomx = response.Position.Zoom.x;
			//var zooms = response.Position.Zoom.space;
			GodotCamera.SetGlobalRotationDegrees(new Vector3(OriginAngle.X + pantiltx, OriginAngle.Y - pantilty, 0));
		}
	}
}
