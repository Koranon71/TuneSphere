using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using NAudio.Gui;
using NAudio.Wave;

namespace TuneSphere
{
    public partial class MainWindow : Window
    {
        private string connectionString = "Server=Koranon;Database=TuneSDB;Trusted_Connection=True;";
        private ObservableCollection<Track> tracks;
        private bool isPlaying = false;

        public MainWindow()
        {
            InitializeComponent();
            tracks = new ObservableCollection<Track>();
            TracksListView.ItemsSource = tracks;
            LoadTracks();
            AudioPlayer.Volume = VolumeSlider.Value;
            VolumeSlider.ValueChanged += VolumeSlider_ValueChanged;
        }

        private void CoverImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;";
            if (openFileDialog.ShowDialog() == true)
            {
                CoverImagePath.Text = openFileDialog.FileName;
                SelectedCoverImage.Source = new BitmapImage(new Uri(openFileDialog.FileName));
            }
        }
        private void AudioFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Audio Files|*.mp3;*.wav;";
            if (openFileDialog.ShowDialog() == true)
            {
                AudioFilePath.Text = openFileDialog.FileName;
            }
        }

        private void AddTrackButton_Click(object sender, RoutedEventArgs e)
        {
            string title = TitleTextBox.Text;
            string author = AuthorTextBox.Text;
            string coverImage = CoverImagePath.Text;
            string audioFile = AudioFilePath.Text;

            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(author) ||
                string.IsNullOrEmpty(coverImage) || string.IsNullOrEmpty(audioFile))
            {
                MessageBox.Show("Please fill in all fields.");
                return;
            }

            TimeSpan audioDuration;
            try
            {
                audioDuration = AudioFileHelper.GetAudioDuration(audioFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading audio file: {ex.Message}");
                return;
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "INSERT INTO tracks (title, author, cover_image, audio_file, audio_length) VALUES (@title, @author, @coverImage, @audioFile, @audioLength); SELECT SCOPE_IDENTITY();";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@title", title);
                command.Parameters.AddWithValue("@author", author);
                command.Parameters.AddWithValue("@coverImage", coverImage);
                command.Parameters.AddWithValue("@audioFile", audioFile);
                command.Parameters.AddWithValue("@audioLength", audioDuration);

                connection.Open();
                int newTrackId = Convert.ToInt32(command.ExecuteScalar());
                MessageBox.Show("Track added successfully!");

                tracks.Add(new Track
                {
                    Id = newTrackId,
                    Title = title,
                    Author = author,
                    AudioLength = audioDuration.ToString(@"hh\:mm\:ss"),
                    AudioFile = audioFile,
                    CoverImage = new BitmapImage(new Uri(coverImage))
                });
            }
        }

        private void LoadTracks()
        {
            tracks.Clear();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT id, title, author, audio_length, audio_file, cover_image FROM tracks";
                SqlCommand command = new SqlCommand(query, connection);
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = reader.GetInt32(0);
                        string title = reader.GetString(1);
                        string author = reader.GetString(2);
                        string audioLength = reader.GetTimeSpan(3).ToString(@"hh\:mm\:ss");
                        string audioFile = reader.GetString(4);
                        string coverImagePath = reader.GetString(5);
                        tracks.Add(new Track
                        {
                            Id = id,
                            Title = title,
                            Author = author,
                            AudioLength = audioLength,
                            AudioFile = audioFile,
                            CoverImage = new BitmapImage(new Uri(coverImagePath))
                        });
                    }
                }
            }
        }

        private void DeleteTrackButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int trackId)
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = "DELETE FROM tracks WHERE id = @id";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@id", trackId);

                    connection.Open();
                    command.ExecuteNonQuery();
                }

                // Remove the track from the list
                var trackToRemove = tracks.FirstOrDefault(t => t.Id == trackId);
                if (trackToRemove != null)
                {
                    tracks.Remove(trackToRemove);
                }
            }
        }

        private void PlayTrackButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string audioFile)
            {
                AudioPlayer.Source = new Uri(audioFile);
                AudioPlayer.Play();
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            AudioPlayer.Play();
        }
        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (isPlaying)
            {
                AudioPlayer.Pause();
                PlayPauseButton.Content = "Play";
            }
            else
            {
                AudioPlayer.Play();
                PlayPauseButton.Content = "Pause";
            }
            isPlaying = !isPlaying;
        }
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            AudioPlayer.Stop();
            isPlaying = false;
            PlayPauseButton.Content = "Play";
        }
        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Math.Abs(e.NewValue - AudioPlayer.Volume) > 0.01)
            {
                AudioPlayer.Volume = e.NewValue;
            }
        }
    }

    public class Track
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string AudioLength { get; set; }
        public string AudioFile { get; set; }
        public BitmapImage CoverImage { get; set; }
    }

    public static class AudioFileHelper
    {
        public static TimeSpan GetAudioDuration(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            }

            using (var audioFileReader = new AudioFileReader(filePath))
            {
                return audioFileReader.TotalTime;
            }
        }
    }
}