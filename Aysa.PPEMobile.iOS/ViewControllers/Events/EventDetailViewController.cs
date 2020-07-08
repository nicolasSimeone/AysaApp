using System;
using UIKit;
using Foundation;
using Aysa.PPEMobile.Model;
using CoreGraphics;
using Aysa.PPEMobile.iOS.Utilities;
using BigTed;
using System.Threading.Tasks;
using Aysa.PPEMobile.Service;
using Aysa.PPEMobile.Service.HttpExceptions;
using System.Collections.Generic;
using Aysa.PPEMobile.iOS.ViewControllers.Events.BuilderPickerForTextField;
using System.IO;
using QuickLook;
using Aysa.PPEMobile.iOS.ViewControllers.Preview;

namespace Aysa.PPEMobile.iOS.ViewControllers.Events
{
    public partial class EventDetailViewController : UIViewController, AddEventViewControllerDelegate, PickerTextFieldDataSourceDelegate, AttachmentFileViewDelegate
    {
        // Public variables
        public Event EventDetail;

		// Private variables
		// Define general format for Dates
		private static readonly int HeightObservationView = 140;
        List<Section> ActiveSectionsList;
        Section SectionSelectedForObservation;

        private static readonly int HeightAttachmentView = 30;


		public EventDetailViewController(IntPtr handle) : base(handle)
		{
		}


		public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            // Perform any additional setup after loading the view, typically from a nib.

			// Change status bar text color to white
			NavigationController.NavigationBar.BarStyle = UIBarStyle.Black;

			SetupStyleOfView();

			// Load IBOutlets with event properties
			LoadEventInView();

			// Load sections to select when user add an observation
			LoadActiveSectionsInView();

			SetUpViewAccordingUserPermissions();

			// Get files of events
			GetFilesOfEventFromServer();

        }

