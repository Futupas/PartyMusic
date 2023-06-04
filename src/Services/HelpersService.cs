using System.Text.RegularExpressions;

namespace PartyMusic;

public class HelpersService
{
    public HelpersService()
    {
        
    }
    
    public Regex VideoFromUrlRegex { get; } =
        new (@"(youtube\.com\/watch.*([\?\&]v\=(?<videoId>[a-zA-Z0-9\-]*)))|(youtu\.be\/(?<videoId2>[a-zA-Z0-9\-]*))", RegexOptions.Compiled);
}
