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
using Aysa.PPEMobile.iOS.ViewControllers.Features.BuilderPickerForTextField;
using ObjCRuntime;
using System.Runtime.InteropServices;
using AssetsLibrary;
using System.IO;
using MobileCoreServices;

namespace Aysa.PPEMobile.iOS.ViewControllers.Features
{
    /// <summary>
    /// / Deffine Interface to notify that an event was updated
    /// </summary>
    public interface AddFeatureViewControllerDelegate
    {
        void FeatureWasUpdated(Feature eventObj);
    }

    public partial class AddFeatureViewController : UIViewController, PickerTextFieldDataSourceDelegate, IUINavigationControllerDelegate, AttachmentFileViewDelegate
    {
        // Private IBOutlets
        private UIDatePicker PickerFromDate;


        // Private Variables
        List<Section> ActiveSectionsList;
        Section SectionSelected;

        // Flag to avoid load Attachment events many times
        bool FilesAttachmentsAreLoaded = false;


        UIImagePickerController ImagePicker;
        List<AttachmentFile> AttachmentFilesInMemory = new List<AttachmentFile>();
        List<AttachmentFile> UploadedAttachmentFiles = new List<AttachmentFile>();
        int CountAttachmentsUploaded = 0;
        private static readonly int HeightAttachmentView = 30;

        // Public variables
        public Feature EditFeature;

        public Feature objectGlobal;

        /// <summary>
        ///  Define Delegate
        /// </summary>
        WeakReference<AddFeatureViewControllerDelegate> _delegate;

        public AddFeatureViewControllerDelegate Delegate
        {
            get
            {
                AddFeatureViewControllerDelegate workerDelegate;
                return _delegate.TryGetTarget(out workerDelegate) ? workerDelegate : null;
            }
            set
            {
                _delegate = new WeakReference<AddFeatureViewControllerDelegate>(value);
            }
        }

        #region UIViewController Lifecycle

