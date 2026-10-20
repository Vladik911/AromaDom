using System.Globalization;
using System.Windows;
using System.Windows.Controls;
namespace AromaDom;
public class CalculatorWindow : Window
{
    public CalculatorWindow()
    {
        Title = "Калькулятор выпуска продукции";
        Width = 560;
        Height = 620;
        var panel = new StackPanel { Margin = new Thickness(24) };
        Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var product = new ComboBox { DisplayMemberPath = "Name", SelectedValuePath = "Id", ItemsSource = Database.Query("SELECT Id,Name FROM ProductTypes").DefaultView, SelectedIndex = 0 };
        var material = new ComboBox { DisplayMemberPath = "Name", SelectedValuePath = "Id", ItemsSource = Database.Query("SELECT Id,Name FROM MaterialTypes").DefaultView, SelectedIndex = 0 };
        var raw = new TextBox { Text = "100" };
        var first = new TextBox { Text = "1" };
        var second = new TextBox { Text = "1" };
        void Field(string label, Control control)
        {
            panel.Children.Add(new TextBlock { Text = label });
            panel.Children.Add(control);
        }
        Field("Тип продукции", product);
        Field("Тип материала", material);
        Field("Количество сырья (целое)", raw);
        Field("Первый параметр продукции", first);
        Field("Второй параметр продукции", second);
        var result = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,12,0,12), FontSize = 20 };
        var calculate = new Button { Content = "Рассчитать", IsDefault = true };
        calculate.Click += (_, _) =>
        {
            try
            {
                if (!int.TryParse(raw.Text, out var quantity) || !double.TryParse(first.Text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var a) || !double.TryParse(second.Text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var b))
                    throw new Exception("Введите целое количество сырья и два положительных числовых параметра.");
                var count = ProductionCalculator.Calculate(int.Parse(product.SelectedValue.ToString()!), int.Parse(material.SelectedValue.ToString()!), quantity, a, b);
                if (count == -1)
                    throw new Exception("Проверьте ввод: сырьё не меньше нуля, параметры больше нуля, результат в пределах Int32.");
                result.Text = "Получится продукции: " + count + " шт.";
            }
            catch (Exception ex)
            {
                MainWindow.Error(ex);
            }
        };
        panel.Children.Add(calculate);
        panel.Children.Add(result);
        var back = new Button { Content = "Назад", IsCancel = true };
        back.Click += (_, _) => Close();
        panel.Children.Add(back);
    }
}
