using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using NAudio.Wave;

namespace TuneSphere
{
    public partial class MainWindow : Window
    {
        private string dbFilePath = "DB.txt";
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
                MessageBox.Show("Заполните все поля перед добавлением файла");
                return;
            }
            TimeSpan audioDuration;
            try
            {
                audioDuration = AudioFileHelper.GetAudioDuration(audioFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка чтения аудио файла: {ex.Message}");
                return;
            }
            int newTrackId = tracks.Count > 0 ? tracks.Max(t => t.Id) + 1 : 1;
            string trackData = $"{newTrackId},{title},{author},{audioDuration},{audioFile},{coverImage}";
            File.AppendAllText(dbFilePath, trackData + Environment.NewLine);
            tracks.Add(new Track
            {
                Id = newTrackId,
                Title = title,
                Author = author,
                AudioLength = audioDuration.ToString(@"hh\:mm\:ss"),
                AudioFile = audioFile,
                CoverImage = new BitmapImage(new Uri(coverImage))
            });
            MessageBox.Show("Трек успешно добавлен");
        }
        private void LoadTracks()
        {
            tracks.Clear();

            if (File.Exists(dbFilePath))
            {
                var lines = File.ReadAllLines(dbFilePath);
                foreach (var line in lines)
                {
                    var parts = line.Split(',');

                    if (parts.Length == 6)
                    {
                        int id = int.Parse(parts[0]);
                        string title = parts[1];
                        string author = parts[2];
                        TimeSpan audioLength = TimeSpan.Parse(parts[3]);
                        string audioFile = parts[4];
                        string coverImagePath = parts[5];
                        tracks.Add(new Track
                        {
                            Id = id,
                            Title = title,
                            Author = author,
                            AudioLength = audioLength.ToString(@"hh\:mm\:ss"),
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
                var trackToRemove = tracks.FirstOrDefault(t => t.Id == trackId);
                if (trackToRemove != null)
                {
                    tracks.Remove(trackToRemove);
                }
                File.WriteAllLines(dbFilePath, tracks.Select(t => $"{t.Id},{t.Title},{t.Author},{t.AudioLength},{t.AudioFile},{t.CoverImage}"));
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
                PlayPauseButton.Content = "▶️";
            }
            else
            {
                AudioPlayer.Play();
                PlayPauseButton.Content = "⏸️";
            }
            isPlaying = !isPlaying;
        }
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            AudioPlayer.Stop();
            isPlaying = false;
            PlayPauseButton.Content = "▶️";
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
                throw new ArgumentException("Путь к файлу не может быть пустым", nameof(filePath));
            }
            using (var audioFileReader = new AudioFileReader(filePath))
            {
                return audioFileReader.TotalTime;
            }
        }
    }
}