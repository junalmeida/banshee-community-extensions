﻿//
// SongKickGeoLocationSource.fs 
//
// Authors:
//   Dmitrii Petukhov <dimart.sp@gmail.com>
//
// Copyright (C) 2014 Dmitrii Petukhov
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

namespace Banshee.SongKickGeoLocation

open FSharp.Data

open Banshee.SongKick.LocationProvider
open Banshee.ServiceStack
open Banshee.Sources

open Hyena
open Hyena.Data

open Mono.Addins

module Constants =
    let NAME = "SongKickGeoLocation"

type GeoLocation = JsonProvider<"./GeoLocationSample.json">

type Provider() = 
    inherit BaseLocationProvider()

    let name        = Constants.NAME + ".Service"
    let serverUrl   = "https://geoip.fedoraproject.org/city"

    override x.CityName
      with get() =
        match x.GetDataFromServer () with
            | Some x -> (x : GeoLocation.Root).City
            | None   -> GeoLocation.GetSample().City

    override x.Latitude
      with get() =
        match x.GetDataFromServer () with
            | Some x -> (x : GeoLocation.Root).Latitude
            | None   -> GeoLocation.GetSample().Latitude

    override x.Longitude
      with get() =
        match x.GetDataFromServer () with
            | Some x -> (x : GeoLocation.Root).Longitude
            | None   -> GeoLocation.GetSample().Longitude

    member x.GetDataFromServer () =
             try Some (GeoLocation.Load(serverUrl))
             with :? System.Net.WebException -> None
