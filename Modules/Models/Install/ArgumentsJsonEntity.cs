using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace MinecraftLaunch.Modules.Models.Install;

public class ArgumentsJsonEntity
{
	[JsonPropertyName("game")]
	public List<JToken> Game { get; set; }
}
