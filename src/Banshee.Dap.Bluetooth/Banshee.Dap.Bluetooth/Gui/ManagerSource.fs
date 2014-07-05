﻿//
// ManagerSource.fs
//
// Author:
//   Nicholas J. Little <arealityfarbetween@googlemail.com>
//
// Copyright (c) 2014 Nicholas J. Little
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

namespace Banshee.Dap.Bluetooth.Gui

open System
open System.Collections.Generic
open Banshee.Sources
open Banshee.Sources.Gui
open Banshee.ServiceStack
open Banshee.Dap.Bluetooth
open Banshee.Dap.Bluetooth.Client
open Banshee.Dap.Bluetooth.DapGlueApi
open Banshee.Dap.Bluetooth.InversionApi
open Banshee.Dap.Bluetooth.Wrappers
open Banshee.Gui
open DBus
open Gtk
open Hyena

module Constants =
    let SORT = 410 // Puts us in "Devices"
    let NAME = "Banshee.Dap.Bluetooth.Gui"

type ManagerContents(s, dm: DeviceManager, cm: ClientManager) as this =
    inherit VBox(false, 10)
    let act = new AdaptersWidget(dm)
    let box = new VBox()
    let awm = Dictionary<IBansheeAdapter,_>()
    let dwm = Dictionary<IBansheeDevice,DeviceWidget>()
    let mwm = Dictionary<IBansheeMediaControl,MediaControlWidget>()
    do
       base.PackStart (act, false, false, 10u)
       base.PackStart (box, true, true, 10u)
       base.ShowAll ()

       act.PowerEvent.Add (fun o -> if dm.Powered <> act.Power then dm.Powered <- act.Power)
       act.DiscoverEvent.Add (fun o -> if dm.Discovering <> act.Discovery then dm.Discovering <- act.Discovery)

       dm.AdapterChanged.Add (fun o -> match o.Action with
                                       | Added -> this.Add o.Adapter
                                                  act.Refresh ()
                                       | Changed -> act.Power <- dm.Powered
                                                    act.Discovery <- dm.Discovering
                                                    act.Refresh ()
                                       | Removed -> this.Remove o.Adapter |> ignore
                                                    act.Refresh ())
       dm.DeviceChanged.Add (fun o -> match o.Action with
                                      | Added -> this.Add o.Device
                                      | Changed -> dwm.[o.Device].Refresh()
                                      | Removed -> this.Remove o.Device |> ignore)
       dm.MediaControlChanged.Add (fun o -> match o.Action with
                                            | Added -> this.Add o.MediaControl
                                            | Changed -> mwm.[o.MediaControl].Refresh()
                                            | Removed -> this.Remove o.MediaControl |> ignore)

       dm.Adapters |> Seq.iter (fun o -> this.Add o)
       dm.Devices |> Seq.iter (fun o -> this.Add o)
       dm.MediaControls |> Seq.iter (fun o -> this.Add o)
    member x.Add (o: obj) = match o with
                            | :? IBansheeAdapter as a -> awm.[a] <- null
                                                         act.Power <- act.Power || a.Powered
                                                         act.Discovery <- act.Discovery || a.Discovering
                            | :? IBansheeDevice as d -> dwm.[d] <- new DeviceWidget(d, cm)
                                                        box.PackStart (dwm.[d], false, false, 10u)
                            //| :? IBansheeMediaControl as m -> mwm.[m] <- new MediaControlWidget(m)
                            //                                  box.PackStart (mwm.[m], false, false, 10u)
                            | _ -> ()
    member x.Remove (o: obj) = match o with
                               | :? IBansheeAdapter as a -> awm.Remove a
                               | :? IBansheeDevice as d -> box.Remove dwm.[d]
                                                           dwm.Remove d
                               //| :? IBansheeMediaControl as m -> box.Remove mwm.[m]
                               //                                  mwm.Remove m
                               | _ -> false
    interface ISourceContents with
        member x.SetSource y = false
        member x.ResetSource () = ()
        member x.Widget with get () = x :> Widget
        member x.Source with get () = s

type ManagerSource(dm: DeviceManager, cm: ClientManager) as this =
    inherit Source (Functions.Singular "Bluetooth Manager",
                    "Bluetooth Manager",
                    Constants.SORT,
                    "extension-unique-id")
    //let act = new ManagerActions(dm) // FIXME: ToggleButton/Switch ToolItem
    do printfn "Initializing ManagerSource"
       base.Properties.SetStringList ("Icon.Name", "bluetooth")
       base.Properties.Set<ISourceContents> ("Nereid.SourceContents", new ManagerContents(this, dm, cm))
       base.Initialize ();

type ManagerService(name: string) =
    do Log.DebugFormat ("Instantiating {0}", name)
    let mutable dm : DeviceManager = Unchecked.defaultof<_>
    let mutable cm : ClientManager = Unchecked.defaultof<_>
    let mutable ms : ManagerSource = Unchecked.defaultof<_>
    member x.DeviceManager = dm
    member x.Dispose () = ServiceManager.SourceManager.RemoveSource (ms)
    member x.ServiceName = name
    member x.Initialize () = dm <- DeviceManager(Bus.System)
                             cm <- ClientManager(Bus.Session)
    member x.DelayedInitialize () = ms <- new ManagerSource(dm, cm)
                                    ServiceManager.SourceManager.AddSource (ms)
    interface IService with
        member x.ServiceName = x.ServiceName
    interface IDisposable with
        member x.Dispose () = x.Dispose ()
    interface IExtensionService with
        member x.Initialize () = x.Initialize ()
    interface IDelayedInitializeService with
        member x.DelayedInitialize () = x.DelayedInitialize ()
    new() = new ManagerService (Constants.NAME + ".ManagerService")
