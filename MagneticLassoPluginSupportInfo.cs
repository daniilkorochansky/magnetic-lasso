using PaintDotNet;

namespace MagneticLasso;

public sealed class MagneticLassoPluginSupportInfo : IPluginSupportInfo
{
    public string DisplayName
    {
        get
        {
            return "Magnetic Lasso";
        }
    }

    public Version Version
    {
        get
        {
            return new Version(1, 0, 0, 0);
        }
    }

    public string Author
    {
        get
        {
            return "Daniil Korochansky";
        }
    }

    public string Copyright
    {
        get
        {
            return "Copyright © 2026 Daniil Korochansky";
        }
    }

    public Uri WebsiteUri
    {
        get
        {
            return new Uri("https://github.com/daniilkorochansky/magnetic-lasso");
        }
    }
}