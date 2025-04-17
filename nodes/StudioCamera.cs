using Godot;
using System;

public partial class StudioCamera : Node3D
{
    private SubViewport _subviewport;
    private NDIOutput _ndioutput;
    
    [Export]
    public string NDIOutputName {
        get => _ndioutput.OutputName;
        set {
            _ndioutput.OutputName = value;
        }
    }
    
}
