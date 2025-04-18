using Godot;
using System;

[Tool]
public partial class StudioCamera : Node3D
{
    private SubViewport _subviewport;
    private Node _ndioutput;
    private Camera3D _camera;
    private OnvifCameraHookNode _onvifhook;
    
    [Export]
    public string NDIOutputName  {
        get => GetNode<Node>("SubViewport/NDIOutput").Get("name").ToString();
        set => GetNode<Node>("SubViewport/NDIOutput").Set("name", value);
    }
    [Export]
	public string ConnectionIP  {
        get => GetNode<OnvifCameraHookNode>("SubViewport/Camera3D/OnvifCameraHookNode").Get("ConnectionIP").ToString();
        set => GetNode<OnvifCameraHookNode>("SubViewport/Camera3D/OnvifCameraHookNode").Set("ConnectionIP", value);
    }
	[Export]
	public string ConnectionUsername  {
        get => GetNode<OnvifCameraHookNode>("SubViewport/Camera3D/OnvifCameraHookNode").Get("ConnectionUsername").ToString();
        set => GetNode<OnvifCameraHookNode>("SubViewport/Camera3D/OnvifCameraHookNode").Set("ConnectionUsername", value);
    }
	[Export]
	protected string ConnectionPassword  {
        get => GetNode<OnvifCameraHookNode>("SubViewport/Camera3D/OnvifCameraHookNode").Get("ConnectionPassword").ToString();
        set => GetNode<OnvifCameraHookNode>("SubViewport/Camera3D/OnvifCameraHookNode").Set("ConnectionPassword", value);
    }
    public override void _Ready()
    {
        base._Ready();
        _subviewport = GetNode<SubViewport>("SubViewport");
        _ndioutput = GetNode<Node>("SubViewport/NDIOutput");
        _camera = GetNode<Camera3D>("SubViewport/Camera3D");
        _onvifhook = GetNode<OnvifCameraHookNode>("SubViewport/Camera3D/OnvifCameraHookNode");
    }
    
}
