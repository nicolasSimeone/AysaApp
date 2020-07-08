using System;

using Foundation;
using UIKit;

namespace Aysa.PPEMobile.iOS.ViewControllers
{
    public partial class CustomTabBarViewController : UITabBarController
    {
		public CustomTabBarViewController(IntPtr handle) : base(handle)
		{
		}

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();

			// Setup style of BarItems (textColor, font, size, etc).
			this.SetupStyleOfTabBarItems();

            // Add observer to manage session expire
            this.AddObserverToSessionExpired();
		}

		public override void DidReceiveMemoryWarning()
		{
			base.DidReceiveMemoryWarning();
			// Release any cached data, images, etc that aren't in use.     
		}

		#region Internal Methods

		private void SetupStyleOfTabBarItems()
		{
			// Change textColor and font of BarItems for normal and selected state

            UIColor selectedColor = UIColor.FromRGB(248, 155, 9);

			// Define attributes to BarItems for normal state.
			UITextAttributes attributesForNormalState = new UITextAttributes();
			attributesForNormalState.TextColor = UIColor.White;
			attributesForNormalState.Font = UIFont.SystemFontOfSize(12f);
			UITabBarItem.Appearance.SetTitleTextAttributes(attributesForNormalState, UIControlState.Normal);

			// Define attributes to BarItems for selected state.
			UITextAttributes attributesForSelectedState = new UITextAttributes();
            attributesForSelectedState.TextColor = selectedColor;
			attributesForNormalState.Font = UIFont.SystemFontOfSize(12f);
			UITabBarItem.Appearance.SetTitleTextAttributes(attributesForSelectedState, UIControlState.Selected);

            TabBar.SelectedImageTintColor = selectedColor;
            TabBar.UnselectedItemTintColor = UIColor.White;

		}

        private void AddObserverToSessionExpired()
        {
            NSNotificationCenter.DefaultCenter.AddObserver((NSString)"SessionExpired", SessionExpired);
        }

		public void SessionExpired(NSNotification notification)
		{
            // Go to LoginViewController
			Console.WriteLine("Session Expired");
            DismissViewController(true, null);
			
		}

		#endregion
	}
}

