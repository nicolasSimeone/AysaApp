using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Support.V7.App;
using Android.Content.Res;
using Android.Graphics;
using Android.Widget;
using Android.Support.V7.Widget;
using Aysa.PPEMobile.Model;
using System.Threading.Tasks;
using Aysa.PPEMobile.Service;
using Aysa.PPEMobile.Service.HttpExceptions;
using Aysa.PPEMobile.Droid;
using Aysa.PPEMobile.Droid.Utilities;
using Android.Database;
using Android.Provider;
using Java.IO;
using Android.Support.V4.Content;
using Android;
using Android.Content.PM;
using Android.Support.V4.App;

namespace Aysa.PPEMobile.Droid.Activities
{
    [Activity(Label = "AddEventActivity")]
    public class AddEventActivity : AppCompatActivity
    {
        public static readonly int PickImageId = 1000;

        FrameLayout progressOverlay;

        // Private Variables
        List<EventType> EventTypesList;
        List<Section> ActiveSectionsList;

        EditText titleEditText;
        EditText observationsEditText;
        EditText dateEditText;
        EditText placeEditText;
        EditText detailEditText;
        EditText tagsEditText;
        EditText referenceEditText;
        EditText generalEditText;
        Spinner spinnerType;
        Spinner spinnerSectors;
        View statusContainer;
        LinearLayout filesContainer;
        private CheckBox privateCheckBox;
        //TextView privateTextView;

        Button btnSegmented1;
        Button btnSegmented2;
        int segmentedIndexSelected = 0;

        List<AttachmentFile> attachedFiles = new List<AttachmentFile>();
        List<AttachmentFile> uploadedAttachmentFiles = new List<AttachmentFile>();
        List<AttachmentFile> filesToDeleteInServer = new List<AttachmentFile>();
        int countAttachmentsUploaded = 0;
        int countAttachmentsDeleted = 0;

        private Event mEvent;

        private bool editMode = false;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            this.mEvent = EventDataHolder.getInstance().getData();

            SetContentView(Resource.Layout.AddEvent);
            progressOverlay = FindViewById<FrameLayout>(Resource.Id.progress_overlay);

            Button btnCreate = FindViewById<Button>(Resource.Id.btnEventCreate);
            btnCreate.Click += BtnCreate_Click;

            global::Android.Support.V7.Widget.Toolbar toolbar = (global::Android.Support.V7.Widget.Toolbar)FindViewById(Resource.Id.toolbar);

            if (mEvent != null)
            {
                //MODO EDITAR EVENTO
                toolbar.Title = "Editar Evento #" + mEvent.NroEvento.ToString();
                btnCreate.Text = "Guardar";
                editMode = true;
                FindViewById<LinearLayout>(Resource.Id.containerObservaciones).Visibility = ViewStates.Gone;
            }
            else
            {
                //MODO AGREGAR NUEVO EVENTO
                toolbar.SetTitle(Resource.String.addevent_title);
                attachedFiles = new List<AttachmentFile>();
            }

            // Add back button
            SetSupportActionBar(toolbar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);


            titleEditText = FindViewById<EditText>(Resource.Id.editTextEventTitle);
            observationsEditText = FindViewById<EditText>(Resource.Id.editTextEventObservaciones);
            dateEditText = FindViewById<EditText>(Resource.Id.editTextEventDate);
            placeEditText = FindViewById<EditText>(Resource.Id.editTextEventPlace);
            detailEditText = FindViewById<EditText>(Resource.Id.editTextEventDetail);
            tagsEditText = FindViewById<EditText>(Resource.Id.editTextEventTags);
            referenceEditText = FindViewById<EditText>(Resource.Id.editTextEventReference);
            generalEditText = FindViewById<EditText>(Resource.Id.editTextEventObservaciones);
            filesContainer = FindViewById<LinearLayout>(Resource.Id.addEventFilesContainer);
            privateCheckBox = FindViewById<CheckBox>(Resource.Id.confidentialCheckBox);
            //privateTextView = FindViewById<TextView>(Resource.Id.privateTextView);

            FindViewById<Button>(Resource.Id.btnUploadFiles).Click += UploadFile_Click;


                statusContainer = FindViewById<View>(Resource.Id.status_container);
                statusContainer.Visibility = ViewStates.Visible;

                SetUpSegmentedControl();

