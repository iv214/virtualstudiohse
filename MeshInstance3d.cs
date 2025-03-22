using Godot;
using System;

public partial class MeshInstance3d : MeshInstance3D
{
    public override void _Process(double delta)
	{
        Rotate(new Vector3(1, 0, 0), (float)(3*delta));
	}
}