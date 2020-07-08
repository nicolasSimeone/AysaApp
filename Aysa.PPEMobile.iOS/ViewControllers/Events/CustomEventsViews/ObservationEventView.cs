using Foundation;
using System;
using UIKit;
using ObjCRuntime;
using Aysa.PPEMobile.Model;

namespace Aysa.PPEMobile.iOS
{
    public partial class ObservationEventView : UIView
    {
        public ObservationEventView (IntPtr handle) : base (handle)
        {
        }

		public static ObservationEventView Create()
		{

			var arr = NSBundle.MainBundle.LoadNib("ObservationEventView", null, null);
			var v = Runtime.GetNSObject<ObservationEventView>(arr.ValueAt(0));

			return v;
		}

		public override void AwakeFromNib()
		{

		}

        public void LoadObservationInView(Observation observation)
        {
            NameLabel.Text = observation.Usuario.NombreApellido;
            DescriptionTextView.Text = observation.Observacion;
            DateLabel.Text =observation.Fecha.ToString("dd/MM/yyyy");
        }
    }
}