//
// Core.cs
//
// Author:
//       Chris Howie <cdhowie@gmail.com>
//
// Copyright (c) 2009 Chris Howie
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
using OpenVP;
using OpenVP.Core;
using Tao.OpenGl;

using Buffer = OpenVP.Core.Buffer;

namespace Banshee.OpenVP.Visualizations
{
    public class Core : LinearPreset
    {
        public Core()
        {
            TextureHandle bufferTexture = new TextureHandle();

            SingleBuffer buffer = new SingleBuffer();
            buffer.Load = true;
            buffer.Texture = bufferTexture;
            Effects.Add(buffer);

            ClearScreen clear = new ClearScreen();
            clear.ClearColor = new Color(0, 0, 0, 0.085f);
            Effects.Add(clear);

            Effects.Add(new RandomMovement());

            Effects.Add(new CustomScope());
            Effects.Add(new OrangeScope());

            buffer = new SingleBuffer();
            buffer.Load = false;
            buffer.Texture = bufferTexture;
            Effects.Add(buffer);

            Effects.Add(new CircularMovement());
        }

        private class OrangeScope : Scope
        {
            public OrangeScope()
            {
                Color = new Color(1, 0.35f, 0, 1);
                Circular = true;
            }

            public override void RenderFrame(IController controller)
            {
                float bass = GetBass (controller.PlayerData);

                LineWidth = 0.5f + bass * 5.5f;

                Gl.glMatrixMode(Gl.GL_MODELVIEW);
                Gl.glPushMatrix();

                float scale = bass * 0.75f + 0.25f;
                Gl.glScalef(scale, scale, 1);

                base.RenderFrame(controller);

                Gl.glMatrixMode(Gl.GL_MODELVIEW);
                Gl.glPopMatrix();
            }

            private float GetBass (PlayerData data)
            {
                float[] freq = new float[data.NativeSpectrumLength];
                data.GetSpectrum(freq);

                float sum = 0;
                int range = freq.Length / 100;

                for (int i = 0; i < range; i++)
                    sum += freq[i];

                return sum / range;
            }
        }

        private class RandomMovement : MovementBase
        {
            private Random random = new Random();

            public RandomMovement()
            {
                XResolution = 64;
                YResolution = 64;
                Wrap = true;
            }

            protected override void PlotVertex(MovementData data)
            {
                data.Method = MovementMethod.Rectangular;
                data.X += (float) ((random.NextDouble() - 0.5) / 32.0);
                data.Y += (float) ((random.NextDouble() - 0.5) / 32.0);
                data.Alpha = 0.5f;
            }
        }

        private class CustomScope : ScopeBase
        {
            private float green;

            public override void RenderFrame(IController controller)
            {
                float[] pcm = new float[controller.PlayerData.NativePCMLength];
                controller.PlayerData.GetPCM(pcm);

                float avg = 0;
                for (int i = 0; i < pcm.Length; i++)
                    avg += Math.Abs(pcm[i]);

                avg /= pcm.Length;

                green = Math.Min(2 * avg, 0.75f);

                LineWidth = Math.Max(8 * avg, 0.01f);

                base.RenderFrame(controller);
            }

            protected override void PlotVertex(ScopeData data)
            {
                data.X = 2 * data.FractionalI - 1;
                data.Y = data.Value;

                data.Red = 0;
                data.Green = green;
                data.Blue = 1;
            }
        }

        private class CircularMovement : MovementBase
        {
            private float rr = 0;

            public CircularMovement()
            {
                XResolution = 63;
                YResolution = 63;
                Wrap = true;
            }

            protected override void OnRenderFrame()
            {
                rr += 0.05f;
            }

            protected override void PlotVertex (MovementData data)
            {
                data.Method = MovementMethod.Polar;
                data.Rotation = (float) Math.Cos(data.Rotation * 4 - rr / 4) + rr;
                data.Distance *= 0.75f;
            }
        }

        private class SingleBuffer : Effect
        {
            public bool Load { get; set; }

            public TextureHandle Texture { get; set; }

            public override void NextFrame(IController controller)
            {
            }

            public override void RenderFrame(IController controller)
            {
                Gl.glPushAttrib(Gl.GL_ENABLE_BIT);
                Gl.glEnable(Gl.GL_TEXTURE_2D);
                Gl.glDisable(Gl.GL_DEPTH_TEST);
                Gl.glTexEnvf(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_DECAL);

                Texture.SetTextureSize(controller.Width,
                                       controller.Height);

                Gl.glBindTexture(Gl.GL_TEXTURE_2D, Texture.TextureId);

                Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S,
                                   Gl.GL_CLAMP);

                Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T,
                                   Gl.GL_CLAMP);

                if (Load) {
                        Gl.glColor4f(1, 1, 1, 1);
                        Gl.glBegin(Gl.GL_QUADS);

                        Gl.glTexCoord2f(0, 0);
                        Gl.glVertex2f(-1, -1);

                        Gl.glTexCoord2f(0, 1);
                        Gl.glVertex2f(-1,  1);

                        Gl.glTexCoord2f(1, 1);
                        Gl.glVertex2f( 1,  1);

                        Gl.glTexCoord2f(1, 0);
                        Gl.glVertex2f( 1, -1);

                        Gl.glEnd();
                } else {
                        Gl.glCopyTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGB, 0, 0,
                                            controller.Width,
                                            controller.Height, 0);
                }

                Gl.glPopAttrib();
            }

            public override void Dispose()
            {
                if (Texture != null) {
                    Texture.Dispose();
                    Texture = null;
                }

                base.Dispose();
            }
        }
    }
}
