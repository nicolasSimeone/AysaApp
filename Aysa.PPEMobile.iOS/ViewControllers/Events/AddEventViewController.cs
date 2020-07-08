using System;
using UIKit;
using CoreGraphics;
using Foundation;
using Aysa.PPEMobile.iOS.Utilities;
using Aysa.PPEMobile.Model;
using System.Threading.Tasks;
using Aysa.PPEMobile.Service;
using Aysa.PPEMobile.Service.HttpExceptions;
using BigTed;
using System.Collections.Generic;
using Aysa.PPEMobile.iOS.ViewControllers.Events.BuilderPickerForTextField;
using ObjCRuntime;
using System.Runtime.InteropServices;
using AssetsLibrary;
using System.IO;
using MobileCoreServices;
using System.Globalization;

namespace Aysa.PPEMobile.iOS.ViewControllers.Events
{

	/// <summary>
	/// / Deffine Interface to notify that an event was updated
	/// </summary>
	public interface AddEventViewControllerDelegate
	{
        void EventWasUpdated(Event eventObj);
	}

    public partial class AddEventViewController : UIViewController, PickerTextFieldDataSourceDelegate, IUINavigationControllerDelegate, AttachmentFileViewDelegate
    {
        // Private IBOutlets
        private UIDatePicker PickerFromDate;

        // Private Variables
        List<EventType> EventTypesList;
        EventType EventTypeSelected;
        List<Section> ActiveSectionsList;
        Section SectionSelected;
        bool privateFile = false;
        DateTime todayDate;

       
        // Flag to avoid load Attachment events many times
        bool FilesAttachmentsAreLoaded = false;


        UIImagePickerController ImagePicker;
        List<AttachmentFile> AttachmentFilesInMemory = new List<AttachmentFile>();
        List<AttachmentFile> UploadedAttachmentFiles = new List<AttachmentFile>();
        List<AttachmentFile> filesToDeleteInServer = new List<AttachmentFile>();

        int CountAttachmentsUploaded = 0;
        private static readonly int HeightAttachmentView = 30;

        // Public variables
        public Event EditEvent;

		/// <summary>
		///  Define Delegate
		/// </summary>
		WeakReference<AddEventViewControllerDelegate> _delegate;

		public AddEventViewControllerDelegate Delegate
		{
			get
			{
				AddEventViewControllerDelegate workerDelegate;
				return _delegate.TryGetTarget(out workerDelegate) ? workerDelegate : null;
			}
			set
			{
				_delegate = new WeakReference<AddEventViewControllerDelegate>(value);
			}
		}

        #region UIViewController Lifecycle

        public AddEventViewController(IntPtr handle) : base(handle)
        {

        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            // Perform any additional setup after loading the view, typically from a nib.

            SetUpInputIBOutlets();

            GetEventTypesFromServer();
            LoadActiveSectionsInView();

            if(EditEvent != null)
            {
                // The user is editing an event
                PrepareViewToEditEvent();
            }
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            // Load files after calling ViewDidLayoutSubviews
            if(!FilesAttachmentsAreLoaded){
                
				// Load file attachments after that the view was loaded
				if (EditEvent != null)
				{

					LoadAttachmentsOfEvent();
				}

                FilesAttachmentsAreLoaded = true;
            }

        }

	

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            // Release any cached data, images, etc that aren't in use.
        }

        #endregion

        #region Private Methods

