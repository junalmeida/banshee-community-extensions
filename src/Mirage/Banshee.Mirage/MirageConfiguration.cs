/*
 * Mirage - High Performance Music Similarity and Automatic Playlist Generator
 * http://hop.at/mirage
 *
 * Copyright (C) 2007 Dominik Schnitzer <dominik@schnitzer.at>
 *           (C) 2008 Bertrand Lorentz <bertrand.lorentz@gmail.com>
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor,
 * Boston, MA  02110-1301, USA.
 */

using System;

using Banshee.Configuration;

namespace Banshee.Mirage
{
    public static class MirageConfiguration
    {
        public static readonly SchemaEntry<bool> ClearOnQuitSchema = new SchemaEntry<bool> (
            "plugins.mirage", "clear_on_quit",
            false,
            "Clear on Quit",
            "Clear the playlist when quitting"
        );

        public static readonly SchemaEntry<int> PlaylistLength = new SchemaEntry<int> (
            "plugins.mirage", "playlist_length",
            5,
            "Playlist length",
            "Number of tracks in the playlist generated by Mirage"
        );

        public static readonly SchemaEntry<double> DistanceCeiling = new SchemaEntry<double> (
            "plugins.mirage", "distance_ceiling",
            0,
            "Minimum distance between tracks",
            "Mirage calculates a distance between tracks that reflects their similarity. " +
                "A track whose distance to the seeds is below this value will be ignored. " +
                "Legitimate library duplicates such as a track being on a studio album and a " +
                "greatest hits album should have a distance of less than 1. A larger value " +
                "will catch things such as instrumental/karaoke versions, though it also " +
                "will catch unrelated tracks"
        );

        public static readonly SchemaEntry<bool> AutoScan = new SchemaEntry<bool> (
            "plugins.mirage", "auto_scan",
            true,
            "Automatically scan the music library for new tracks and analyze them with Mirage.",
            "Activate to automatically scan the music library for new tracks when Mirage starts. " +
                "Set to false to disable automatic scanning."
        );

    }
}
