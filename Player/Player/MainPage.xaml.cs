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

            
            // Enable JavaScript in WebView
            QuestionWebView.Navigating += QuestionWebView_Navigating;

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
                    _videoView = new VideoView
                    {
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                        VerticalOptions = LayoutOptions.FillAndExpand
                    };
                    VideoContainer.Content = _videoView;

                    if (_mediaPlayer == null)
                    {
                        SetupVlcPlayer();
                    }

                    _videoView.MediaPlayer = _mediaPlayer;
                    if (_mediaPlayer.Media != null)
                    {
                        _mediaPlayer.Play();
                        _mediaPlayer.Time = (long)(_position * 1000); // Restore position
                    }
                }
            });

        }
        private void QuestionWebView_Navigating(object sender, WebNavigatingEventArgs e)
        {
            if (e.Url.StartsWith("js:"))
            {
                e.Cancel = true; // Prevent actual navigation
                string selectedOption = e.Url.Substring(3); // Extract selected answer
                HandleWebViewMessage(selectedOption);
            }
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

        private async void SetupVlcPlayer()
        {
            try
            {
                await Task.Run(() =>
                {
                    Core.Initialize();
                    _libVLC = new LibVLC();
                    _mediaPlayer = new MediaPlayer(_libVLC);

                    var media = new Media(_libVLC, videoUrl, FromType.FromLocation);
                    media.AddOption(":network-caching=3000");
                    media.AddOption(":file-caching=3000");
                    media.AddOption(":live-caching=3000");
                    media.AddOption(":disk-caching=3000");
                    media.AddOption(":rtsp-caching=3000");

                    _mediaPlayer.Media = media;
                });

                _videoView = new VideoView
                {
                    MediaPlayer = _mediaPlayer,
                    VerticalOptions = LayoutOptions.FillAndExpand,
                    HorizontalOptions = LayoutOptions.FillAndExpand
                };

                _mediaPlayer.Play();
                Device.StartTimer(TimeSpan.FromMilliseconds(1000), () =>
                {
                    UpdateSlider();
                    CheckForQuestions();
                    return true;
                });
                Debug.WriteLine("VLC PLAYER SETUP COMPLETE");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting up VLC player: {ex.Message}");
            }
        }



        private void CheckForQuestions()
        {
            if (_mediaPlayer == null)
                return;
            if(_questionGroups == null || _questionGroups.Count == 0)
            {
                return;
            }

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

        //private void OnPlayButtonClicked(object sender, EventArgs e)
        //{
        //    if (Device.RuntimePlatform == Device.Android || Device.RuntimePlatform == Device.iOS)
        //    {
        //        _mediaPlayer?.Play();
        //    }
        //    else if (Device.RuntimePlatform == Device.UWP)
        //    {
        //        (VideoContainer.Content as Xam.Forms.VideoPlayer.VideoPlayer)?.Play();
        //    }
        //}

        

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
                if (_videoView == null)
                {
                    _videoView = new VideoView
                    {
                        VerticalOptions = LayoutOptions.FillAndExpand,
                        HorizontalOptions = LayoutOptions.FillAndExpand
                    };
                    VideoContainer.Content = _videoView; // Add VideoView to the UI
                }

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


        //protected override void OnDisappearing()
        //{
        //    base.OnDisappearing();
        //    MessagingCenter.Unsubscribe<string>(this, "OnPause");
        //    MessagingCenter.Unsubscribe<string>(this, "OnRestart");
        //}

        private void MediaPlayerChanged(object sender, EventArgs e)
        {
            _mediaPlayer.Play();
        }

        private void LoadQuestions()
        {
            _questionGroups = new Dictionary<TimeSpan, List<Question>>();
            _currentQuestionIndex = new Dictionary<TimeSpan, int>();

            var group1Time = TimeSpan.FromSeconds(10);
            var group2Time = TimeSpan.FromSeconds(20);

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
            var optionsHtml = string.Join("", question.Options.ConvertAll(option =>
                $"<div style='margin: 10px 0;'><button style='width: 100%; padding: 10px;' onclick=\"sendMessage('{option}')\">{option}</button></div>"));

            return $@"
            <html>
            <head>
                <style>
                    body {{
                        font-family: Arial, sans-serif;
                        text-align: center;
                        padding: 20px;
                    }}
                    h2 {{
                        margin-bottom: 20px;
                    }}
                </style>
                <script type='text/javascript'>
                    function sendMessage(option) {{
                        window.location.href = 'js:' + option;
                    }}
                </script>
            </head>
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
                var duration = _mediaPlayer.Media.Duration;
                if (duration > 0)
                {
                    DurationSlider.Maximum = duration / 1000.0;
                    DurationSlider.Value = _mediaPlayer.Time / 1000.0;
                }
                _isSliderUpdating = false;
            });
        }

        

        private void HandleWebViewMessage(string message)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                var currentTime = TimeSpan.FromMilliseconds(_mediaPlayer.Time);
                foreach (var timestamp in _questionGroups.Keys)
                {
                    if (currentTime >= timestamp && _currentQuestionIndex[timestamp] < _questionGroups[timestamp].Count)
                    {
                        var question = _questionGroups[timestamp][_currentQuestionIndex[timestamp]];
                        if (message == question.CorrectAnswer)
                        {
                            _currentQuestionIndex[timestamp]++;
                            if (_currentQuestionIndex[timestamp] < _questionGroups[timestamp].Count)
                            {
                                ShowNextQuestion(timestamp);
                            }
                            else
                            {
                                QuestionWebView.IsVisible = false;
                                _mediaPlayer.Play();
                            }
                        }
                        else
                        {
                            var html = GenerateQuestionHtmlWithFeedback(question, message);
                            QuestionWebView.Source = new HtmlWebViewSource { Html = html };
                        }
                        break;
                    }
                }
            });
        }

        private string GenerateQuestionHtmlWithFeedback(Question question, string selectedOption)
        {
            var optionsHtml = string.Join("", question.Options.ConvertAll(option =>
                $"<button onclick=\"window.external.notify('{option}')\" style=\"background-color:{(option == selectedOption ? "red" : "lightgray")}\">{option}</button>"));
            return $@"
            <html>
            <body>
                <h2>{question.QuestionText}</h2>
                {optionsHtml}
                <p style='color: red;'>Wrong Answer! Try again.</p>
            </body>
            </html> ";
        }
    }

    public class Question
    {
        public string QuestionText { get; set; }
        public List<string> Options { get; set; }
        public string CorrectAnswer { get; set; }
    }
}