        private void SetUpInputIBOutlets()
        {

            // Add tap gesture recognizer to dismiss the keyboard
            View.AddGestureRecognizer(new UITapGestureRecognizer(() => this.DismissKeyboard()));


            // Add left padding in NumberEventTextField
            UIView paddingDateView = new UIView(new CGRect(0, 0, 30, 0));
            DateTextField.LeftView = paddingDateView;
            DateTextField.LeftViewMode = UITextFieldViewMode.Always;

            LoadPickerViewInDateTextFields();

			// For some reason the event TouchUpInside don't response if it's assign from .Storyboard
			CreateButton.TouchUpInside += CreateEventAction;
            UploadFileButton.TouchUpInside += SelectFileAction;


				// It's necessary hidden Status Conteiner because it's don't allow edit this field when an event is being created
				//StatusConteinerView.Hidden = true;
                //TopTagConstraint.Constant = -StatusConteinerView.Frame.Height; 



            // Int ImagePickerViewController
            ImagePicker = new UIImagePickerController();
			ImagePicker.SourceType = UIImagePickerControllerSourceType.PhotoLibrary;
			//ImagePicker.MediaTypes = UIImagePickerController.AvailableMediaTypes(UIImagePickerControllerSourceType.PhotoLibrary);
			ImagePicker.FinishedPickingMedia += Handle_FinishedPickingMedia;
			ImagePicker.Canceled += Handle_Canceled;


        }

        private void LoadPickerViewInDateTextFields()
        {
            // Load datePicker for DateTextField
            PickerFromDate = new UIDatePicker();
            PickerFromDate.Mode = UIDatePickerMode.Date;
            PickerFromDate.Date = (NSDate)DateTime.Today;
            PickerFromDate.ValueChanged += TextFieldDateFromValueChanged;
            DateTextField.InputView = PickerFromDate;
            todayDate = (DateTime)PickerFromDate.Date;
            DateTextField.Text = todayDate.ToString(AysaConstants.FormatDateToSendEvent);

        }



        private void LoadPickerViewInEventTypeTextField()
		{
			// Check Event Types have values
			if (EventTypesList == null)
			{
				return;
			}

            // Build data from list of EventsType
            List<string> data = new List<string>();

            for (int i = 0; i < EventTypesList.Count; i++){
                data.Add(EventTypesList[i].Nombre);
            }

			// Load Picker for TypeTextField
			UIPickerView picker = new UIPickerView();
            PickerTextFieldDataSource modelPicker = new PickerTextFieldDataSource(data, TypeTextField);
            modelPicker.Delegate = this;
            picker.Model = modelPicker;

            TypeTextField.InputView = picker;

        }


