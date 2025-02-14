using System;
using Xamarin.Forms;
using LibVLCSharp.Shared;
using LibVLCSharp.Forms.Shared;
using System.Diagnostics;
using Xam.Forms.VideoPlayer;
using System.Threading.Tasks;
using System.Collections.Generic;
using Player.Models;

namespace Player
{
    public partial class MainPage : ContentPage
    {
        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;
        private VideoView _videoView;
        private float _position;
        private string videoUrl = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4";
        private Dictionary<TimeSpan, List<Question>> _questionGroups;
        private Dictionary<TimeSpan, int> _currentQuestionIndex;
        private bool _isSliderUpdating;

        public MainPage()
        {
            InitializeComponent();
            SetupVideoPlayer();
            LoadQuestions();

            //Handle lifecycle events
            MessagingCenter.Subscribe<string>(this, "OnPause", app =>
            {
                if (_videoView != null)
                {
                    _videoView.MediaPlayerChanged -= MediaPlayerChanged;
                }
                if (_mediaPlayer != null)
                {
                    _mediaPlayer.Pause();
                    _position = _mediaPlayer.Position;
                    _mediaPlayer.Stop();
                }
                VideoContainer.Content = null;
                Debug.WriteLine($"saving mediaplayer position {_position}");
            });

            MessagingCenter.Subscribe<string>(this, "OnRestart", app =>
            {
                if (Device.RuntimePlatform == Device.UWP)
                {
                    SetupUwpPlayer();
                }
                else
                {
                    _videoView = new VideoView { HorizontalOptions = LayoutOptions.FillAndExpand, VerticalOptions = LayoutOptions.FillAndExpand };
                    VideoContainer.Content = _videoView;

                    _videoView.MediaPlayerChanged += MediaPlayerChanged;

                    _videoView.MediaPlayer = _mediaPlayer;
                    _videoView.MediaPlayer.Position = _position;
                    _position = 0;
                }
            });

            Device.StartTimer(TimeSpan.FromSeconds(1), () =>
            {
                UpdateSlider();
                CheckForQuestions();
                return true;
            });
        }

        private void SetupVideoPlayer()
        {
            if (Device.RuntimePlatform == Device.Android || Device.RuntimePlatform == Device.iOS)
            {
                SetupVlcPlayer();
            }
            else if (Device.RuntimePlatform == Device.UWP)
            {
                SetupUwpPlayer();
            }

            // Add the selected video player to the UI
            VideoContainer.Content = _videoView;
        }

        private void SetupVlcPlayer()
        {
            Core.Initialize();
            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC);

            _videoView = new VideoView
            {
                MediaPlayer = _mediaPlayer,
                VerticalOptions = LayoutOptions.FillAndExpand,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            var media = new Media(_libVLC, new Uri(videoUrl));
            _mediaPlayer.Media = media;
        }

