using System.Windows;

namespace music_player;

public partial class PlaylistNameWindow : Window
{
    public string PlaylistName { get; private set; } = "";

    public PlaylistNameWindow(string title, string defaultName = "")
    {
        InitializeComponent();

        Title = title;
        NameTextBox.Text = defaultName;

        NameTextBox.Focus();
        NameTextBox.SelectAll();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        PlaylistName = NameTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(PlaylistName))
        {
            MessageBox.Show(
                "Please enter a playlist name.",
                "Invalid Name",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}