//
// Live365Plugin.cs
//
// Authors:
//   Frank Ziegler <funtastix@googlemail.com>
//
// Copyright (C) 2010 Frank Ziegler
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Collections.Generic;

using Banshee.Base;
using Banshee.Collection.Database;

using Hyena;
using Banshee.Configuration;

namespace Banshee.LiveRadio.Plugins
{


    public class Live365Plugin : LiveRadioBasePlugin
    {

        private const string base_url = "http://www.live365.com";
        private const string request_url = "/cgi-bin/directory.cgi?site=xml&app_id=BansheeExtension&access=all&version=4&rows=200&charset=UTF-8";
        private const string genre_request = "&genre=";
        private const string freetext_request = "&searchdesc=";
        private const string genre_url = "/cgi-bin/api_genres.cgi?site=xml&app_id=Banshee&access=all&version=4&charset=UTF-8";

        public Live365Plugin () : base (true)
        {
            use_proxy = UseProxyEntry.Get ().Equals ("True") ? true : false;
            use_credentials = UseCredentialsEntry.Get ().Equals ("True") ? true : false;

            if (!Int32.TryParse(HttpTimeoutEntry.Get (), out http_timeout_seconds))
                http_timeout_seconds = 20;
            credentials_username = HttpUsernameEntry.Get ();
            credentials_password = HttpPasswordEntry.Get ();
            proxy_url = ProxyUrlEntry.Get ();
        }

        protected override void RetrieveGenres ()
        {
            ParseGenres(RetrieveXml(base_url + genre_url));
        }

        protected override void RetrieveRequest (LiveRadioRequestType request_type, string query)
        {
            string request;
            if (request_type == LiveRadioRequestType.ByGenre) {
                request = base_url + request_url + genre_request + query;
            } else {
                request = base_url + request_url + freetext_request + query;
            }
            XmlDocument document = RetrieveXml(request);
            Log.Debug ("[Live365Plugin] <RetrieveRequest> Start Parsing");
            if (document != null) ParseXmlResponse(document, request_type, query);
        }

        private void ParseGenres(XmlDocument doc)
        {
            Log.Debug ("[Live365Plugin] <ParseGenres> START");
            
            XmlNodeList XML_genre_nodes = doc.GetElementsByTagName ("Genre");
            Log.DebugFormat ("[Live365Plugin] <ParseGenres> {0} nodes found", XML_genre_nodes.Count);

            List<Genre> new_genres = new List<Genre> ();

            foreach (XmlNode node in XML_genre_nodes) {
                XmlNodeList data_nodes = node.ChildNodes;

                foreach (XmlNode data_node in data_nodes)
                {
                    try {
                        if (data_node.LocalName.Equals("Name"))
                        {
                            Genre genre = new Genre(data_node.InnerText);

                            if (!new_genres.Contains (genre))
                                new_genres.Add (genre);
                        }
                    } catch (Exception ex) {
                        Log.Exception ("[Live365Plugin] <ParseGenres> ERROR", ex);
                        continue;
                    }
                }

            }
            
            new_genres.Sort ();
            genres = new_genres;

            Log.DebugFormat ("[Live365Plugin] <ParseGenres> {0} genres found", genres.Count);
        }

