// 
// ClutterFlowTimeline.cs
//  
// Author:
//       Mathijs Dumon <mathijsken@hotmail.com>
// 
// Copyright (c) 2010 Mathijs Dumon
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
using Clutter;

namespace ClutterFlow
{
	
	public class NewFrameEventArgs : EventArgs 
	{
		public double Progress;
		public NewFrameEventArgs (double progress) : base ()
		{ 
			Progress = progress;
		}
	}
	
	public class TargetReachedEventArgs : EventArgs 
	{
		public uint Target;
		public TargetReachedEventArgs (uint target) : base ()
		{ 
			Target = target;
		}
	}
	
	public class ThrottledTimeline : IDisposable
	{
		
		#region fields
		public event EventHandler<NewFrameEventArgs> NewFrame;
		protected void InvokeNewFrameEvent () 
		{
			if (NewFrame!=null) NewFrame (this, new NewFrameEventArgs (progress));
		}
		
		public event EventHandler<TargetReachedEventArgs> TargetMarkerReached;
		protected void InvokeTargetReached() {
			if (TargetMarkerReached!=null) TargetMarkerReached(this, new TargetReachedEventArgs(Target));
		}
		
		protected uint funcId;
		protected bool stop_timeout = false;
		
		protected TimelineDirection direction = TimelineDirection.Forward;
		public TimelineDirection Direction {
			get { return direction; }
		}
		
		protected uint target = 0;
		public virtual uint Target {
			get { return target; }
			set {
				if (value >= indexCount) value = indexCount-1;
				if (value < 0) value = 0;
				if (value > AbsoluteProgress) {
					target = value;
					direction = TimelineDirection.Forward;
				} else if (value < AbsoluteProgress) {
					target = value;
					direction = TimelineDirection.Backward;
				} else {
					InvokeNewFrameEvent ();
					InvokeTargetReached ();
				}
				delta = (int) Math.Abs(AbsoluteProgress - Target);
			}
		}
		
		public double RelativeTarget {
			get { return target / (double) (indexCount-1); }
		}
		
		protected double progress = 0;
		public virtual double Progress {
			get { return progress; }
			set { 
				if (value > 1) value = 1;
				if (value < 0) value = 0;
				progress = value;
				delta = (int) Math.Abs(AbsoluteProgress - Target);
			}
		}
		
		public double AbsoluteProgress {
			get { return (double) (progress*(indexCount-1)); }
		}
		
		protected int delta = 0;
		public int Delta {
			get { return delta;	}
		}

		protected int timeout = -1;
		public virtual int Timeout {
			get { lock (func_lock) { return timeout; } }
			set {
				lock (func_lock) {
					lastTime = DateTime.Now;
					timeout = value; 
				}
			}
		}

		protected static double time_threshold = 1000; //threshold to assure visible animations
        protected static double target_fps = 30; //target fps
        protected static double target_tmd = 1000 / target_fps;  // target timestep in ms;
		
		protected double frequency = 0.004;	//indeces per millisecond
		public virtual double Frequency {
			get { return frequency; }
			set { 
				if (value < 0) value = 0;
				frequency = value;
			}
		}
		protected DateTime lastTime = DateTime.Now;		//last iteration timestamp
		
		protected uint indexCount = 0;
		public uint IndexCount {
			get { return indexCount; }
		}
		
		protected bool isPlaying = false;
		public bool IsPlaying {
			get { return isPlaying; }
			set { isPlaying = value; }
		}

		private bool run_frame_source = true;
		protected void StopFrameSource () {
			lock (func_lock) { run_frame_source = false; }
		}
		
		#endregion
		
		public ThrottledTimeline ()
		{
			Clutter.Threads.AddFrameSourceFull (250, (uint) target_fps, RepaintFunc);
			funcId = Clutter.Threads.AddRepaintFunc (RepaintFunc);
		}
        public virtual void Dispose ()
        {
            Clutter.Threads.RemoveRepaintFunc (funcId);
            StopFrameSource ();
        }
		