            if(mEvent != null)
            { 
                titleEditText.Text = mEvent.Titulo;
                dateEditText.Text = mEvent.Fecha.ToString(AysaConstants.FormatDate);
                placeEditText.Text = mEvent.Lugar;
                detailEditText.Text = mEvent.Detalle;
                tagsEditText.Text = mEvent.Tag;
                referenceEditText.Text = mEvent.Referencia.ToString() == "0" ? "" : mEvent.Referencia.ToString();

                if (mEvent.Archivos != null)
                {
                    foreach (AttachmentFile file in mEvent.Archivos)
                    {
                        addAttachedFile(file.FileName);
                    }
                }

                attachedFiles.AddRange(mEvent.Archivos);

            }
            // Date Field
            dateEditText.Click += DateEditText_Click;

            // Type field
            spinnerType = FindViewById<Spinner>(Resource.Id.spinnerType);
            // Sector field
            spinnerSectors = FindViewById<Spinner>(Resource.Id.spinnerSectors);


            // Load lists of values from server
            GetEventTypesFromServer();

            LoadActiveSectionsInView();
        }

        private void UploadFile_Click(object sender, EventArgs e)
        {
            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage) == (int)Permission.Granted)
            {
                Intent = new Intent();
                Intent.SetType("image/*");
                Intent.SetAction(Intent.ActionGetContent);
                StartActivityForResult(Intent.CreateChooser(Intent, "Select Picture"), PickImageId);
            }
            else
            {
                ActivityCompat.RequestPermissions(this, new String[] { Manifest.Permission.WriteExternalStorage }, 1);
            }
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if ((requestCode == PickImageId) && (resultCode == Result.Ok) && (data != null))
            {
                Android.Net.Uri uri = data.Data;
                string filename = GetFileName(uri);
                AttachmentFile file = new AttachmentFile(filename);


                Byte[] fileByteArray = ConvertUriToByteArray(uri, filename);
                file.BytesArray = fileByteArray;
                if(privateCheckBox.Checked)
                {
                    file.Private = true;
                }
                attachedFiles.Add(file);

                addAttachedFile(filename);
            }
        }

        private byte[] ConvertUriToByteArray(Android.Net.Uri data, string name)
        {


            ByteArrayOutputStream baos = new ByteArrayOutputStream();
            FileInputStream fis;
            try
            {
                fis = new FileInputStream(new File(GetPath(data)));

                byte[] buf = new byte[1024];
                int n;
                while (-1 != (n = fis.Read(buf)))
                    baos.Write(buf, 0, n);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Error in convert Byte Array" + e.ToString());

                return null;
            }

            byte[] bbytes = baos.ToByteArray();

            return bbytes;
        }

        private string GetFileName(Android.Net.Uri uri)
        {
            string path = GetPath(uri);
            string[] parts = path.Split('/');

            return parts[parts.Length - 1];
        }

        private string GetPath(Android.Net.Uri uri)
        {
            ICursor cursor = this.ContentResolver.Query(uri, null, null, null, null);
            cursor.MoveToFirst();
            string document_id = cursor.GetString(0);
            document_id = document_id.Split(':')[1];
            cursor.Close();

            cursor = ContentResolver.Query(
            Android.Provider.MediaStore.Images.Media.ExternalContentUri,
            null, MediaStore.Images.Media.InterfaceConsts.Id + " = ? ", new String[] { document_id }, null);
            cursor.MoveToFirst();
            string path = cursor.GetString(cursor.GetColumnIndex(MediaStore.Images.Media.InterfaceConsts.Data));
            cursor.Close();

            return path;
            //return document_id;
        }

        private void addAttachedFile(string filename)
        {
            string privatepublic;
            if(privateCheckBox.Checked)
            {
                privatepublic = "Privado";
            }
            else
            {
                privatepublic = "Publico";
            }
            LayoutInflater inflater = (LayoutInflater)GetSystemService(Context.LayoutInflaterService);
            ViewGroup view = (ViewGroup)inflater.Inflate(Resource.Layout.AddEventDocumentRow, filesContainer, false);
            view.FindViewById<TextView>(Resource.Id.txtDocumentName).Text = filename + "   " + privatepublic;
            view.FindViewById<TextView>(Resource.Id.txtDocumentName).PaintFlags = Android.Graphics.PaintFlags.UnderlineText;
            view.FindViewById<ImageView>(Resource.Id.btnRemoveEventDocument).Click += ClickRemoveFile;

            filesContainer.AddView(view);
        }

        private void ClickRemoveFile(object sender, EventArgs e)
        {
            Android.App.AlertDialog.Builder dialog = new Android.App.AlertDialog.Builder(this);
            Android.App.AlertDialog alert = dialog.Create();
            alert.SetTitle("Aviso");
            alert.SetMessage("¿Está seguro que desea quitar el Archivo?");
            alert.SetButton("Si", (c, ev) =>
            {
                ViewGroup fileRow = (ViewGroup)((View)sender).Parent;
                int filePosition = filesContainer.IndexOfChild(fileRow);

                DeleteFile(attachedFiles[filePosition], filePosition);

            });

            alert.SetButton2("Cancelar", (c, ev) => { });

            alert.Show();


        }

        private void DeleteFile(AttachmentFile documentFile, int filePosition)
        {

            // Delete file from server if it's necessary 
            // If the file has id that means that it was upload to the server, so it's needed to remove
            if (documentFile.Id != null)
            {
                filesToDeleteInServer.Add(documentFile);

                // Call WS to remove file in server
                //RemoveFileFromServer(documentFile, filePosition);
            }

            // Remove file in view
            attachedFiles.RemoveAt(filePosition);

            filesContainer.RemoveViewAt(filePosition);

        }


        private void RemoveFilesFromServer()
        {

            //Show Progress
            ShowProgressDialog(true);

            Task.Run(async () =>
            {

                try
                {

                    AttachmentFile fileDelete = filesToDeleteInServer[countAttachmentsDeleted];

                    AttachmentFile response = await AysaClient.Instance.DeleteFile(fileDelete.Id);

                    RunOnUiThread(() =>
                    {
                        countAttachmentsDeleted++;

                        if (filesToDeleteInServer.Count() > countAttachmentsDeleted)
                        {
                            RemoveFilesFromServer();
                        }
                        else
                        {

                            // Finish to delete files, continue with the secuence
                            if (attachedFiles.Count > 0)
                            {
                                UploadFilesToServer();
                            }
                            else
                            {
                                UploadEventToServer();

                            }
                        }

                    });

                }
                catch (HttpUnauthorized)
                {
                    RunOnUiThread(() =>
                    {
                        ShowErrorAlert("No tiene permisos para eliminar archivos");
                    });
                }
                catch (Exception ex)
                {
                    RunOnUiThread(() =>
                    {
                        ShowErrorAlert(ex.Message);
                    });
                }
                finally
                {
                    RunOnUiThread(() =>
                    {
                        // Remove progress
                        ShowProgressDialog(false);
                    });

                }
            });

        }

        //private void RemoveFileFromServer(AttachmentFile file)
        //{

        ////Show Progress
        //ShowProgressDialog(true);

        //Task.Run(async () =>
        //{

        //try
        //{
        //    string response = await AysaClient.Instance.DeleteFile(file.Id);

        //    RunOnUiThread(() =>
        //    {
        //        global::Android.Support.V7.App.AlertDialog.Builder builder = new global::Android.Support.V7.App.AlertDialog.Builder(this);
        //        builder.SetTitle("Aviso");
        //        builder.SetMessage("El archivo ha sido eliminado");
        //        builder.SetPositiveButton("OK", OkAction);

        //        Dialog dialog = builder.Create();
        //        dialog.Show();

        //    });

        //}
        //catch (HttpUnauthorized)
        //{
        //RunOnUiThread(() =>
        //{
        //ShowErrorAlert("No tiene permisos para ejecutar esta acción");
        //            });
        //        }
        //        catch (Exception ex)
        //        {
        //            RunOnUiThread(() =>
        //            {
        //                ShowErrorAlert(ex.Message);
        //            });
        //        }
        //        finally
        //        {
        //            RunOnUiThread(() =>
        //            {
        //                // Remove progress
        //                ShowProgressDialog(false);
        //            });

        //        }
        //    });
        //}

        private void DateEditText_Click(object sender, EventArgs e)
        {
            Fragments.DatePickerFragment frag = Fragments.DatePickerFragment.NewInstance(delegate (DateTime time)
            {
                dateEditText.Text = time.ToString("dd/MM/yyyy");
            });
            frag.Show(FragmentManager, Fragments.DatePickerFragment.TAG);
        }

        private void BtnCreate_Click(object sender, EventArgs e)
        {
            string validateErrorMessage = ValidateInputFields();
            if (validateErrorMessage.Length > 0)
            {
                // There are errors in the Input fields.. show Alert
                ShowErrorAlert(validateErrorMessage);
                ShowProgressDialog(false);
            }
            else
            {
                //Show progress
                ShowProgressDialog(true);

                if (filesToDeleteInServer.Count > 0)
                {
                    // Init secuence to remove files
                    RemoveFilesFromServer();
                }
                else
                {
                    if (attachedFiles.Count > 0)
                    {
                        UploadFilesToServer();
                    }
                    else
                    {
                        UploadEventToServer();
                    }
                }
            }
        }

        private void UploadEventToServer()
        {
            Event eventObj = BuildEventWillCreateFromUI();

            if (eventObj != null)
            {
                SendEventToServer(eventObj);
            }
        }

        private void UploadFilesToServer()
        {
            // Upload files

            Task.Run(async () =>
            {

                try
                {
                    AttachmentFile fileInMemory = attachedFiles[countAttachmentsUploaded];

                    // If Attachment file doesn't have file that means that it's already added
                    if (fileInMemory.BytesArray == null)
                    {
                        countAttachmentsUploaded++;
                        if (countAttachmentsUploaded < attachedFiles.Count())
                        {
                            UploadFilesToServer();
                        }
                        else
                        {
                            UploadEventToServer();
                        }

                        return;
                    }

                    AttachmentFile fileUpload = await AysaClient.Instance.UploadFile(fileInMemory.BytesArray, fileInMemory.FileName);
                    
                //AttachmentFile fileUpload = await AysaClient.Instance.UploadFile(attachmentFile.BytesArray, attachmentFile.FileName);

                    RunOnUiThread(() =>
                    {

                        if(privateCheckBox.Checked)
                        {
                            fileUpload.Private = true;
                        }
                        // Save uploaded file in list, this list will be assigned to the event that it will be created
                        uploadedAttachmentFiles.Add(fileUpload);

                        countAttachmentsUploaded++;

                        if (countAttachmentsUploaded < attachedFiles.Count)
                        {

                            UploadFilesToServer();
                        }
                        else
                        {
                            UploadEventToServer();
                        }

                    });
                    
                }
                catch (HttpUnauthorized)
                {
                    RunOnUiThread(() =>
                    {
                        // Remove progress
                        ShowProgressDialog(false);

                        ShowSessionExpiredError();
                    });

                }
                catch (Exception ex)
                {
                    RunOnUiThread(() =>
                    {
                        // Remove progress
                        ShowProgressDialog(false);

                        ShowErrorAlert(ex.Message);
                    });
                }

            });

        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            Finish();
            return base.OnOptionsItemSelected(item);
        }

        private void ShowProgressDialog(bool show)
        {
            progressOverlay.Visibility = show ? ViewStates.Visible : ViewStates.Gone;
        }

        private string ValidateInputFields()
        {
            // Validate Titulo
            if (IsEditTextEmpty(titleEditText))
            {
                return "El campo Titulo no puede estar  vacío";
            }

            // Validate Fecha de Ocurrencia
            if (IsEditTextEmpty(dateEditText))
            {
                return "El campo Fecha de Ocurrencia no puede estar  vacío";
            }

            // Validate Lugar
            if (IsEditTextEmpty(placeEditText))
            {
                return "El campo Lugar no puede estar  vacío";
            }

            // Validate Lugar
            if (IsEditTextEmpty(detailEditText))
            {
                return "El campo Detalle no puede estar  vacío";
            }

            // Validate Event Type
            if (IsSpinnerEmpty(spinnerType))
            {
                return "Es necesario seleccionar un Tipo de Evento";
            }

            // Validate Section
            if (IsSpinnerEmpty(spinnerSectors))
            {
                return "Es necesario seleccionar un Sector";
            }

            return "";
        }

        void SetUpSegmentedControl()
        {
            btnSegmented1 = FindViewById<Button>(Resource.Id.btn_segmented1);
            btnSegmented1.SetBackgroundColor(Color.ParseColor("#672E8A"));
            btnSegmented1.Click += delegate
            {
                BtnSegmented_Click(btnSegmented1);
            };

            btnSegmented2 = FindViewById<Button>(Resource.Id.btn_segmented2);
            btnSegmented2.Click += delegate
            {
                BtnSegmented_Click(btnSegmented2);
            };

            if (mEvent != null)
            {
                if (mEvent.Estado == 2)
                {
                    ResetStatusSegmentedControl();
                    btnSegmented2.SetBackgroundColor(Color.ParseColor("#672E8A"));
                    btnSegmented2.SetTextColor(Color.ParseColor("#ffffff"));
                }
            }

        }

        public void BtnSegmented_Click(Button segmented)
        {

            ResetStatusSegmentedControl();

            segmented.SetBackgroundColor(Color.ParseColor("#672E8A"));
            segmented.SetTextColor(Color.ParseColor("#ffffff"));

            switch (segmented.Id)
            {
                case Resource.Id.btn_segmented1:
                    segmentedIndexSelected = 1;
                    break;
                case Resource.Id.btn_segmented2:
                    segmentedIndexSelected = 2;
                    break;
            }
        }

        void ResetStatusSegmentedControl()
        {
            btnSegmented1.SetBackgroundColor(Color.ParseColor("#ffffff"));
            btnSegmented1.SetTextColor(Color.ParseColor("#6B6B6E"));
            btnSegmented2.SetBackgroundColor(Color.ParseColor("#ffffff"));
            btnSegmented2.SetTextColor(Color.ParseColor("#6B6B6E"));
        }

        private bool IsSpinnerEmpty(Spinner spinner)
        {
            return spinner.SelectedItem == null;
        }

        private bool IsEditTextEmpty(EditText editText)
        {
            return editText.Text == null || editText.Text.Trim().Count() == 0;
        }

        private bool IsEmptyTextView(AppCompatEditText editText)
        {
            if (editText.Text.Length == 0)
            {
                ColorStateList colorStateList = ColorStateList.ValueOf(Color.Red);
                editText.SupportBackgroundTintList = colorStateList;
                return true;
            }
            else
            {
                ColorStateList colorStateList = ColorStateList.ValueOf(Color.Gray);
                editText.SupportBackgroundTintList = colorStateList;

                return false;
            }
        }

        private User GetUserLogged()
        {
            User user = new User();
            user.UserName = UserSession.Instance.UserName;

            return user;
        }

        private List<Observation> GetObservations(Event eventWillCreate)
        {
            if (generalEditText.Text.Length > 0)
            {
                List<Observation> observations = new List<Observation>();

                Observation observationObj = new Observation();
                observationObj.Observacion = generalEditText.Text;
                observationObj.Usuario = eventWillCreate.Usuario;
                observationObj.Sector = eventWillCreate.Sector;
                observationObj.Fecha = eventWillCreate.Fecha;

                observations.Add(observationObj);


                return observations;
            }

            return null;
        }

        public void GetEventTypesFromServer()
        {
            // Get events type from server

            Task.Run(async () =>
            {

                try
                {
                    EventTypesList = await AysaClient.Instance.GetEventsType();

                    RunOnUiThread(() =>
                    {
                        // Fill spinner items
                        ArrayAdapter adapter = new ArrayAdapter(this, global::Android.Resource.Layout.SimpleSpinnerItem, EventTypesList.ToArray());
                        spinnerType.Adapter = adapter;

                        if (EventTypesList != null && mEvent != null)
                        {
                            for (int i = 0; i < EventTypesList.Count(); i++)
                            {
                                EventType type = EventTypesList[i];
                                if (type.Id == mEvent.Tipo.Id)
                                {
                                    spinnerType.SetSelection(i);
                                    break;
                                }
                            }
                        }
                    });

                }
                catch (HttpUnauthorized)
                {
                    RunOnUiThread(() =>
                    {
                        ShowErrorAlert("Sesión expirada.");
                    });
                }
                catch (Exception ex)
                {
                    RunOnUiThread(() =>
                    {
                        ShowErrorAlert(ex.Message);
                    });
                }
                finally
                {
                }
            });

        }

        public void LoadActiveSectionsInView()
        {
            ActiveSectionsList = UserSession.Instance.ActiveSections;

            // After get Sections Active list, load elements in PickerView
            if (ActiveSectionsList != null && ActiveSectionsList.Count > 0)
            {
                spinnerSectors.Enabled = true;

                ArrayAdapter adapter = new ArrayAdapter(this, global::Android.Resource.Layout.SimpleSpinnerItem, ActiveSectionsList.ToArray());
                spinnerSectors.Adapter = adapter;

                if (ActiveSectionsList != null && mEvent != null)
                {
                    for (int i = 0; i < ActiveSectionsList.Count(); i++)
                    {
                        Section type = ActiveSectionsList[i];
                        if (type.Id == mEvent.Sector.Id)
                        {
                            spinnerSectors.SetSelection(i);
                            break;
                        }
                    }
                }
            }
            else
            {
                spinnerSectors.Enabled = false;

                List<Section> auxSections = new List<Section>();
                auxSections.Add(mEvent.Sector);
                ArrayAdapter adapter = new ArrayAdapter(this, global::Android.Resource.Layout.SimpleSpinnerItem, auxSections.ToArray());
                spinnerSectors.Adapter = adapter;
            }
        }

        private Event BuildEventWillCreateFromUI()
        {
            // Create Event
            Event eventObj = new Event();
            eventObj.Id = mEvent != null ? mEvent.Id : null;
            eventObj.Titulo = titleEditText.Text;
            eventObj.Fecha = DateTime.ParseExact(dateEditText.Text, AysaConstants.FormatDate, null);
            eventObj.Lugar = placeEditText.Text;
            eventObj.Detalle = detailEditText.Text;
            eventObj.Tag = tagsEditText.Text;
            eventObj.Estado = 1;
            eventObj.Tipo = EventTypesList[spinnerType.SelectedItemPosition];

            if (ActiveSectionsList == null || ActiveSectionsList.Count() <= 0)
            {
                eventObj.Sector = mEvent.Sector;
                eventObj.SectorOrigen = mEvent.SectorOrigen;
            }
            else
            {
                Section auxSector = ActiveSectionsList[spinnerSectors.SelectedItemPosition];
                eventObj.Sector = auxSector;
                eventObj.SectorOrigen = auxSector;
            }

            eventObj.Referencia = referenceEditText.Text.Length > 0 ? int.Parse(referenceEditText.Text) : 0;
            eventObj.Usuario = GetUserLogged();
            eventObj.Observaciones = GetObservations(eventObj);


            if (mEvent != null)
            {
                eventObj.Archivos = mEvent.Archivos;
                // Concatenate new files
                if(filesToDeleteInServer.Count() > 0)
                {
                    foreach(AttachmentFile file in filesToDeleteInServer)
                    {
                        eventObj.Archivos.Remove(file);
                    }
                }
                else
                {
                    eventObj.Archivos.AddRange(uploadedAttachmentFiles);
                }
            }
            else
            {

                eventObj.Archivos = uploadedAttachmentFiles;
            }


            // Set Status in case the user is editing an event


                switch (segmentedIndexSelected)
                {
                    case 0:
                        eventObj.Estado = (int)Event.Status.Open;
                        break;
                    case 2:
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

                    if (mEvent == null)
                    {
                        //Create Event
                        eventCreated = await AysaClient.Instance.CreateEvent(eventObj);
                    }
                    else
                    {
                        // Update Event
                        eventCreated = await AysaClient.Instance.UpdateEvent(mEvent.Id, eventObj);
                    }

                    RunOnUiThread(() =>
                    {
                        OnEventCreated();
                    });

                }
                catch (HttpUnauthorized)
                {
                    RunOnUiThread(() =>
                    {
                        // Remove progress
                        ShowProgressDialog(false);

                        ShowSessionExpiredError();
                    });
                }
                catch (Exception ex)
                {
                    RunOnUiThread(() =>
                    {
                        // Remove progress
                        ShowProgressDialog(false);

                        ShowErrorAlert(ex.Message);
                    });
                }
            });
        }

        private void ShowErrorAlert(string message)
        {
            global::Android.Support.V7.App.AlertDialog.Builder alert = new global::Android.Support.V7.App.AlertDialog.Builder(this);
            alert.SetTitle("Error");
            alert.SetMessage(message);

            Dialog dialog = alert.Create();
            dialog.Show();
        }

        private void ShowSessionExpiredError()
        {
            global::Android.Support.V7.App.AlertDialog.Builder alert = new global::Android.Support.V7.App.AlertDialog.Builder(this);
            alert.SetTitle("Aviso");
            alert.SetMessage("Su sesión ha expirado, por favor ingrese sus credenciales nuevamente");

            Dialog dialog = alert.Create();
            dialog.Show();
        }

        private void OnEventCreated()
        {
            global::Android.Support.V7.App.AlertDialog.Builder builder = new global::Android.Support.V7.App.AlertDialog.Builder(this);
            builder.SetTitle("Aviso");
            builder.SetMessage(editMode ? "El evento ha sido editado con éxito" : "El evento ha sido creado con éxito");
            builder.SetPositiveButton("OK", OkAction);
            builder.SetCancelable(false);

            Dialog dialog = builder.Create();
            dialog.Show();
        }

        private void OkAction(object sender, DialogClickEventArgs e)
        {
            SetResult(Result.Ok);
            Finish();
        }
    }



}