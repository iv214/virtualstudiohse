using Godot;
using System;

public partial class StudioCamera : Node3D
{
    private SubViewport _subviewport;
    private Node _ndioutput;
    private Camera3D _camera;
    private OnvifCameraHookNode _onvifhook;
    
    [Export]
    public string NDIOutputName;

    public override void _Ready()
    {
        base._Ready();
        _subviewport = GetNode<SubViewport>("SubViewport");
        _ndioutput = GetNode<Node>("SubViewport/NDIOutput");
        _camera = GetNode<Camera3D>("Camera3D");
        _onvifhook = GetNode<OnvifCameraHookNode>("Camera3D/OnvifCameraHookNode");
        _ndioutput.Set("name", NDIOutputName);
    }
    
}
