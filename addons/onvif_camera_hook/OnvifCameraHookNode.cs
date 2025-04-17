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
	
	public string ConnectionProfileToken { get; set; } = "";
	[Export]
	public Vector2 OriginAngle { get; set; } = new Vector2(0, 0);
	public Vector2 Angle;
#nullable enable
	public Camera? ONVIFCamera { get; set; } = null;
	public Camera3D? GodotCamera { get; set; } = null;
#nullable disable
	//private CancellationTokenSource _cts;
	public override void _EnterTree()
	{
		GodotCamera = GetParentOrNull<Camera3D>();
		if (GodotCamera != null) GD.Print("Attached to " + GodotCamera.Name);
		else GD.Print("Attached to null");
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
		else ConnectionProfileToken = "";
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
			float pantilty = (float)(response.Position.PanTilt.x * 180 / Math.PI);
			float pantiltx = (float)(response.Position.PanTilt.y * 90 / Math.PI);
			//var pantilts = response.Position.PanTilt.space;
			//var zoomx = response.Position.Zoom.x;
			//var zooms = response.Position.Zoom.space;
			GodotCamera.SetGlobalRotationDegrees(new Vector3(OriginAngle.X + pantiltx, OriginAngle.Y - pantilty, 0));
		}
	}
}
