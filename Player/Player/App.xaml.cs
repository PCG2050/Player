using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Player
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new MainPage();
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
            //(MainPage as MainPage)?.OnAppSleep(this, EventArgs.Empty);
        }

        protected override void OnResume()
        {
            //(MainPage as MainPage)?.OnAppResumed(this, EventArgs.Empty);
        }
    }
}
