//
// FanArtArtistImageSpec.cs
//
// Author:
//   Tomasz Maczyński <tmtimon@gmail.com>
//   Aaron Bockover <abockover@novell.com>
//
// Copyright 2013 Tomasz Maczyński
// Copyright (C) 2007-2008 Novell, Inc.
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
using System.IO;

using Hyena;

namespace Banshee.FanArt
{
    public static class FanArtArtistImageSpec
    {
        private static string root_path =
            Path.Combine (XdgBaseDirectorySpec.GetUserDirectory ("XDG_CACHE_HOME", ".cache"),  "media-art");

        public static string RootPath {
            get { return root_path; }
        }

        public static string CreateArtistImageFileName (string artistMBDI)
        {
            return String.Format ("artist-{0}", artistMBDI);
        }

        public static string GetPath (string filename)
        {
            return GetPathForSize (filename, 0);
        }

        public static string GetPathForSize (string filename, int size)
        {
            return size == 0
                ? Path.Combine (RootPath, String.Format ("{0}", filename)) 
                : Path.Combine (RootPath, Path.Combine (size.ToString (), String.Format ("{0}", filename)));
        }

        public static string[] GetAllFileArtistImagePaths () 
        {
            string fileMask = @"artist-*";
            //FIXME: use Banshee.IO
            return System.IO.Directory.GetFiles (RootPath, fileMask);
        }
    }
}

