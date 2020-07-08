// WARNING
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
    [Register ("AddFeatureViewController")]
    partial class AddFeatureViewController
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIView AttachmentContentView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITextField AutorTextField { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton CreateButton { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITextField DateTextField { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.NSLayoutConstraint HeightAttachmentContentConstraint { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITextField SectionEventTextField { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITextView TitleTextView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.NSLayoutConstraint TopAttachmentContentConstraint { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.NSLayoutConstraint TopCreateButtonConstraint { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton UploadFileButton { get; set; }

        [Action ("CreateEvent:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void CreateEvent (UIKit.UIButton sender);

        void ReleaseDesignerOutlets ()
        {
            if (AttachmentContentView != null) {
                AttachmentContentView.Dispose ();
                AttachmentContentView = null;
            }

            if (AutorTextField != null) {
                AutorTextField.Dispose ();
                AutorTextField = null;
            }

            if (CreateButton != null) {
                CreateButton.Dispose ();
                CreateButton = null;
            }

            if (DateTextField != null) {
                DateTextField.Dispose ();
                DateTextField = null;
            }

            if (HeightAttachmentContentConstraint != null) {
                HeightAttachmentContentConstraint.Dispose ();
                HeightAttachmentContentConstraint = null;
            }

            if (SectionEventTextField != null) {
                SectionEventTextField.Dispose ();
                SectionEventTextField = null;
            }

            if (TitleTextView != null) {
                TitleTextView.Dispose ();
                TitleTextView = null;
            }

            if (TopAttachmentContentConstraint != null) {
                TopAttachmentContentConstraint.Dispose ();
                TopAttachmentContentConstraint = null;
            }

            if (TopCreateButtonConstraint != null) {
                TopCreateButtonConstraint.Dispose ();
                TopCreateButtonConstraint = null;
            }

            if (UploadFileButton != null) {
                UploadFileButton.Dispose ();
                UploadFileButton = null;
            }
        }
    }
}