        public AddFeatureViewController(IntPtr handle) : base(handle)
        {

        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            // Perform any additional setup after loading the view, typically from a nib.

            SetUpInputIBOutlets();
            
            LoadActiveSectionsInView();

            if (EditFeature != null)
            {
                // The user is editing an event
                PrepareViewToEditEvent();
            }
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            // Load files after calling ViewDidLayoutSubviews
            if (!FilesAttachmentsAreLoaded)
            {

                // Load file attachments after that the view was loaded
                if (EditFeature != null)
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
            User usuario = GetUserLogged();
            AutorTextField.Text = usuario.NombreApellido;
            AutorTextField.UserInteractionEnabled = false;

            LoadPickerViewInDateTextFields();

            // For some reason the event TouchUpInside don't response if it's assign from .Storyboard
            CreateButton.TouchUpInside += CreateEventAction;
            UploadFileButton.TouchUpInside += SelectFileAction;
            


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

        }



        private void LoadPickerViewInSectionTextField()
        {

            // Check Active Sections have values
            if (ActiveSectionsList.Count == 0)
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

            CreateButton.SetTitle("Modificar", UIControlState.Normal);

            this.NavigationItem.Title = "Editar Novedad";

        }

        private void LoadEventWillBeEditedInView()
        {
            User usuario = GetUserLogged();
            TitleTextView.Text = EditFeature.Detail;
            DateTextField.Text = EditFeature.Date.ToString(AysaConstants.FormatDateToSendEvent);
            DateTextField.Enabled = false;
            SectionSelected = EditFeature.Sector;
            AutorTextField.Text = usuario.NombreApellido;
            SectionEventTextField.Text = EditFeature.Sector.Nombre;


            // Load AttachmentContentView with files of event
            AttachmentFilesInMemory = new List<AttachmentFile>();
            AttachmentFilesInMemory.AddRange(EditFeature.Archivos);

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
            if (textField.Text.Length == 0)
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

        private bool IsMaxCharactersTextView(UITextView textView)
        {
            if (textView.Text.Length > 500)
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
                return "El campo Detalle no puede estar  vacío";
            }

            if(IsMaxCharactersTextView(TitleTextView))
            {
                return "El campo Detalle no puede contener mas de 500 caracteres";
            }

            // Validate Fecha de Ocurrencia
            if (IsEmptyTextField(DateTextField))
            {
                // Throw error message
                return "El campo Fecha de Ocurrencia no puede estar  vacío";
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
            user.Id = UserSession.Instance.Id;
            user.NombreApellido = UserSession.Instance.nomApel;


            return user;
        }


        private Feature BuildEventWillCreateFromUI()
        {

            // Validate Input fields
            string validateErrorMessage = ValidateInputFields();
            if (validateErrorMessage.Length > 0)
            {
                // There are errors in the Input fields.. show Alert
                ShowErrorAlert(validateErrorMessage);
                return null;
            }


            // Create Event
            Feature eventObj = new Feature();
            eventObj.Id = EditFeature != null ? EditFeature.Id : null;
            eventObj.Detail = TitleTextView.Text;
            eventObj.Date = DateTime.ParseExact(DateTextField.Text, AysaConstants.FormatDateToSendEvent, null);
            eventObj.Sector = SectionSelected;
            eventObj.Usuario = GetUserLogged();

            if (EditFeature != null)
            {
                eventObj.Archivos = EditFeature.Archivos;
                // Concatenate new files
                eventObj.Archivos.AddRange(UploadedAttachmentFiles);
            }
            else
            {
                eventObj.Archivos = UploadedAttachmentFiles;
            }



            return eventObj;
        }

        private void SendEventToServer(Feature eventObj)
        {

            Task.Run(async () =>
            {

                try
                {
                    Feature featureCreated;

                    if (EditFeature == null)
                    {
                        // Create Event
                        featureCreated = await AysaClient.Instance.CreateFeature(eventObj);
                    }
                    else
                    {
                        // Update Event
                        featureCreated = await AysaClient.Instance.UpdateFeature(EditFeature.Id, eventObj);
                    }

                    InvokeOnMainThread(() =>
                    {
                        if (EditFeature != null)
                        {
                            // Send filter to EventsViewController to get events by filter
                            if (_delegate != null)
                                Delegate?.FeatureWasUpdated(featureCreated);
                        }

                        string successMsj = EditFeature == null ? "La novedad ha sido creado con éxito" : "La novedad ha sido modificado con éxito";
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
                        if(CountAttachmentsUploaded < AttachmentFilesInMemory.Count)
                        {
                            UploadFilesToServer();
                        }
                        else
                        {
                            InvokeOnMainThread(() =>
                            {
                                Feature eventObj = BuildEventWillCreateFromUI();

                                if (eventObj != null)
                                {
                                    SendEventToServer(eventObj);
                                }
                            });
                        }

                        return;
                    }

                    AttachmentFile fileUpload = await AysaClient.Instance.UploadFileFeature(fileInMemory.BytesArray, fileInMemory.FileName);

                    //AttachmentFile fileUpload = await AysaClient.Instance.UploadFile(attachmentFile.BytesArray, attachmentFile.FileName);

                    InvokeOnMainThread(() =>
                    {
                        // Save uploaded file in list, this list will be assigned to the event that it will be created
                        UploadedAttachmentFiles.Add(fileUpload);

                        CountAttachmentsUploaded++;

                        if (CountAttachmentsUploaded < AttachmentFilesInMemory.Count)
                        {

                            UploadFilesToServer();
                        }
                        else
                        {
                            Feature eventObj = BuildEventWillCreateFromUI();

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
            if (AttachmentFilesInMemory.Count > 0)
            {
                AttachmentContentView.Hidden = false;
                TopAttachmentContentConstraint.Constant = 10;
                View.LayoutIfNeeded(); ;
            }
            else
            {
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

            if (AttachmentFilesInMemory.Count > 0)
            {
                UploadFilesToServer();
            }
            else
            {
                Feature eventObj = BuildEventWillCreateFromUI();

                objectGlobal = eventObj;

                if (eventObj != null)
                {
                    SendEventToServer(eventObj);
                }
            }
            BTProgressHUD.Dismiss();

        }

        #endregion

        #region Implement PickerTextFieldDataSourceDelegate Metods

        public void ItemSelectedValue(int indexSelected, UITextField textField)
        {

            // TextField with Tag value 1 = TypeEventTextField
            switch (textField.Tag)
            {
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
            //  NSUrl mediaURL = e.Info[UIImagePickerController.MediaURL] as NSUrl;
            //  if (mediaURL != null)
            //  {
            //      Console.WriteLine(mediaURL.ToString());
            //  }
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

        public void AttachmentSelected(AttachmentFile documentFile) { }

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
            if (documentFile.Id != null)
            {
                // Call WS to remove file in server
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
                        string successMsj = "El evento ha sido eliminado";
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
            if(EditFeature != null)
            {
                if(EditFeature.Usuario.NombreApellido == AutorTextField.Text)
                {
                    IntSecuenceToUploadEventInServer();
                }
                else
                {
                    string errorMss = "No puedes modificar una novedad de la que no eres autor.";
                    var alert = UIAlertController.Create("Error", errorMss, UIAlertControllerStyle.Alert);
                    alert.AddAction(UIAlertAction.Create("Ok", UIAlertActionStyle.Default, null));
                    PresentViewController(alert, true, null);
                }
            }
            else
            {
                IntSecuenceToUploadEventInServer();
            }


        }



        #endregion
    }
}

