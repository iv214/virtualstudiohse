using Godot;
using System;

[Tool]
public partial class NDIOutput : Node
{
    public string OutputName {
        get => Get("name").ToString();
        set {
            Set("name", value);
        }
    }
}
