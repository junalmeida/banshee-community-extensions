//
// FanartService.cs
//
// Author:
//   James Willcox <snorp@novell.com>
//   Gabriel Burt <gburt@novell.com>
//   Tomasz Maczyński <tmtimon@gmail.com>
//
// Copyright 2013 Tomasz Maczyński
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using Banshee.ServiceStack;
using Hyena;
using Banshee.Configuration;
using Banshee.Sources;
using Hyena.Data.Sqlite;
using Banshee.Sources.Gui;
using Banshee.Gui;
using Banshee.Fanart.UI;
using Banshee.Collection.Gui;
using Banshee.Collection;
using Mono.Addins;
using System.Linq;

namespace Banshee.Fanart
{
    public class FanartService : IExtensionService
    {
        private bool disposed;
        private ArtistImageJob job;
        const string ExtensionId = "Banshee.Fanart"; // have this in sync with Fanart.addin.xml's <Addin id> attribute

        private bool shouldRestoreDefaultViews = false;
        private TrackFilterListView<ArtistInfo> oldArtistView;
        private TrackFilterListView<ArtistInfo> oldAlbumartistView;


        private CompositeTrackSourceContents view;

        public FanartService ()
        {
        }

        void IExtensionService.Initialize ()
        {
            PrepareViews ();
            StoreOldViewInfo ();
            InitializeFanartViews ();

            // TODO: check it:
            // TODO: add disposing
            Banshee.Metadata.MetadataService.Instance.AddProvider (
                new FanartMetadataProvider ());

            if (!ServiceManager.DbConnection.TableExists ("ArtistImageDownloads")) {
                ServiceManager.DbConnection.Execute (@"
                    CREATE TABLE ArtistImageDownloads (
                        MusicBrainzID INTEGER UNIQUE,
                        Downloaded  BOOLEAN,
                        LastAttempt INTEGER NOT NULL
                    )");
            }

            if (!ServiceManager.DbConnection.TableExists ("ArtistMusicBrainz")) {
                ServiceManager.DbConnection.Execute (@"
                    CREATE TABLE ArtistMusicBrainz (
                        ArtistName TEXT UNIQUE,
                        MusicBrainzID INTEGER,
                        LastAttempt INTEGER NOT NULL
                    )");
            }

            if (!ServiceStartup ()) {
                ServiceManager.SourceManager.SourceAdded += OnSourceAdded;
            }

            Mono.Addins.AddinManager.ExtensionChanged += OnExtensionChanged;

        }

        void OnExtensionChanged (object sender, ExtensionEventArgs args)
        {
            var addinEngine = sender as AddinEngine;
            if (addinEngine != null && addinEngine.CurrentAddin.Id == ExtensionId) {
                var addins = AddinManager.Registry.GetAddins ();
                var isEnabled = addins.Any (a => a.LocalId == ExtensionId && a.Enabled);

                if (!isEnabled) {
                    Hyena.Log.Debug ("Fanart extension is being disabled, performing cleanup");
                    OnDisabled ();
                }
            }
        }

        private bool ServiceStartup ()
        {
            if (ServiceManager.SourceManager.MusicLibrary == null) {
                return false;
            }

            Initialize ();

            return true;
        }

        private void Initialize ()
        {
            ServiceManager.SourceManager.MusicLibrary.TracksAdded += OnTracksAdded;
            ServiceManager.SourceManager.MusicLibrary.TracksChanged += OnTracksChanged;
            ServiceManager.SourceManager.MusicLibrary.TracksDeleted += OnTracksDeleted;
            FetchArtistImages ();
        }

        private void PrepareViews ()
        {
            var composite_view = ((IClientWindow)ServiceManager.Get ("NereidPlayerInterface")).CompositeView;
            if (composite_view == null) {
                throw new InvalidOperationException ("IClientWindow.CompositeView was null");
            }
            view = composite_view as CompositeTrackSourceContents;
            if (view == null) {
                throw new NotSupportedException ("IClientWindow.CompositeView needs to be of type CompositeTrackSourceContents for FanArt extension to work");
            }
        }

        private void InitializeFanartViews () {
            view.ArtistView = new FanartArtistListView ();
            view.AlbumartistView = new FanartArtistListView ();
        }

        public void FetchArtistImages ()
        {
            bool force = false;
            if (!String.IsNullOrEmpty (Environment.GetEnvironmentVariable ("BANSHEE_FORCE_ARTISTS_IMAGES_FETCH"))) {
                Log.Debug ("Forcing artists' images download session");
                force = true;
            }

            FetchArtistImages (force);
        }

        public void FetchArtistImages (bool force)
        {
            if (job == null) {
                var config_variable_name = "last_artist_image_scan";
                DateTime last_scan = DateTime.MinValue;

                if (!force) {
                    try {
                        last_scan = DatabaseConfigurationClient.Client.Get<DateTime> (config_variable_name,
                                                                                      DateTime.MinValue);
                    } catch (FormatException) {
                        Log.Warning (String.Format ("{0} is malformed, resetting to default value", 
                                                    config_variable_name));
                        DatabaseConfigurationClient.Client.Set<DateTime> (config_variable_name,
                                                                          DateTime.MinValue);
                    }
                }

                job = new ArtistImageJob (last_scan);

                job.Finished += delegate {
                    if (!job.IsCancelRequested) {
                        DatabaseConfigurationClient.Client.Set<DateTime> (config_variable_name, DateTime.Now);
                    }
                    job = null;
                };
                job.Start ();
            }
        }

        private void StoreOldViewInfo ()
        {
            if ((view.ArtistView is ArtistListView || view.ArtistView == null) &&
                (view.AlbumartistView is ArtistListView || view.AlbumartistView == null)) {
                shouldRestoreDefaultViews = true;
            } else {
                Hyena.Log.Warning ("Fanart: ArtistView or AlbumartistView is not and instance of " +
                                   "ArtistListView, Fanart extension may not function properly " +
                                   "because of other extensions that are turned on.");
                shouldRestoreDefaultViews = false;

                oldArtistView = view.ArtistView;
                oldAlbumartistView = view.AlbumartistView;
            }     
        }

        private void RestoreOldViews ()
        {
            if (shouldRestoreDefaultViews) {
                if (view != null) {
                    view.ArtistView = new ArtistListView ();
                    view.AlbumartistView = new ArtistListView ();
                }
            } else {
                view.ArtistView = oldArtistView;
                view.AlbumartistView = oldAlbumartistView;
            }
        }

        private void OnSourceAdded (SourceAddedArgs args)
        {
            if (ServiceStartup ()) {
                ServiceManager.SourceManager.SourceAdded -= OnSourceAdded;
            }
        }

        private void OnTracksAdded (Source sender, TrackEventArgs args)
        {
            FetchArtistImages ();
        }

        private void OnTracksChanged (Source sender, TrackEventArgs args)
        {
            if (args.ChangedFields == null) {
                FetchArtistImages ();
            } else {
                foreach (Hyena.Query.QueryField field in args.ChangedFields) {
                    // TODO: check that:
                    if (field == Banshee.Query.BansheeQuery.AlbumField ||
                        field == Banshee.Query.BansheeQuery.ArtistField ||
                        field == Banshee.Query.BansheeQuery.AlbumArtistField) {
                        FetchArtistImages ();
                        break;
                    }
                }
            }
        }

        private static HyenaSqliteCommand deleteQuery = new HyenaSqliteCommand (
            "DELETE FROM ArtistMusicBrainz WHERE ArtistName NOT IN (SELECT Name FROM CoreArtists)");
        private static HyenaSqliteCommand deleteQuery2 = new HyenaSqliteCommand (
            "DELETE FROM ArtistImageDownloads WHERE MusicBrainzID NOT IN (SELECT MusicBrainzID FROM CoreArtists UNION SELECT MusicBrainzID FROM ArtistMusicBrainz)");

        private void OnTracksDeleted (Source sender, TrackEventArgs args)
        {
            ServiceManager.DbConnection.Execute (deleteQuery);
            ServiceManager.DbConnection.Execute (deleteQuery2);
        }

        public void Dispose ()
        {
            if (disposed) {
                return;
            }

            RestoreOldViews ();


            ServiceManager.SourceManager.MusicLibrary.TracksAdded -= OnTracksAdded;
            ServiceManager.SourceManager.MusicLibrary.TracksChanged -= OnTracksChanged;
            ServiceManager.SourceManager.MusicLibrary.TracksDeleted -= OnTracksDeleted;

            disposed = true;
        }

        private void OnDisabled ()
        {
            // TODO: delete db tables and cache entries
        }

        #region IService implementation

        string IService.ServiceName {
            get { return "FanartService"; }
        }

        #endregion
    }
}

