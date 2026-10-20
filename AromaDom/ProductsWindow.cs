using System.Globalization;
using System.Windows;
using System.Windows.Controls;
namespace AromaDom;
public class ProductsWindow : Window
{
    public ProductsWindow(int materialId)
    {
        var material = Database.Query($"SELECT m.Name,u.Name Unit FROM Materials m JOIN Units u ON u.Id=m.UnitId WHERE m.Id={materialId}").Rows[0];
        Title = "Продукция — " + material["Name"];
        Width = 800;
        Height = 520;
        var panel = new DockPanel { Margin = new Thickness(20) };
        Content = panel;
        var title = new TextBlock { Text = Title, FontSize = 22, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,0,0,15) };
        DockPanel.SetDock(title, Dock.Top);
        panel.Children.Add(title);
        var back = new Button { Content = "Назад", IsCancel = true };
        back.Click += (_, _) => Close();
        DockPanel.SetDock(back, Dock.Bottom);
        panel.Children.Add(back);
        var table = Database.Query($"SELECT p.Name AS 'Наименование продукции',r.Quantity AS 'Количество на единицу продукции' FROM Recipes r JOIN Products p ON p.Id=r.ProductId WHERE r.MaterialId={materialId} ORDER BY p.Name");
        if (table.Rows.Count == 0)
            panel.Children.Add(new TextBlock { Text = "Материал пока не используется в продукции." });
        else
            panel.Children.Add(new DataGrid { ItemsSource = table.DefaultView, IsReadOnly = true, AutoGenerateColumns = true, CanUserAddRows = false, Background = System.Windows.Media.Brushes.White });
        title.Text += " (" + material["Unit"] + ")";
    }
}
