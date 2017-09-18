using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Episodes;
using Logger;
using Microsoft.FSharp.Core;
using System.IO;
using Microsoft.FSharp.Collections;

namespace MovieDownloader
{
    [Activity(Label = "MovieDownloader", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        int count = 1;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            Action<Logger.Logger.LogEvent> f = (logEvent =>
                {
                    var txtLog = FindViewById<EditText>(Resource.Id.txtLogger);
                    txtLog.Text = txtLog.Text + "\n" + Logger.Logger.toString(logEvent);
                });
            Logger.Logger.logger.addObserver(f);

            // Get our button from the layout resource,
            // and attach an event to it
            var button = FindViewById<Button>(Resource.Id.MyButton);
            //Episodes.EpisodeListDownloader.downloadEpisodeList("serie/stream/alle-unter-einem-dach");
            var s = EpisodeListDownloader.readSeries("alle-unter-einem-dach");
            button.Click += delegate
            {
                button.Text = string.Format("{0} clicks!", count++);
                //                var episodeDownload = new Episodes.EpisodeTypes.EpisodeDownload("/sdcard/Download/mp3", "gaaaa", ListModule.OfSeq(new string[] { "--no-check-certificate" }), ListModule.OfSeq(new string[] { "https://soundgasm.net/u/alwaysslightlysleepy/F4A-Quick-Christmas-Cuddles-short-Havenmas-2016-Req-Fill-for-uLameLunaire" }), 0);
                //               Episodes.EpisodeDownloader.downloadEpisode(episodeDownload);
                Episodes.EpisodeListDownloader.downloadEpisodeList("alle-unter-einem-dach");
            };
        }
    }
}

