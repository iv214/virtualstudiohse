#if TOOLS
using Godot;
using System;

[Tool]
public partial class StudioEditorTool : EditorPlugin
{
	private Button _addStudioCameraButton;

	public override void _EnterTree()
	{
		_addStudioCameraButton = new Button();
		_addStudioCameraButton.Text = "Add Studio Cam";
		_addStudioCameraButton.Pressed += OnStudioCameraButtonPressed;
		AddControlToContainer(CustomControlContainer.Toolbar, _addStudioCameraButton);
		// Initialization of the plugin goes here.
	}

	public override void _ExitTree()
	{
		RemoveControlFromContainer(CustomControlContainer.Toolbar, _addStudioCameraButton);
		_addStudioCameraButton.QueueFree();
		// Clean-up of the plugin goes here.
	}
	private void OnStudioCameraButtonPressed()
	{
		var packedScene = ResourceLoader.Load<PackedScene>("addons/godot-studio/studio_camera.tscn");
		if (packedScene == null)
		{
			GD.PrintErr("Failed to load \"studio_camera.tscn\".");
			return;
		}
		var instance = packedScene.Instantiate();
		
		var root = GetEditorInterface().GetEditedSceneRoot();
		if (root == null)
		{
			GD.PrintErr("No scene currently open.");
			return;
		}
		root.AddChild(instance, true);
		instance.Owner = root;
		GD.Print("Added new studio camera.");
	}
}
#endif