		public override void ViewWillAppear(Boolean animated)
		{
			base.ViewWillAppear(animated);

		}

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            // Release any cached data, images, etc that aren't in use.
        }

		#region Internal Methods

		private void SetupStyleOfView()
		{
			UIColor borderColorContentView = UIColor.FromRGB(107, 107, 110);
			float borderWidth = 1.5f;
			float radius = 15.0f;

			// Add border and round of Tags
            ViewTagConteiner.Layer.BorderWidth = borderWidth;
			ViewTagConteiner.Layer.CornerRadius = radius;
			ViewTagConteiner.Layer.BorderColor = borderColorContentView.CGColor;

			// For some reason the event TouchUpInside don't response if it's assign from .Storyboard
            CheckButton.TouchUpInside += CheckAction;
            CreateObservationButton.TouchUpInside += CreateObservationAction;

		}

        private void LoadEventInView()
        {
            TitleEventLabel.Text = EventDetail.Titulo;
            DateLabel.Text = EventDetail.Fecha.ToString(AysaConstants.FormatDateToSendEvent);
            PlaceLabel.Text = EventDetail.Lugar;
            TypeLabel.Text = EventDetail.Tipo.Nombre;

			// Status 1 = Open
			// Status 2 = Close
			StatusLabel.Text = EventDetail.Estado == 1 ? "Abierto" : "Cerrado";

            if(EventDetail.Tag.Length > 0)
            {
                TagLabel.Text = EventDetail.Tag;
                ViewTagConteiner.Hidden = false;
            }


            // Set deail event and setup format for DetailLabel
            LoadDetailLabel();

            if(EventDetail.Observaciones != null)
            {
				// Load list of Observations
				LoadObservationsOfEvent();
            }

            // Define confidential 
            string nameImage = EventDetail.Confidencial ? "checked" : "unchecked";

			CheckButton.SetImage(UIImage.FromBundle(nameImage), UIControlState.Normal);
           
        }

        private void HiddenSectionForAddObservation()
        {
            SectionConteinerView.Hidden = true;
            HeightAddObservationConstraint.Constant = HeightAddObservationConstraint.Constant - HeightSectionConteinerViewConstraint.Constant;
            HeightSectionConteinerViewConstraint.Constant = 0;
            TopSectionConteinerViewConstraint.Constant = 0;
            View.LayoutIfNeeded();
        }

        private void SetUpViewAccordingUserPermissions()
        {

            if (UserSession.Instance.CheckIfUserHasPermission(Permissions.ModificarEvento))
			{
                // Allow to user do everything

                // Check if user doesn't have section active, in this case hide section combo, and set section assigned of event
                if(!UserSession.Instance.CheckIfUserHasActiveSections())
                {
                    SectionSelectedForObservation = EventDetail.Sector;
                    HiddenSectionForAddObservation();
                }

				// Check if user can edit confidential field.
				// The user can edit confidential field if he has guard responsable in section 1 or 2
				if (!UserSession.Instance.CheckIfUserIsGuardResponsableOfMainSection())
				{
					// The user doesn't have guard responsable in section 1 or 2, he can't edit confidential field
					HiddenConfidentialField();
				}

                return;
            }else{
                if (UserSession.Instance.CheckIfUserHasPermission(Permissions.ModificarEventoAutorizado))
                {
                    if(UserSession.Instance.CheckIfUserHasActiveSections())
                    {
                        // User has active section so he can add observations

                        // Check if user can edit confidential field.
                        // The user can edit confidential field if he has guard responsable in section 1 or 2
                        if(!UserSession.Instance.CheckIfUserIsGuardResponsableOfMainSection())
                        {
                            // The user doesn't have guard responsable in section 1 or 2, he can't edit confidential field
							HiddenConfidentialField();
                        }

                        return;
                    }else{
                        // User doesn't have active sections so he can't add observations
                        // The user only can edit the events that they were created by himself
                        EditButton.Enabled = EventDetail.CanEdit;
                        HiddenAddObservationSection();
                        HiddenConfidentialField();
                        return;
                    }
                }
            }

			//// User doesn't have any permissions
			//// Disable edit button, add observations and confidential field
            EditButton.Enabled = false;
			HiddenAddObservationSection();
            HiddenConfidentialField();
        }

        private void HiddenAddObservationSection()
        {
			
			AddObservationView.Hidden = true;
			HeightAddObservationConstraint.Constant = 0;
			View.LayoutIfNeeded();
        }

        private void HiddenConfidentialField()
        {
			ConfidentialConteinerView.Hidden = true;
			HeightConfidentialConteinerConstraint.Constant = 0;
			TopConfidentialContainerConstraint.Constant = 0;
			View.LayoutIfNeeded();
        }

        private void LoadDetailLabel()
        {
			// Set "Detalle" of Detail label text bold and another part default
			string detailText = "Detalle: " + EventDetail.Detalle;
            var strText = new NSMutableAttributedString(detailText);
            strText.AddAttribute(UIStringAttributeKey.Font, UIFont.FromName("Helvetica Bold", 15), new NSRange(0, 8));
            strText.AddAttribute(UIStringAttributeKey.ForegroundColor, UIColor.FromRGB(77, 77, 81), new NSRange(0, 8));

            DetailLabel.AttributedText = strText;
        }

        private void LoadObservationsOfEvent()
        {
            // Clear conteiner of Observations views

            // Remove all SubViews
            foreach(UIView observationView in ObservationContentView.Subviews)
            {
                observationView.RemoveFromSuperview();
            }

            // Set height of ObservationContentView
			AdjustSizeObservationsContentView();

			// Add observations in ObservationContentView
            for (int i = 0; i < EventDetail.Observaciones.Count; i++)
            {
                // Create observation view
				ObservationEventView observationView = ObservationEventView.Create();

                // Get top position of observation
                int topPosition = i * HeightObservationView;
                observationView.Frame = new CGRect(0, topPosition, ObservationContentView.Frame.Width, HeightObservationView);

                // Load observation object in View
                observationView.LoadObservationInView(EventDetail.Observaciones[i]);

                // Add Observation in Content View
				ObservationContentView.AddSubview(observationView);
            }
		}

		private void AdjustSizeObservationsContentView()
		{
			// Set Height of Observation ContentView according to count of observations in Event
			ConstraintHeightObservationContentView.Constant = EventDetail.Observaciones.Count * HeightObservationView;
			View.LayoutIfNeeded();
		}

		private void LoadAttachmentsOfEvent()
		{

			// Remove all SubViews
			foreach (UIView attachmentView in AttachmentContentView.Subviews)
			{
				attachmentView.RemoveFromSuperview();
			}

			// Set height of AttachmentContentView
			AdjustSizeAttachmentContentView();

			// Add attachment file in AttachmentContentView
            for (int i = 0; i < EventDetail.Archivos.Count; i++)
			{
				// Create observation view
                AttachmentFileView attachmentView = AttachmentFileView.Create();

				// Get top position of attachment file
				int topPosition = i * HeightAttachmentView;
                attachmentView.Frame = new CGRect(0, topPosition, AttachmentContentView.Frame.Width, HeightAttachmentView);
                attachmentView.Delegate = this;

				// Load file object in View
                attachmentView.LoadAttachmentFileInView(EventDetail.Archivos[i], true);

				// Add attachment in Content View
                AttachmentContentView.AddSubview(attachmentView);
			}
		}

		private void AdjustSizeAttachmentContentView()
		{
			// Set Height of AttachmentContentView according to count of files in Event
            HeightAttachmentConstraint.Constant = EventDetail.Archivos.Count * HeightAttachmentView;
			View.LayoutIfNeeded();
		}

		private void ShowErrorAlert(string message)
		{
			var alert = UIAlertController.Create("Error", message, UIAlertControllerStyle.Alert);
			alert.AddAction(UIAlertAction.Create("Ok", UIAlertActionStyle.Default, null));
			PresentViewController(alert, true, null);
		}

		private void ShowSessionExpiredError()
		{
			UIAlertController alert = UIAlertController.Create("Aviso", "Su sesión ha expirado, por favor ingrese sus credenciales nuevamente", UIAlertControllerStyle.Alert);

			alert.AddAction(UIAlertAction.Create("Ok", UIAlertActionStyle.Default, action => {
				// Send notification to TabBarController to return to login
				NSNotificationCenter.DefaultCenter.PostNotificationName("SessionExpired", this);
			}));

			PresentViewController(alert, animated: true, completionHandler: null);
		}

		public void LoadActiveSectionsInView()
		{

			ActiveSectionsList = UserSession.Instance.ActiveSections;
			// After get Sections Active list, load elements in PickerView
			LoadPickerViewInSectionTextField();

            // Load by default the first section
            if(ActiveSectionsList.Count > 0)
            {
				SectionSelectedForObservation = ActiveSectionsList[0];
				SectionTextField.Text = string.Format("{0} - Nivel: {1}", SectionSelectedForObservation.Nombre, SectionSelectedForObservation.Nivel);
            }
			

		}

		private void LoadPickerViewInSectionTextField()
		{

			// Check Active Sections have values
			if (ActiveSectionsList == null)
			{
				return;
			}

			// Build data from list of EventsType
			List<string> data = new List<string>();

			for (int i = 0; i < ActiveSectionsList.Count; i++)
			{
				Section sectionObj = ActiveSectionsList[i];
				data.Add(string.Format("{0} - Nivel: {1}", sectionObj.Nombre, sectionObj.Nivel));
			}

			// Load Picker for TypeTextField
			UIPickerView picker = new UIPickerView();
            PickerTextFieldDataSource modelPicker = new PickerTextFieldDataSource(data, SectionTextField);
			modelPicker.Delegate = this;
			picker.Model = modelPicker;

			SectionTextField.InputView = picker;

		}

		private User GetUserLogged()
		{
			User user = new User();
			user.UserName = UserSession.Instance.UserName;


			return user;
		}

		private Event GetEventAssociated()
		{
			// Build event
			Event eventObservation = new Event();
			eventObservation.Id = EventDetail.Id;

			return eventObservation;
		}


        private Observation BuildObservation(){

            if(ObservationTextView.Text.Length == 0)
            {
                ShowErrorAlert("El detalle de la observación no puede ser vacio");
                return null;
            }

            Observation observationObj = new Observation();
            observationObj.Fecha = DateTime.Now;
            observationObj.Observacion = ObservationTextView.Text;
            observationObj.Evento = GetEventAssociated();
            observationObj.Usuario = GetUserLogged();
            observationObj.Sector = SectionSelectedForObservation;

            return observationObj;
        }

		private void SendObservationToServer(Observation observationObj)
		{
			//Show a HUD with a progress spinner and the text
			BTProgressHUD.Show("Cargando...", -1, ProgressHUD.MaskType.Black);

			Task.Run(async () =>
			{

				try
				{
					Observation observation = await AysaClient.Instance.CreateObservation(observationObj);

					InvokeOnMainThread(() =>
					{
						if (observation != null)
						{
							if (EventDetail.Observaciones != null)
							{
                                // Clear TextField
                                ObservationTextView.Text = "";

                                // Add observetion in fist position to show it in first place
                                EventDetail.Observaciones.Insert(0, observation);
								// Reload list of Observations
								LoadObservationsOfEvent();
							}
						}

                        BTProgressHUD.ShowImage(UIImage.FromBundle("ok_icon"), "La observación ha sido creada con éxito", 2000);

					});

				}
				catch (HttpUnauthorized)
				{
					InvokeOnMainThread(() =>
					{
						// Remove progress
						BTProgressHUD.Dismiss();

						ShowSessionExpiredError();
					});
				}
				catch (Exception ex)
				{
					InvokeOnMainThread(() =>
					{
						// Remove progress
						BTProgressHUD.Dismiss();

						ShowErrorAlert(ex.Message);
					});
				}
			});
		}

		private void ChangeEventStatusCenfidential()
		{
			//Show a HUD with a progress spinner and the text
			BTProgressHUD.Show("Cargando...", -1, ProgressHUD.MaskType.Black);

			Task.Run(async () =>
			{

				try
				{
                    await AysaClient.Instance.SetEventConfidential(EventDetail.Id);

					InvokeOnMainThread(() =>
					{

						// Set inverse image
						string nameImage = EventDetail.Confidencial ? "unchecked" : "checked";

						CheckButton.SetImage(UIImage.FromBundle(nameImage), UIControlState.Normal);

                        // Set inverse 
                        EventDetail.Confidencial = !EventDetail.Confidencial;

						// Remove progress
						BTProgressHUD.Dismiss();
					});

				}
				catch (HttpUnauthorized)
				{
					InvokeOnMainThread(() =>
					{
						// Remove progress
						BTProgressHUD.Dismiss();

						ShowSessionExpiredError();
					});
				}
				catch (Exception ex)
				{
					InvokeOnMainThread(() =>
					{
						// Remove progress
						BTProgressHUD.Dismiss();

						ShowErrorAlert(ex.Message);
					});
				}
			});
		}

        private void GetFilesOfEventFromServer()
		{
            
			// Display an Activity Indicator in the status bar
			UIApplication.SharedApplication.NetworkActivityIndicatorVisible = true;

			Task.Run(async () =>
			{

				try
				{
                    // Get Files
                    EventDetail.Archivos = await AysaClient.Instance.GetFilesOfEvent(EventDetail.Id);

					InvokeOnMainThread(() =>
					{
                        if (EventDetail.Archivos.Count > 0)
                        {
							// Load AttachmentContentView with files of event
							LoadAttachmentsOfEvent();
                        }
                        else
                        {
                            // Hidden conteiner
                            AttachmentContentView.Hidden = true;
                            TopAttachmentContentConstraint.Constant = 0;
							HeightAttachmentConstraint.Constant = 0;
							View.LayoutIfNeeded();
                        }

					});

				}
				catch (HttpUnauthorized)
				{
					InvokeOnMainThread(() =>
					{
						ShowSessionExpiredError();
					});
				}
				catch (Exception ex)
				{
					InvokeOnMainThread(() =>
					{
						ShowErrorAlert(ex.Message);
					});
				}
				finally
				{
					// Dismiss an Activity Indicator in the status bar
					UIApplication.SharedApplication.NetworkActivityIndicatorVisible = false;
				}
			});

		}

		private void DownloadFileToShowIt(AttachmentFile documentFile)
		{
			// Display an Activity Indicator in the status bar
			UIApplication.SharedApplication.NetworkActivityIndicatorVisible = true;

			Task.Run(async () =>
			{

				try
				{

					// Download file from server
					byte[] bytesArray = await AysaClient.Instance.GetFile(documentFile.Id);
					//var text = System.Text.Encoding.Default.GetString(bytesArray);
					//text = text.Replace("\"", "");
					//bytesArray = Convert.FromBase64String(text);


					InvokeOnMainThread(() =>
					{

						NSData data = NSData.FromArray(bytesArray);

						SaveFileInLocalFolder(data, documentFile);


					});

				}
				catch (HttpUnauthorized)
				{
					InvokeOnMainThread(() =>
					{
						ShowSessionExpiredError();
					});
				}
				catch (Exception ex)
				{
					InvokeOnMainThread(() =>
					{
						ShowErrorAlert(ex.Message);
					});
				}
				finally
				{
					// Dismiss an Activity Indicator in the status bar
					UIApplication.SharedApplication.NetworkActivityIndicatorVisible = false;
				}
			});
		}

		private void SaveFileInLocalFolder(NSData data, AttachmentFile documentFile)
		{
			var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var filename = Path.Combine(documents, documentFile.FileName);
			data.Save(filename, false);

			ShowFileSavedInPreviewController(documentFile);

		}

		private void ShowFileSavedInPreviewController(AttachmentFile fileAtt)
		{

			QLPreviewController quickLookController = new QLPreviewController();
            quickLookController.DataSource = new PreviewControllerDataSource(fileAtt.FileName);
			NavigationController.PushViewController(quickLookController, true);

		}

		#endregion


		#region Implement Delegates of AddEventViewControllerDelegate 

		// This delegate will be called when the event has been updated
		public void EventWasUpdated(Event eventObj)
        {
            // Load event updated in View
            EventDetail = eventObj;
            LoadEventInView();

			// Get files of events
			GetFilesOfEventFromServer();
        }

		#endregion

		#region Implement Delegates of AttachmentFileViewDelegate 

        public void AttachmentSelected(AttachmentFile documentFile)
        {
            DownloadFileToShowIt(documentFile);
        }

        public void RemoveAttachmentSelected(AttachmentFile documentFile){}

		#endregion

		#region IBActions

		public void CheckAction(object sender, EventArgs e)
		{
            
            string message = EventDetail.Confidencial ? "¿Está seguro que desea desmarcar el evento como confidencial?" : "¿Está seguro que desea marcar el evento como confidencial?";

            UIAlertController alert = UIAlertController.Create("Aviso", message, UIAlertControllerStyle.Alert);

			alert.AddAction(UIAlertAction.Create("Cancelar", UIAlertActionStyle.Cancel, null));

			alert.AddAction(UIAlertAction.Create("Si", UIAlertActionStyle.Default, action => {

                ChangeEventStatusCenfidential();
			}));

			PresentViewController(alert, animated: true, completionHandler: null);
		}

		public void CreateObservationAction(object sender, EventArgs e)
		{
            
            Observation observationObj = BuildObservation();

            if(observationObj != null)
            {
                SendObservationToServer(observationObj);
            }
		}

		#endregion

		#region Implement PickerTextFieldDataSourceDelegate Metods

		public void ItemSelectedValue(int indexSelected, UITextField textField)
		{

            SectionSelectedForObservation = ActiveSectionsList[indexSelected];
            SectionTextField.Text = string.Format("{0} - Nivel: {1}", SectionSelectedForObservation.Nombre, SectionSelectedForObservation.Nivel);
		}

		#endregion

		#region Navigation Methods

		public override void PrepareForSegue(UIStoryboardSegue segue, NSObject sender)
		{
			if (segue.Identifier == "EditEventSegue")
			{
                AddEventViewController addEventViewController = (AddEventViewController)segue.DestinationViewController;
                addEventViewController.EditEvent = EventDetail;
                addEventViewController.Delegate = this;	
            }

		}

		#endregion

	}
}