        private void ParseXmlResponse (XmlDocument xml_response, LiveRadioRequestType request_type, string query)
        {
            Log.Debug ("[Live365Plugin] <ParseXmlResponse> Start");

            List<string> pages = new List<string> ();
            pages.Add(null);
            XmlNodeList pagination_nodes = xml_response.GetElementsByTagName ("PAGINATION_PAGES");

            foreach (XmlNode node in pagination_nodes)
            {
                string url = null;
                bool url_valid = false;
                XmlNodeList page_nodes = node.ChildNodes;
                foreach (XmlNode page_node in page_nodes)
                {
                    try {
                        if (page_node.LocalName.Equals("PAGINATION_DESC")
                            && !page_node.InnerText.Trim ().Equals ("1")
                            && !page_node.InnerText.Trim ().Equals ("Next"))
                            url_valid = true;

                        if (page_node.LocalName.Equals("PAGINATION_LINK"))
                            url = page_node.InnerText;

                    }
                    catch (Exception e) {
                        Log.Exception ("[Live365Plugin] <ParseXmlResponse> ERROR: ", e);
                        return;
                    }
                }
                if (!String.IsNullOrEmpty(url) && url_valid)
                        pages.Add(url);
            }

            Log.Debug ("[Live365Plugin] <ParseXmlResponse> analyzing stations");

            string key;
            if (request_type == LiveRadioRequestType.ByGenre) {
                key = "Genre:" + query;
                if (!cached_results.ContainsKey (key)) {
                    cached_results[key] = new List<DatabaseTrackInfo> ();
                }
            } else {
                key = query;
                if (!cached_results.ContainsKey (key)) {
                    cached_results[key] = new List<DatabaseTrackInfo> ();
                }
            }

            cached_results[key].Clear ();

            foreach (string page in pages)
            {
                XmlDocument doc;
                if (String.IsNullOrEmpty(page)) {
                    doc = xml_response;
                } else {
                    doc = RetrieveXml(page);
                }

                XmlNodeList XML_station_nodes = doc.GetElementsByTagName ("LIVE365_STATION");

                foreach (XmlNode node in XML_station_nodes)
                {
                    string name = null;
                    string access = null;
                    string ex_id = null;
                    string url = null;
                    string broadcaster = null;
                    string description = null;
                    string keywords = null;
                    string genre = null;
                    string bitrate = null;
                    string rating = null;
                    string status = null;
                    string location = null;

                    XmlNodeList childnodes = node.ChildNodes;
                    foreach (XmlNode child in childnodes)
                    {

                        try {
                            if (child.LocalName.Equals ("STATION_ADDRESS"))
                                url = child.InnerText;
                            else if (child.LocalName.Equals ("STATION_ID"))
                                ex_id = child.InnerText;
                            else if (child.LocalName.Equals ("STATION_BROADCASTER"))
                                broadcaster = child.InnerText;
                            else if (child.LocalName.Equals ("STATION_TITLE"))
                                name = child.InnerText;
                            else if (child.LocalName.Equals ("STATION_DESCRIPTION"))
                                description = child.InnerText;
                            else if (child.LocalName.Equals ("STATION_KEYWORDS"))
                                keywords = child.InnerText;
                            else if (child.LocalName.Equals ("STATION_GENRE"))
                                genre = child.InnerText;
                            else if (child.LocalName.Equals ("STATION_CONNECTION"))
                                bitrate = child.InnerText;
                            else if (child.LocalName.Equals ("STATION_RATING"))
                                rating = child.InnerText;
                            else if (child.LocalName.Equals ("STATION_STATUS"))
                                status = child.InnerText;
                            else if (child.LocalName.Equals ("LISTENER_ACCESS"))
                                access = child.InnerText;
                            else if (child.LocalName.Equals ("STATION_LOCATION"))
                                location = child.InnerText;

                        }
                        catch (Exception e) {
                            Log.Exception ("[Live365Plugin] <ParseXmlResponse> ERROR: ", e);
                            continue;
                        }
                    }

                    int bitrate_int;
                    int id_int;
                    int rating_int;

                    if (!String.IsNullOrEmpty (access)
                        && !String.IsNullOrEmpty (status)
                        && access.Equals ("PUBLIC")
                        && status.Equals ("OK"))
                    {

                        DatabaseTrackInfo new_station = new DatabaseTrackInfo ();

                        new_station.Uri = new SafeUri (url);
                        new_station.ArtistName = broadcaster;
                        new_station.Genre = genre;
                        new_station.TrackTitle = name;
                        new_station.Comment = description + location + keywords;
                        Int32.TryParse (rating.Trim (), out rating_int);
                        new_station.Rating = rating_int;
                        Int32.TryParse (ex_id.Trim (), out id_int);
                        new_station.ExternalId = id_int;
                        new_station.PrimarySource = source;
                        new_station.IsLive = true;
                        Int32.TryParse (bitrate.Trim (), out bitrate_int);
                        new_station.BitRate = bitrate_int;
                        new_station.IsLive = true;

                        cached_results[key].Add (new_station);
                    }
                }
            }

            Log.Debug ("[Live365Plugin] <ParseXmlResponse> End");
        }

        public override string Name {
            get { return "live365.com"; }
        }

        public override void SaveConfiguration ()
        {
            if (configuration_widget == null) return;
            http_timeout_seconds = configuration_widget.HttpTimeout;
            credentials_password = configuration_widget.HttpPassword;
            credentials_username = configuration_widget.HttpUsername;
            proxy_url = configuration_widget.ProxyUrl;
            use_credentials = configuration_widget.UseCredentials;
            use_proxy = configuration_widget.UseProxy;
            HttpTimeoutEntry.Set (http_timeout_seconds.ToString ());
            HttpPasswordEntry.Set (credentials_password);
            HttpUsernameEntry.Set (credentials_username);
            ProxyUrlEntry.Set (proxy_url);
            UseCredentialsEntry.Set (use_credentials.ToString ());
            UseProxyEntry.Set (use_proxy.ToString ());
        }

        public static readonly SchemaEntry<string> UseProxyEntry = new SchemaEntry<string> (
        "plugins.liveradio.live365" , "use_proxy", "", "whether to use proxy for HTTP", "whether to use proxy for HTTP");

        public static readonly SchemaEntry<string> ProxyUrlEntry = new SchemaEntry<string> (
        "plugins.liveradio.live365", "proxy_url", "", "HTTP proxy url", "HTTP proxy url");

        public static readonly SchemaEntry<string> UseCredentialsEntry = new SchemaEntry<string> (
        "plugins.liveradio.live365", "use_credentials", "", "whether to use credentials authentification", "whether to use credentials authentification");

        public static readonly SchemaEntry<string> HttpUsernameEntry = new SchemaEntry<string> (
        "plugins.liveradio.live365", "credentials_username", "", "HTTP username", "HTTP username");

        public static readonly SchemaEntry<string> HttpPasswordEntry = new SchemaEntry<string> (
        "plugins.liveradio.live365", "credentials_password", "", "HTTP password", "HTTP password");

        public static readonly SchemaEntry<string> HttpTimeoutEntry = new SchemaEntry<string> (
        "plugins.liveradio.live365", "http_timeout_seconds", "", "HTTP timeout", "HTTP timeout");

    }
    
}