        private void LoadPickerViewInSectionTextField()
		{

            // Check Active Sections have values
            if(ActiveSectionsList.Count == 0)
            {
                SectionEventTextField.Enabled = false;
                SectionEventTextField.BackgroundColor = UIColor.FromRGB(220, 220, 220);
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
			PickerTextFieldDataSource modelPicker = new PickerTextFieldDataSource(data, SectionEventTextField);
			modelPicker.Delegate = this;
			picker.Model = modelPicker;

            SectionEventTextField.InputView = picker;

		}

        private void PrepareViewToEditEvent() 
        {
			// Load IBOutlets with values of event that it will be edited
			LoadEventWillBeEditedInView();

            // In edit event it's not possible edit the observations so remove this section
            ConteinerObservationView.Hidden = true;
            TopCreateButtonConstraint.Constant = 40;
            View.LayoutIfNeeded();

            CreateButton.SetTitle("Modificar", UIControlState.Normal);

            this.NavigationItem.Title = "Editar Evento";

        }

        private void LoadEventWillBeEditedInView()
        {
            TitleTextView.Text = EditEvent.Titulo;
            DateTextField.Text = EditEvent.Fecha.ToString(AysaConstants.FormatDate);
            PlaceTextField.Text = EditEvent.Lugar;
            DetailTextView.Text = EditEvent.Detalle;
            TagTextField.Text = EditEvent.Tag;
            ReferenceTextField.Text = EditEvent.Referencia > 0 ? EditEvent.Referencia.ToString() : "";
			EventTypeSelected = EditEvent.Tipo;
            TypeTextField.Text = EditEvent.Tipo.Nombre;
            SectionSelected = EditEvent.Sector;
            SectionEventTextField.Text = EditEvent.Sector.Nombre;

            // Status 1 = Open
            // Status 2 = Closed
            switch (EditEvent.Estado)
			{
				case 1:
					StatusSegmentedControl.SelectedSegment = 0;
					break;
				case 2:
					StatusSegmentedControl.SelectedSegment = 1;
					break;
				default:
					break;
			}

            // Load AttachmentContentView with files of event
            AttachmentFilesInMemory = new List<AttachmentFile>();
            AttachmentFilesInMemory.AddRange(EditEvent.Archivos);
                
        }


        private void DismissKeyboard()
        {
            View.EndEditing(true);
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

        private bool IsEmptyTextField(UITextField textField)
        {
            if(textField.Text.Length == 0){
                
                textField.Layer.BorderColor = UIColor.Red.CGColor;
                textField.Layer.BorderWidth = 1.0f;

                return true;
            }else{
                textField.Layer.BorderColor = UIColor.Clear.CGColor;
				textField.Layer.BorderWidth = 1.0f;

                return false;
            }
        }

        private bool IsDateTextFieldValid(UITextField textField)
        {
            DateTime dateText;

            bool isValid = DateTime.TryParseExact(textField.Text, AysaConstants.FormatDate,CultureInfo.InvariantCulture, DateTimeStyles.None, out dateText);

            if (!isValid)
            {

                textField.Layer.BorderColor = UIColor.Red.CGColor;
                textField.Layer.BorderWidth = 1.0f;

                return true;
            }
            else
            {
                textField.Layer.BorderColor = UIColor.Clear.CGColor;
                textField.Layer.BorderWidth = 1.0f;

                return false;
            }
        }

        private bool IsEmptyTextView(UITextView textView)
		{
			if (textView.Text.Length == 0)
			{

				textView.Layer.BorderColor = UIColor.Red.CGColor;
				textView.Layer.BorderWidth = 1.0f;

                return true;
			}
			else
			{
				textView.Layer.BorderColor = UIColor.Clear.CGColor;
				textView.Layer.BorderWidth = 1.0f;

                return false;
			}
		}

        private bool IsObjectEmpty(Object objectValue, UITextField textView)
		{
			if (objectValue == null)
			{

				textView.Layer.BorderColor = UIColor.Red.CGColor;
				textView.Layer.BorderWidth = 1.0f;

				return true;
			}
			else
			{
				textView.Layer.BorderColor = UIColor.Clear.CGColor;
				textView.Layer.BorderWidth = 1.0f;

				return false;
			}
		}


        private string ValidateInputFields()
        {

			// Validate Titulo
			if (IsEmptyTextView(TitleTextView))
			{
				// Throw error message
                return "El campo Titulo no puede estar  vacío";
			}

			// Validate Fecha de Ocurrencia
			if (IsEmptyTextField(DateTextField))
			{
				// Throw error message
				return "El campo Fecha de Ocurrencia no puede estar  vacío";
			}

            if(IsDateTextFieldValid(DateTextField))
            {
                return "El campo Fecha de Ocurrencia debe contener una fecha valida";
            }


            // Validate Lugar
            if (IsEmptyTextField(PlaceTextField))
			{
                // Throw error message
                return "El campo Lugar no puede estar  vacío";
			}

			// Validate Lugar
            if (IsEmptyTextView(DetailTextView))
			{
				// Throw error message
				return "El campo Detalle no puede estar  vacío";
			}

			// Validate Event Type
            if (IsObjectEmpty(EventTypeSelected, TypeTextField))
			{
				// Throw error message
				return "Es necesario seleccionar un Tipo de Evento";
			}

			// Validate Section
            if (IsObjectEmpty(SectionSelected, SectionEventTextField))
			{
				// Throw error message
				return "Es necesario seleccionar un Sector";
			}


            return "";
        }


        private User GetUserLogged()
		{
            User user = new User();
            user.UserName = UserSession.Instance.UserName;


            return user;
		}

        private List<Observation> GetObservations(Event eventWillCreate) 
        {
            if(ObservationTextView.Text.Length > 0)
            {
                List<Observation> observations = new List<Observation>();

                Observation observationObj = new Observation();
                observationObj.Observacion = ObservationTextView.Text;
                observationObj.Usuario = eventWillCreate.Usuario;
                observationObj.Sector = eventWillCreate.Sector;
                observationObj.Fecha = eventWillCreate.Fecha;

                observations.Add(observationObj);


                return observations;
            }

            return null;
        }

        private Event BuildEventWillCreateFromUI()
        {
           
            // Validate Input fields
            string validateErrorMessage = ValidateInputFields();
            if(validateErrorMessage.Length > 0)
            {
                // There are errors in the Input fields.. show Alert
                ShowErrorAlert(validateErrorMessage);
                return null;
            }


            // Create Event
			Event eventObj = new Event();
            eventObj.Id = EditEvent != null ? EditEvent.Id : null;
            eventObj.Titulo = TitleTextView.Text;
            eventObj.Fecha = DateTime.ParseExact(DateTextField.Text, AysaConstants.FormatDate, null);
            eventObj.Lugar = PlaceTextField.Text;
            eventObj.Detalle = DetailTextView.Text;
            eventObj.Tag = TagTextField.Text;
            eventObj.Estado = 1;
            eventObj.Sector = SectionSelected;
            eventObj.SectorOrigen = SectionSelected;
            eventObj.Referencia = ReferenceTextField.Text.Length > 0 ? int.Parse(ReferenceTextField.Text) : 0;
            eventObj.Tipo = EventTypeSelected;
            eventObj.Usuario = GetUserLogged();
            eventObj.Observaciones = GetObservations(eventObj);

            if(EditEvent != null)
            {
                eventObj.Archivos = EditEvent.Archivos;

                if (filesToDeleteInServer.Count > 0)
                {
                    foreach (AttachmentFile file in filesToDeleteInServer)
                    {
                        eventObj.Archivos.Remove(file);
                    }
                }
                else
                {
                    // Concatenate new files
                    eventObj.Archivos.AddRange(UploadedAttachmentFiles);
                }
            }
            else
            {
                eventObj.Archivos = UploadedAttachmentFiles;
            }


            // Set Status in case the user is editing an event
            

				switch (StatusSegmentedControl.SelectedSegment)
				{
					case 0:
                        eventObj.Estado = (int)Event.Status.Open;
						break;
					case 1:
                        eventObj.Estado = (int)Event.Status.Close;
						break;
					default:
						break;
				}


            return eventObj;
        }

        private void SendEventToServer(Event eventObj)
        {
            
			Task.Run(async () =>
			{

				try
				{
                    Event eventCreated;

                    if(EditEvent == null)
                    {
                        // Create Event
                        eventCreated = await AysaClient.Instance.CreateEvent(eventObj);
                    }else{
                        // Update Event
                        eventCreated = await AysaClient.Instance.UpdateEvent(EditEvent.Id, eventObj);
                    }

					InvokeOnMainThread(() =>
					{
                        if(EditEvent!= null)
                        {
							// Send filter to EventsViewController to get events by filter
							if (_delegate != null)
								Delegate?.EventWasUpdated(eventCreated);
                        }

                        string successMsj = EditEvent == null ? "El evento ha sido creado con éxito" : "El evento ha sido modificado con éxito";
                        BTProgressHUD.ShowImage(UIImage.FromBundle("ok_icon"), successMsj, 2000);
						PerformSelector(new Selector("PopViewController"), null, 2.0f);
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

                        CountAttachmentsUploaded = 0;
					});
				}
			});
        }

		[Export("PopViewController")]
		void PopViewController()
        {
			// Remove progress
			BTProgressHUD.Dismiss();
            this.NavigationController.PopViewController(true);
		}

		
		public void GetEventTypesFromServer()
		{
			// Get events type from server

			// Display an Activity Indicator in the status bar
			UIApplication.SharedApplication.NetworkActivityIndicatorVisible = true;

			Task.Run(async () =>
			{

				try
				{
                    EventTypesList = await AysaClient.Instance.GetEventsType();

					InvokeOnMainThread(() =>
					{
                        // After get EventsType list, load elements in PickerView
                        LoadPickerViewInEventTypeTextField();
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

		public void LoadActiveSectionsInView()
		{

            ActiveSectionsList = UserSession.Instance.ActiveSections;
			// After get Sections Active list, load elements in PickerView
			LoadPickerViewInSectionTextField();
		}


		private void UploadFilesToServer()
		{
			// Upload files

			Task.Run(async () =>
			{

				try
				{
                    AttachmentFile fileInMemory = AttachmentFilesInMemory[CountAttachmentsUploaded];

                    // If Attachment file doesn't have file that means that it's already added
                    if (fileInMemory.BytesArray == null)
                    {
                        CountAttachmentsUploaded++;
                        UploadFilesToServer();
                        return;
                    }

                    AttachmentFile fileUpload = await AysaClient.Instance.UploadFile(fileInMemory.BytesArray, fileInMemory.FileName);

                    //AttachmentFile fileUpload = await AysaClient.Instance.UploadFile(attachmentFile.BytesArray, attachmentFile.FileName);

					InvokeOnMainThread(() =>
					{
                        fileUpload.Private = privateFile;
						// Save uploaded file in list, this list will be assigned to the event that it will be created
						UploadedAttachmentFiles.Add(fileUpload);

                        CountAttachmentsUploaded++;

                        if(CountAttachmentsUploaded < AttachmentFilesInMemory.Count)
                        {
							
                            UploadFilesToServer();
                        }
                        else
                        {
							Event eventObj = BuildEventWillCreateFromUI();

							if (eventObj != null)
							{
								SendEventToServer(eventObj);
							}
                        }

					});

				}
				catch (HttpUnauthorized)
				{
					InvokeOnMainThread(() =>
					{
						ShowSessionExpiredError();
						// Remove progress
						BTProgressHUD.Dismiss();
					});
					
				}
				catch (Exception ex)
				{
					InvokeOnMainThread(() =>
					{
						ShowErrorAlert(ex.Message);
						// Remove progress
						BTProgressHUD.Dismiss();

                        CountAttachmentsUploaded = 0;
					});
				}
			
			});

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
			for (int i = 0; i < AttachmentFilesInMemory.Count; i++)
			{
				// Create observation view
				AttachmentFileView attachmentView = AttachmentFileView.Create();

				// Get top position of attachment file
				int topPosition = i * HeightAttachmentView;
				attachmentView.Frame = new CGRect(0, topPosition, AttachmentContentView.Frame.Width, HeightAttachmentView);
				attachmentView.Delegate = this;

				// Load file object in View
                attachmentView.LoadAttachmentFileInView(AttachmentFilesInMemory[i], false);

				// Add attachment in Content View
				AttachmentContentView.AddSubview(attachmentView);
			}
		}

		private void AdjustSizeAttachmentContentView()
		{
            // Config AttachmentContent according to there are attachments or not
            if(AttachmentFilesInMemory.Count > 0)
            {
                AttachmentContentView.Hidden = false;
                TopAttachmentContentConstraint.Constant = 10;
                View.LayoutIfNeeded();;
            }else{
                AttachmentContentView.Hidden = true;
				TopAttachmentContentConstraint.Constant = 0;
				View.LayoutIfNeeded(); ;
            }

			// Set Height of AttachmentContentView according to count of files in Event
            HeightAttachmentContentConstraint.Constant = AttachmentFilesInMemory.Count * HeightAttachmentView;
			View.LayoutIfNeeded();
		}

		private void BuildFileAttachment(UIImagePickerMediaPickedEventArgs media)
		{
			string name = "";

			// Build attachment and show in view

			NSUrl referenceURL = media.Info[new NSString("UIImagePickerControllerReferenceURL")] as NSUrl;
			if (referenceURL != null)
			{
				ALAssetsLibrary assetsLibrary = new ALAssetsLibrary();
				assetsLibrary.AssetForUrl(referenceURL, delegate (ALAsset asset) {

					ALAssetRepresentation representation = asset.DefaultRepresentation;

					if (representation != null)
					{
						name = representation.Filename;
					}

					string nameFile = name.Length > 0 ? name : "Archivo Adjunto " + (AttachmentFilesInMemory.Count + 1).ToString();

					AttachmentFile attachmentFile = new AttachmentFile();
                    attachmentFile.FileName = nameFile;
                    attachmentFile.Private = privateFile;

                    // Get image and convert to BytesArray
                    UIImage originalImage = media.Info[UIImagePickerController.OriginalImage] as UIImage;

					if (originalImage != null)
					{

                        string extension = referenceURL.PathExtension;

                        using (NSData imageData = originalImage.AsJPEG(0.5f))
						{
							Byte[] fileByteArray = new Byte[imageData.Length];
							Marshal.Copy(imageData.Bytes, fileByteArray, 0, Convert.ToInt32(imageData.Length));
							attachmentFile.BytesArray = fileByteArray;
						}

					}

					
					AttachmentFilesInMemory.Add(attachmentFile);

					// Show file selected in view
					LoadAttachmentsOfEvent();

				}, delegate (NSError error) {
					return;
				});
			}

		}

        private void IntSecuenceToUploadEventInServer()
        {

			//Show a HUD with a progress spinner and the text
			BTProgressHUD.Show("Cargando...", -1, ProgressHUD.MaskType.Black);

            if(AttachmentFilesInMemory.Count > 0)
            {
                UploadFilesToServer();
            }
            else
            {
				Event eventObj = BuildEventWillCreateFromUI();

				if (eventObj != null)
				{
					SendEventToServer(eventObj);
				}
            }
            BTProgressHUD.Dismiss();

        }
		
		#endregion

		#region Implement PickerTextFieldDataSourceDelegate Metods

		public void ItemSelectedValue(int indexSelected, UITextField textField){

			// TextField with Tag value 1 = TypeEventTextField
            switch (textField.Tag)
			{
				case 1:
					EventTypeSelected = EventTypesList[indexSelected];
                    TypeTextField.Text = EventTypeSelected.Nombre;
					break;
				case 2:
                    SectionSelected = ActiveSectionsList[indexSelected];
                    SectionEventTextField.Text = string.Format("{0} - Nivel: {1}", SectionSelected.Nombre, SectionSelected.Nivel);
					break;
				default:
					break;
			}
        }

		#endregion

		#region Implement UIImagePickerViewController Delegate

		protected void Handle_FinishedPickingMedia(object sender, UIImagePickerMediaPickedEventArgs media)
		{
			// determine what was selected, video or image
			bool isImage = false;
            switch (media.Info[UIImagePickerController.MediaType].ToString())
			{
				case "public.image":
					Console.WriteLine("Image selected");
					isImage = true;
					break;
				case "public.video":
					Console.WriteLine("Video selected");
					break;
			}

			// if it was an image, get the other image info
			if (isImage)
			{

                BuildFileAttachment(media);
			}


			//else
			//{ // if it's a video
			//  // get video url
			//	NSUrl mediaURL = e.Info[UIImagePickerController.MediaURL] as NSUrl;
			//	if (mediaURL != null)
			//	{
			//		Console.WriteLine(mediaURL.ToString());
			//	}
			//}

            // dismiss the picker
            ImagePicker.DismissViewController(true, null);
		}

		void Handle_Canceled(object sender, EventArgs e)
		{
            ImagePicker.DismissViewController(true, null);
		}

		#endregion

		#region Implement Delegates of AttachmentFileViewDelegate 

		public void AttachmentSelected(AttachmentFile documentFile){}

        public void RemoveAttachmentSelected(AttachmentFile documentFile)
        {

			UIAlertController alert = UIAlertController.Create("Aviso", "¿Está seguro que desea quitar el Archivo?", UIAlertControllerStyle.Alert);

			alert.AddAction(UIAlertAction.Create("Cancelar", UIAlertActionStyle.Cancel, null));

			alert.AddAction(UIAlertAction.Create("Si", UIAlertActionStyle.Default, action => {
				// Delete file
				DeleteFile(documentFile);
			}));

			PresentViewController(alert, animated: true, completionHandler: null);
        }

        private void DeleteFile(AttachmentFile documentFile)
        {
            
			// Delete file from server if it's necessary 
            // If the file has id that means that it was upload to the server, so it's needed to remove
            if (documentFile.Id != null){
                // Call WS to remove file in server
                filesToDeleteInServer.Add(documentFile);
                RemoveFileFromServer(documentFile);
            }
            else
            {
                // It's not necessary remove file in the server, so only remove in view
                RemoveFileFromView(documentFile);
            }
        }

        private void RemoveFileFromView(AttachmentFile documentFile)
        {
			// Search file to remove
			int index = -1;

			for (int i = 0; i < AttachmentFilesInMemory.Count; i++)
			{
				AttachmentFile file = AttachmentFilesInMemory[i];
				if (file.FileName == documentFile.FileName)
				{
					index = i;
					// Break the loop
					i = AttachmentFilesInMemory.Count;
				}
			}

			if (index != -1)
			{
				AttachmentFilesInMemory.RemoveAt(index);

				// Reload files attachments
				LoadAttachmentsOfEvent();
			}
        }

		private void RemoveFileFromServer(AttachmentFile file)
		{

			//Show a HUD with a progress spinner and the text
			BTProgressHUD.Show("Cargando...", -1, ProgressHUD.MaskType.Black);

			Task.Run(async () =>
			{

				try
				{
                    AttachmentFile response = await AysaClient.Instance.DeleteFile(file.Id);

					InvokeOnMainThread(() =>
					{
						string successMsj = "El archivo ha sido eliminado";
                        BTProgressHUD.ShowImage(UIImage.FromBundle("ok_icon"), successMsj, 2000);

                        // Remove file in view
                        RemoveFileFromView(file);
					});

				}
				catch (HttpUnauthorized)
				{
					InvokeOnMainThread(() =>
					{
						ShowErrorAlert("No tiene permisos para ejecutar esta acción");
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
                    BTProgressHUD.Dismiss();
                }
			});
		}

		#endregion

		#region IBActions Methods

		public void TextFieldDateFromValueChanged(object sender, EventArgs e)
		{
            // Format NSDate
            DateTime fromDate = (DateTime)PickerFromDate.Date;
			DateTextField.Text = fromDate.ToString(AysaConstants.FormatDateToSendEvent);
        }


        public void SelectFileAction(object sender, EventArgs e)
		{
            // Show picker to select file
            this.NavigationController.PresentViewController(ImagePicker, true, null);
		}


        public void CreateEventAction(object sender, EventArgs e)
		{

            IntSecuenceToUploadEventInServer();

		}




        #endregion


        partial void checkButton_TouchUpInside(UIButton sender)
        {

            sender.Selected = !sender.Selected;
            string nameImage = checkButton.Selected? "checked" : "unchecked";

            checkButton.SetImage(UIImage.FromBundle(nameImage), UIControlState.Normal);

            if (checkButton.Selected)
            {
                privateFile = true;
            }
            else
            {
                privateFile = false;
            }
        }
    }


}

