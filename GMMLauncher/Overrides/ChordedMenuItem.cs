using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;

#nullable enable
namespace Avalonia.Controls;

/// <summary>A menu item control.</summary>
[TemplatePart("PART_Popup", typeof(Popup))]
[Avalonia.Controls.Metadata.PseudoClasses(new string[]
  { ":separator", ":radio", ":toggle", ":checked", ":icon", ":open", ":pressed", ":selected" })]
public class ChordedMenuItem : MenuItem
{
  public new static readonly StyledProperty<string?> InputGestureProperty = AvaloniaProperty.Register<MenuItem, string>(nameof (InputGesture));

  public new string? InputGesture
  {
    get => this.GetValue<string>(InputGestureProperty);
    set => this.SetValue<string>(InputGestureProperty, value);
  }
}
