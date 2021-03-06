//
// RecommendationProvider.cs
//
// Author:
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
using System.Collections.Generic;

using Banshee.ServiceStack;
using Hyena.Data.Sqlite;

namespace Banshee.SongKick.Search
{
    public class RecommendationProvider
    {
        /*
        DB queries that list top artists (those which have tracks rated 4 or 5):
        
        SELECT DISTINCT CoreArtists.Name, CoreArtists.MusicBrainzID FROM 
          CoreTracks
        JOIN 
          CoreArtists 
        WHERE 
            CoreTracks.Rating >=4
          AND
            CoreTracks.ArtistID=CoreArtists.ArtistID;

        or

        SELECT CoreArtists.Name, CoreArtists.MusicBrainzID FROM 
            (SELECT DISTINCT ArtistID FROM CoreTracks WHERE CoreTracks.Rating >=4) AS TopTracks 
          NATURAL JOIN 
            CoreArtists;
        */

        private const string topArtistsQuery = @"
                SELECT DISTINCT CoreArtists.Name, CoreArtists.MusicBrainzID FROM 
                  CoreTracks
                JOIN 
                  CoreArtists 
                WHERE 
                    CoreTracks.Rating >=4
                  AND
                    CoreTracks.ArtistID=CoreArtists.ArtistID;";

        public IEnumerable<RecommendedArtist> GetRecommendations ()
        {
            var command = new HyenaSqliteCommand (topArtistsQuery);

            using (IDataReader reader = ServiceManager.DbConnection.Query (command)) {
                while (reader.Read ()) {
                    var artistName = reader.Get<string> (0);
                    var artistMusicBrainzId = reader.Get<string> (1);

                    var artist = new RecommendedArtist (artistName, artistMusicBrainzId);

                    yield return artist;
                }
            }
        }
    }
}

