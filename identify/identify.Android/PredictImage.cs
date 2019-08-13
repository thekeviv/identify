using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using identify.Droid;
using identify.Services;
using Org.Tensorflow.Contrib.Android;
using Plugin.Media.Abstractions;

[assembly: Xamarin.Forms.Dependency(typeof(PredictImage))]
namespace identify.Droid
{   
    class PredictImage : IPredict
    {
        public string getPrediction(Plugin.Media.Abstractions.MediaFile image)
        {
            Task<Bitmap> temp = getBitmap(image);
            Bitmap bitmap = temp.Result;
            var assets = Android.App.Application.Context.Assets;
            TensorFlowInferenceInterface inferenceInterface = new TensorFlowInferenceInterface(assets, "frozen_inference_graph.pb");
            var sr = new StreamReader(assets.Open("labels.txt"));
            var labels = sr.ReadToEnd()
                           .Split('\n')
                           .Select(s => s.Trim())
                           .Where(s => !string.IsNullOrEmpty(s))
                           .ToList();
            var outputNames = new[] { "detection_classes" };
            var floatValues = GetBitmapPixels(bitmap);
            var outputs = new float[labels.Count];

            inferenceInterface.Feed("ToFloat", floatValues, 1, 227, 227, 3);
            inferenceInterface.Run(outputNames);
            inferenceInterface.Fetch("detection_classes", outputs);

            var results = new List<Tuple<float, string>>();
            for (var i = 0; i < outputs.Length; ++i)
                results.Add(Tuple.Create(outputs[i], labels[i]));

            return results.OrderByDescending(t => t.Item1).First().Item2;

        }

        async Task<Bitmap> getBitmap(MediaFile image)
        {
            var bitmap = await Android.Graphics.BitmapFactory.DecodeStreamAsync(image.GetStreamWithImageRotatedForExternalStorage());
            return bitmap as Bitmap;
        }
        static float[] GetBitmapPixels(Bitmap bitmap)
        {
            var floatValues = new float[227 * 227 * 3];

            using (var scaledBitmap = Bitmap.CreateScaledBitmap(bitmap, 227, 227, false))
            {
                using (var resizedBitmap = scaledBitmap.Copy(Bitmap.Config.Argb8888, false))
                {
                    var intValues = new int[227 * 227];
                    resizedBitmap.GetPixels(intValues, 0, resizedBitmap.Width, 0, 0, resizedBitmap.Width, resizedBitmap.Height);

                    for (int i = 0; i < intValues.Length; ++i)
                    {
                        var val = intValues[i];

                        floatValues[i * 3 + 0] = ((val & 0xFF) - 104);
                        floatValues[i * 3 + 1] = (((val >> 8) & 0xFF) - 117);
                        floatValues[i * 3 + 2] = (((val >> 16) & 0xFF) - 123);
                    }

                    resizedBitmap.Recycle();
                }

                scaledBitmap.Recycle();
            }

            return floatValues;
        }

        
    }
}