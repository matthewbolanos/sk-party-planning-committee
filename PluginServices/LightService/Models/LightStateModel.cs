
namespace PartyPlanning.PluginServices.LightService.Models;

public class LightStateModel
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public bool? On { get; set; }
    public byte? Brightness { get; set; }
    public string? HexColor { get; set; }
}