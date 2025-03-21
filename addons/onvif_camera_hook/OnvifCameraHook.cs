#if TOOLS
using Godot;
using System;

[Tool]
public partial class OnvifCameraHook : EditorPlugin
{
	public override void _EnterTree()
	{
		// Initialization of the plugin goes here.
		var script = GD.Load<Script>("addons/onvif_camera_hook/OnvifCameraHookNode.cs");
		var texture = GD.Load<Texture2D>("addons/onvif_camera_hook/icon16.png");
		AddCustomType("OnvifCameraHookNode", "Node", script, texture);
	}

	public override void _ExitTree()
	{
		RemoveCustomType("OnvifCameraHookNode");
	}
}
#endif
