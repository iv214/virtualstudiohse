using Godot;
using System;

public partial class NDIOutput : Node
{
    public string OutputName {
        get => (string)Get("name");
        set {
            Set("name", value);
        }
    }
}