		public ThrottledTimeline (uint indexCount, double frequency) : this()
		{
			SetIndexCount(indexCount);
			Frequency = frequency;
		}

		private object func_lock = new object();
		protected virtual bool RepaintFunc ()
		{
			lock (func_lock) {
				DateTime now = DateTime.Now;
				double timeDelta = (now - lastTime).Milliseconds;
				if (timeout != -1) {
					if (timeout <= timeDelta) {
						timeout = -1;
						lastTime = now;
					}
					return true;
				}
                if (timeDelta > time_threshold) timeDelta = time_threshold;
                if (timeDelta >= target_tmd) { //if smaller we are at a higher fps than targetted
    				if (IsPlaying) {
    					if (direction==TimelineDirection.Forward) {
    						progress +=	timeDelta * Frequency / (double) (indexCount-1);
    						if (target<=AbsoluteProgress) {
    							isPlaying = false;
    							progress = RelativeTarget;
    							InvokeTargetReached();
    						}
    					} else {
    						progress -= timeDelta * Frequency / (double) (indexCount-1);
    						if (target>=AbsoluteProgress) {
    							isPlaying = false;
    							progress = RelativeTarget;
    							InvokeTargetReached();
    						}
    					}
    					delta = (int) Math.Abs(AbsoluteProgress - Target);
    				}
                    lastTime = now;
                    InvokeNewFrameEvent ();
                }
				return run_frame_source; //keep on calling this function
			}
		}
		
		public void Start ()
		{
			lastTime = DateTime.Now;
			IsPlaying = true;
		}
		
		public void Halt ()
		{
			IsPlaying = false;
		}
		
		public void AdvanceToTarget (uint target)
		{
			Target = target;
			if (!IsPlaying) IsPlaying = true;
		}
		
		public void SetIndexCount (uint newCount) 
		{
			SetIndexCount (newCount, true, true);
		}
		public virtual void SetIndexCount (uint newCount, bool scaleProgress, bool scaleTarget)
		{
			if (IndexCount > 0)
				Target = (uint) (RelativeTarget * newCount);
			else 
				Target = 0;
			indexCount = newCount;
		}
	}
	
	public class ClutterFlowTimeline : ThrottledTimeline
	{
		#region Fields
		protected int lastDelta = 0;

		public override double Frequency {
			get {
				double retval = (double) Math.Max((Delta - (Delta - lastDelta)*0.25 ),1) / (double) CoverManager.MaxAnimationSpan;
				lastDelta = Delta;
				return retval;
			}
		}
				
		protected CoverManager coverManager;
		public CoverManager CoverManager {
			get { return coverManager; }
			set {
				if (value!=coverManager) {
					if (coverManager!=null) {
						coverManager.CoversChanged -= HandleCoversChanged;
						coverManager.TargetIndexChanged -= HandleTargetIndexChanged;
					}
					coverManager = value;
					if (coverManager!=null) {
						coverManager.CoversChanged += HandleCoversChanged;
                        coverManager.TargetIndexChanged += HandleTargetIndexChanged;
                    }
				}
			}
		}


		#endregion
		
		public ClutterFlowTimeline (CoverManager coverManager) : base((uint) coverManager.TotalCovers, 1 / (double) CoverManager.MaxAnimationSpan)
		{
			this.CoverManager = coverManager;
		}
        public override void Dispose ()
        {
            CoverManager = null;
            base.Dispose ();
        }

		
		#region Event Handlers
		protected void HandleCoversChanged(object sender, EventArgs e)
		{
			SetIndexCount ((uint) coverManager.TotalCovers, false, false);
		}
		
		protected void HandleTargetIndexChanged(object sender, EventArgs e)
		{
			AdvanceToTarget((uint) coverManager.TargetIndex);
		}
		#endregion
		
	}
}
