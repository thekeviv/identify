using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace identify.Services
{
    public interface IPredict
    {
        string getPrediction(Plugin.Media.Abstractions.MediaFile image);
    }
}
