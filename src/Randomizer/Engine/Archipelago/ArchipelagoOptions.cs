namespace MinishootRandomizer;

public class ArchipelagoOptions
{
    private string _uri;
    private string _slotName;
    private string _password;
    private bool _isDeathLink;

    public string Uri => _uri;
    public string SlotName => _slotName;
    public string Password => _password;
    public bool IsDeathLink => _isDeathLink;

    public ArchipelagoOptions(string uri, string slotName, string password, bool isDeathLink)
    {
        _uri = uri;
        _slotName = slotName;
        _password = password;
        _isDeathLink = isDeathLink;
    }
}