        private void SetupUwpPlayer()
        {
            var uwpPlayer = new Xam.Forms.VideoPlayer.VideoPlayer
            {
                Source = new UriVideoSource { Uri = videoUrl },
                AutoPlay = false,
                VerticalOptions = LayoutOptions.FillAndExpand,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            VideoContainer.Content = uwpPlayer;
        }

        private void OnPlayButtonClicked(object sender, EventArgs e)
        {
            if (Device.RuntimePlatform == Device.Android || Device.RuntimePlatform == Device.iOS)
            {
                _mediaPlayer?.Play();
            }
            else if (Device.RuntimePlatform == Device.UWP)
            {
                (VideoContainer.Content as Xam.Forms.VideoPlayer.VideoPlayer)?.Play();
            }
        }

        //Pause when app goes to background
        public void OnAppSleep(Object sender, EventArgs e)
        {
            MessagingCenter.Send("App", "OnPause");
        }

        //Resume when app comes back to foreground
        public void OnAppResumed(Object sender, EventArgs e)
        {
            MessagingCenter.Send("App", "OnRestart");
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (Device.RuntimePlatform == Device.Android || Device.RuntimePlatform == Device.iOS)
            {
                _videoView.MediaPlayerChanged += MediaPlayerChanged;

                Core.Initialize();

                _libVLC = new LibVLC();
                var media = new Media(_libVLC, new Uri(videoUrl));

                _mediaPlayer = new MediaPlayer(_libVLC)
                {
                    Media = media
                };

                _videoView.MediaPlayer = _mediaPlayer;
            }
            else if (Device.RuntimePlatform == Device.UWP)
            {
                var uwpPlayer = new Xam.Forms.VideoPlayer.VideoPlayer
                {
                    Source = new UriVideoSource { Uri = videoUrl },
                    AutoPlay = true,
                    VerticalOptions = LayoutOptions.FillAndExpand,
                    HorizontalOptions = LayoutOptions.FillAndExpand
                };

                VideoContainer.Content = uwpPlayer;
                uwpPlayer.Play();
            }
        }

        private void MediaPlayerChanged(object sender, EventArgs e)
        {
            _mediaPlayer.Play();
        }

        private void LoadQuestions()
        {
            _questionGroups = new Dictionary<TimeSpan, List<Question>>();
            _currentQuestionIndex = new Dictionary<TimeSpan, int>();

            var group1Time = TimeSpan.FromSeconds(20);
            var group2Time = TimeSpan.FromSeconds(30);

            _questionGroups[group1Time] = new List<Question>
            {
                new Question
                {
                    QuestionText = "What is the capital of France?",
                    Options = new List<string> { "Paris", "London", "Berlin", "Madrid" },
                    CorrectAnswer = "Paris"
                },
                new Question
                {
                    QuestionText = "Who won the 2018 FIFA World Cup?",
                    Options = new List<string> { "Brazil", "Germany", "Spain", "Argentina" },
                    CorrectAnswer = "Germany"
                },
                new Question
                {
                    QuestionText = "What is the name of the famous painting by Leonardo da Vinci?",
                    Options = new List<string> { "Mona Lisa", "The Last Supper", "The Starry Night", "The Creation of Adam" },
                    CorrectAnswer = "Mona Lisa"
                }
            };

            _questionGroups[group2Time] = new List<Question>
            {
                new Question
                {
                    QuestionText = "Who is the current Prime Minister of Australia?",
                    Options = new List<string> { "Joe Biden", "George Osborne", "David Cameron", "Australian Labor Party Leader" },
                    CorrectAnswer = "David Cameron"
                },
                new Question
                {
                    QuestionText = "What is the capital city of Sweden?",
                    Options = new List<string> { "Stockholm", "Copenhagen", "London", "Berlin" },
                    CorrectAnswer = "Stockholm"
                },
            };
            foreach (var timestamp in _questionGroups.Keys)
            {
                _currentQuestionIndex[timestamp] = 0;
            }
        }

        private void ShowNextQuestion(TimeSpan timestamp)
        {
            int index = _currentQuestionIndex[timestamp];

            if (index < _questionGroups[timestamp].Count)
            {
                Question question = _questionGroups[timestamp][index];

                // Display question in WebView
                ShowQuestionPopup(question, timestamp);
            }
            else
            {
                // All questions at this timestamp are answered, resume the video
                _mediaPlayer.Play();
            }
        }

        private void ShowQuestionPopup(Question question, TimeSpan timestamp)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                // Stop video playback
                _mediaPlayer.Pause();

                // Load question into WebView
                var html = GenerateQuestionHtml(question);
                QuestionWebView.Source = new HtmlWebViewSource { Html = html };

                // Show WebView
                QuestionWebView.IsVisible = true;
            });
        }

        private string GenerateQuestionHtml(Question question)
        {
            var optionsHtml = string.Join("", question.Options.ConvertAll(option => $"<button onclick=\"window.external.notify('{option}')\">{option}</button>"));
            return $@"
                <html>
                <body>
                    <h2>{question.QuestionText}</h2>
                    {optionsHtml}
                </body>
                </html>";
        }

        private void UpdateSlider()
        {
            if (_mediaPlayer == null || _mediaPlayer.Media == null || _isSliderUpdating)
                return;

            _isSliderUpdating = true;
            Device.BeginInvokeOnMainThread(() =>
            {
                DurationSlider.Maximum = _mediaPlayer.Media.Duration / 1000.0;
                DurationSlider.Value = _mediaPlayer.Time / 1000.0;
                _isSliderUpdating = false;
            });
        }

        private void CheckForQuestions()
        {
            if (_mediaPlayer == null)
                return;

            var currentTime = TimeSpan.FromMilliseconds(_mediaPlayer.Time);
            foreach (var timestamp in _questionGroups.Keys)
            {
                if (currentTime >= timestamp && _currentQuestionIndex[timestamp] < _questionGroups[timestamp].Count)
                {
                    ShowNextQuestion(timestamp);
                    break;
                }
            }
        }
    }

    public class Question
    {
        public string QuestionText { get; set; }
        public List<string> Options { get; set; }
        public string CorrectAnswer { get; set; }
    }
}
