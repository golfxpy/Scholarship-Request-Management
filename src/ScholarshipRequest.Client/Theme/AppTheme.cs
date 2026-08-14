using MudBlazor;

namespace ScholarshipRequest.Client.Theme;

public static class AppTheme
{
    public static MudTheme Default { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#155E8A",
            PrimaryContrastText = "#FFFFFF",
            Secondary = "#D49A2A",
            SecondaryContrastText = "#172A3A",
            Background = "#F4F7FA",
            Surface = "#FFFFFF",
            AppbarBackground = "#FFFFFF",
            AppbarText = "#17324D",
            TextPrimary = "#17324D",
            TextSecondary = "#5B6F82",
            Success = "#287A52",
            Warning = "#A86508",
            Error = "#B3261E",
            Divider = "#DDE5EC"
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "12px"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily =
                [
                    "Leelawadee UI",
                    "Tahoma",
                    "Arial",
                    "sans-serif"
                ]
            },
            Button = new ButtonTypography
            {
                TextTransform = "none",
                FontWeight = "600"
            }
        }
    };
}
