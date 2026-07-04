using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace ProxiFyre.UI.Converters;

public sealed class BoolToBrushConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, string l) =>
        new SolidColorBrush((value is bool b && b) ? Colors.LimeGreen : Colors.Gray);
    public object ConvertBack(object v, Type t, object p, string l) => throw new NotSupportedException();
}
