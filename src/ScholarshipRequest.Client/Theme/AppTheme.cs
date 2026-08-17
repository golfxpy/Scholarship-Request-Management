using MudBlazor;

namespace ScholarshipRequest.Client.Theme;

public static class AppTheme
{
    private static readonly string[] FontFamily =
    [
        "PSU-STIDTI",
        "Leelawadee UI",
        "Tahoma",
        "Arial",
        "sans-serif"
    ];

    public static MudTheme Default { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#155E8A",
            PrimaryContrastText = "#FFFFFF",
            Secondary = "#D49A2A",
            SecondaryContrastText = "#172A3A",
            Background = "#F6F8FA",
            Surface = "#FFFFFF",
            AppbarBackground = "#FFFFFF",
            AppbarText = "#17324D",
            TextPrimary = "#17324D",
            TextSecondary = "#5B6F82",
            Success = "#287A52",
            Warning = "#A86508",
            Error = "#B3261E",
            Divider = "#DDE5EC",
            LinesDefault = "#DDE5EC",
            TableLines = "#E4EBF0",
            DrawerBackground = "#FFFFFF",
            DrawerText = "#334F65"
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "12px"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = FontFamily,
                FontSize = "1rem",
                LineHeight = "1.55"
            },
            H1 = new H1Typography { FontFamily = FontFamily, FontWeight = "700", LineHeight = "1.15" },
            H2 = new H2Typography { FontFamily = FontFamily, FontWeight = "700", LineHeight = "1.2" },
            H3 = new H3Typography { FontFamily = FontFamily, FontWeight = "700", LineHeight = "1.25" },
            H4 = new H4Typography { FontFamily = FontFamily, FontWeight = "700" },
            H5 = new H5Typography { FontFamily = FontFamily, FontWeight = "700" },
            H6 = new H6Typography { FontFamily = FontFamily, FontWeight = "700" },
            Body1 = new Body1Typography { FontFamily = FontFamily },
            Body2 = new Body2Typography { FontFamily = FontFamily },
            Subtitle1 = new Subtitle1Typography { FontFamily = FontFamily },
            Subtitle2 = new Subtitle2Typography { FontFamily = FontFamily, FontWeight = "700" },
            Caption = new CaptionTypography { FontFamily = FontFamily },
            Overline = new OverlineTypography { FontFamily = FontFamily, FontWeight = "700" },
            Button = new ButtonTypography
            {
                FontFamily = FontFamily,
                TextTransform = "none",
                FontWeight = "700"
            }
        }
    };
}
