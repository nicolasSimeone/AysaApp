﻿// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;
using UIKit;

namespace Aysa.PPEMobile.iOS.ViewControllers.Features
{
    [Register ("FilterFeaturesViewController")]
    partial class FilterFeaturesViewController
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIBarButtonItem cancelButton { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIBarButtonItem ClearFilterButton { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITextField FromDateTextField { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITextField NumberEventTextField { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton SaveChangesButton { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITextField SectionFeatureTextField { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITextField TitleTextField { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITextField ToDateTextField { get; set; }

        [Action ("CancelButton_Activated:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void CancelButton_Activated (UIKit.UIBarButtonItem sender);

        [Action ("ClearFilterButton_Activated:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void ClearFilterButton_Activated (UIKit.UIBarButtonItem sender);

        [Action ("SaveChangesButton_TouchUpInside:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void SaveChangesButton_TouchUpInside (UIKit.UIButton sender);

        void ReleaseDesignerOutlets ()
        {
            if (cancelButton != null) {
                cancelButton.Dispose ();
                cancelButton = null;
            }

            if (ClearFilterButton != null) {
                ClearFilterButton.Dispose ();
                ClearFilterButton = null;
            }

            if (FromDateTextField != null) {
                FromDateTextField.Dispose ();
                FromDateTextField = null;
            }

            if (NumberEventTextField != null) {
                NumberEventTextField.Dispose ();
                NumberEventTextField = null;
            }

            if (SaveChangesButton != null) {
                SaveChangesButton.Dispose ();
                SaveChangesButton = null;
            }

            if (SectionFeatureTextField != null) {
                SectionFeatureTextField.Dispose ();
                SectionFeatureTextField = null;
            }

            if (TitleTextField != null) {
                TitleTextField.Dispose ();
                TitleTextField = null;
            }

            if (ToDateTextField != null) {
                ToDateTextField.Dispose ();
                ToDateTextField = null;
            }
        }
    }
}