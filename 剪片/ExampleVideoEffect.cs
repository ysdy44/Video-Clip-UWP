using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Text;
using System.Collections.Generic;
using System.Numerics;
using Windows.Foundation.Collections;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;
using Windows.UI.Text;

namespace 剪片
{
    public sealed class ExampleVideoEffect : IBasicVideoEffect
    {

        CanvasDevice canvasDevice;
        public void SetEncodingProperties(VideoEncodingProperties encodingProperties, IDirect3DDevice device)//设置编码属性
        {
            canvasDevice = CanvasDevice.CreateFromDirect3D11Device(device);
        }
        public void ProcessFrame(ProcessVideoFrameContext context)//过程帧
        {
            IDirect3DSurface inputSurface = context.InputFrame.Direct3DSurface;
            IDirect3DSurface outputSurface = context.OutputFrame.Direct3DSurface;

            using (CanvasBitmap inputBitmap = CanvasBitmap.CreateFromDirect3D11Surface(canvasDevice, inputSurface))
            using (CanvasRenderTarget renderTarget = CanvasRenderTarget.CreateFromDirect3D11Surface(canvasDevice, outputSurface))
            using (CanvasDrawingSession ds = renderTarget.CreateDrawingSession())
            using (CanvasImageBrush brush = new CanvasImageBrush(canvasDevice, inputBitmap))
            using (CanvasCommandList textCommandList = new CanvasCommandList(canvasDevice))
            {
                using (var clds = textCommandList.CreateDrawingSession())
                {
                    clds.DrawText(
                        "Win2D\nMediaClip",
                        (float)inputBitmap.Size.Width / 2,
                        (float)inputBitmap.Size.Height / 2,
                        brush,
                        new CanvasTextFormat()
                        {
                            FontSize = (float)inputBitmap.Size.Width / 5,
                            FontWeight = new FontWeight() { Weight = 999 },
                            HorizontalAlignment = CanvasHorizontalAlignment.Center,
                            VerticalAlignment = CanvasVerticalAlignment.Center
                        });
                }

                GaussianBlurEffect background = new GaussianBlurEffect()
                {
                    BlurAmount = 10,
                    BorderMode = EffectBorderMode.Hard,
                    Source = new BrightnessEffect()
                    {
                        BlackPoint = new Vector2(0.5f, 0.7f),
                        Source = new SaturationEffect()
                        {
                            Saturation = 0,
                            Source = inputBitmap
                        }
                    }
                };

                var shadow = new ShadowEffect()
                {
                    Source = textCommandList,
                    BlurAmount = 10
                };

                var composite = new CompositeEffect()
                {
                    Sources = { background, shadow, textCommandList }
                };

                ds.DrawImage(composite);
            }
        }
        public void Close(MediaEffectClosedReason reason) { }
        public void DiscardQueuedFrames() { }



        public bool IsReadOnly => false;
        public IReadOnlyList<VideoEncodingProperties> SupportedEncodingProperties => new List<VideoEncodingProperties>();
        public MediaMemoryTypes SupportedMemoryTypes => MediaMemoryTypes.Gpu;
        public bool TimeIndependent => true;



        public void SetProperties(IPropertySet configuration) { }



    }
}
