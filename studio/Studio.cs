using Godot;
using System;

public partial class Studio : Node3D
{
    public override void _Ready()
    {
        Visible = Engine.IsEditorHint();
    }
}
