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
using Android;
using Android.Content.PM;
using Android.Support.V4.Content;
using Android.Support.V4.App;
using Uri = Android.Net.Uri;

namespace Aysa.PPEMobile.Droid.Activities
{
    [Activity(Label = "AddFeatureActivity")]
    public class AddFeatureActivity : AppCompatActivity
    {
        public static readonly int PickImageId = 1000;

        FrameLayout progressOverlay;

        // Private Variables
        List<Section> ActiveSectionsList;
        Section Sectors;

        EditText dateEditText;
        EditText detailEditText;
        EditText authorEditText;
        Spinner spinnerSectors;
        LinearLayout filesContainer;

        Button btnSegmented1;
        Button btnSegmented2;

        List<AttachmentFile> attachedFiles = new List<AttachmentFile>();
        List<AttachmentFile> uploadedAttachmentFiles = new List<AttachmentFile>();
        List<AttachmentFile> filesToDeleteInServer = new List<AttachmentFile>();
        int countAttachmentsUploaded = 0;
        int countAttachmentsDeleted = 0;

        private Feature mFeature;

        private bool editMode = false;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            this.mFeature = FeatureDataHolder.getInstance().getData();

            SetContentView(Resource.Layout.AddFeature);
            progressOverlay = FindViewById<FrameLayout>(Resource.Id.progress_overlay);

            Button btnCreate = FindViewById<Button>(Resource.Id.btnEventCreate);
            btnCreate.Click += BtnCreate_Click;

            global::Android.Support.V7.Widget.Toolbar toolbar = (global::Android.Support.V7.Widget.Toolbar)FindViewById(Resource.Id.toolbar);

            if (mFeature != null)
            {
                //MODO EDITAR NOVEDAD
                toolbar.Title = "Editar Novedad #" + mFeature.Detail;
                btnCreate.Text = "Guardar";
                editMode = true;
            }
            else
            {
                //MODO AGREGAR NUEVO NOVEDAD
                toolbar.SetTitle(Resource.String.add_feature_title);
                attachedFiles = new List<AttachmentFile>();
            }

            // Add back button
            SetSupportActionBar(toolbar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);


            dateEditText = FindViewById<EditText>(Resource.Id.editTextEventDate);
            detailEditText = FindViewById<EditText>(Resource.Id.editTextEventDetail);
            filesContainer = FindViewById<LinearLayout>(Resource.Id.addEventFilesContainer);
            authorEditText = FindViewById<EditText>(Resource.Id.editTextAuthor);

            User usuario = GetUserLogged();
            authorEditText.Text = usuario.NombreApellido;

            FindViewById<Button>(Resource.Id.btnUploadFiles).Click += UploadFile_Click;

            if (mFeature != null)
            {


                dateEditText.Text = mFeature.Date.ToString(AysaConstants.FormatDate);
                dateEditText.Enabled = false;
                detailEditText.Text = mFeature.Detail;

                if (mFeature.Archivos != null)
                {
                    foreach (AttachmentFile file in mFeature.Archivos)
                    {
                        addAttachedFile(file.FileName);
                    }
                }

                attachedFiles.AddRange(mFeature.Archivos);
            }

            // Date Field
            dateEditText.Click += DateEditText_Click;

            // Sector field
            spinnerSectors = FindViewById<Spinner>(Resource.Id.spinnerSectors);

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
        }

        private void addAttachedFile(string filename)
        {

            LayoutInflater inflater = (LayoutInflater)GetSystemService(Context.LayoutInflaterService);
            ViewGroup view = (ViewGroup)inflater.Inflate(Resource.Layout.AddEventDocumentRow, filesContainer, false);
            view.FindViewById<TextView>(Resource.Id.txtDocumentName).Text = filename;
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
                                UploadFeatureToServer();

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
                if (mFeature != null)
                {
                    if (mFeature.Usuario.NombreApellido == authorEditText.Text)
                    {
                        AttemptFeatureUpload();
                    }
                    else
                    {
                        OnFeatureModified();
                    }
                }
                else
                {
                    AttemptFeatureUpload();
                }
            }
        }

        private void AttemptFeatureUpload()
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
                    UploadFeatureToServer();

                }
            }
        }


        private void UploadFeatureToServer()
        {
            Feature eventObj = BuildFeatureWillCreateFromUI();

            if (eventObj != null)
            {
                SendFeatureToServer(eventObj);
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
                            UploadFeatureToServer();
                        }

                        return;
                    }

                    AttachmentFile fileUpload = await AysaClient.Instance.UploadFileFeature(fileInMemory.BytesArray, fileInMemory.FileName);

                    //AttachmentFile fileUpload = await AysaClient.Instance.UploadFile(attachmentFile.BytesArray, attachmentFile.FileName);

                    RunOnUiThread(() =>
                    {

                        // Save uploaded file in list, this list will be assigned to the event that it will be created
                        uploadedAttachmentFiles.Add(fileUpload);

                        countAttachmentsUploaded++;

                        if (countAttachmentsUploaded < attachedFiles.Count)
                        {

                            UploadFilesToServer();
                        }
                        else
                        {
                            UploadFeatureToServer();
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

            // Validate Fecha de Ocurrencia
            if (IsEditTextEmpty(dateEditText))
            {
                return "El campo Fecha de Ocurrencia no puede estar  vacío";
            }

            // Validate Lugar
            if (IsEditTextEmpty(detailEditText))
            {
                return "El campo Detalle no puede estar  vacío";
            }

            // Validate Section
            if (IsSpinnerEmpty(spinnerSectors))
            {
                return "Es necesario seleccionar un Sector";
            }

            return "";
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
            user.Id = UserSession.Instance.Id   ;
            user.NombreApellido = UserSession.Instance.nomApel;

            return user;
        }


        //public void GetEventTypesFromServer()
        //{
        //    // Get events type from server

        //    Task.Run(async () =>
        //    {

        //        try
        //        {
        //            EventTypesList = await AysaClient.Instance.GetEventsType();

        //            RunOnUiThread(() =>
        //            {
        //                // Fill spinner items
        //                ArrayAdapter adapter = new ArrayAdapter(this, global::Android.Resource.Layout.SimpleSpinnerItem, EventTypesList.ToArray());
        //                spinnerType.Adapter = adapter;

        //                if (EventTypesList != null && mFeature != null)
        //                {
        //                    for (int i = 0; i < EventTypesList.Count(); i++)
        //                    {
        //                        EventType type = EventTypesList[i];
        //                        if (type.Id == "1")
        //                        {
        //                            spinnerType.SetSelection(i);
        //                            break;
        //                        }
        //                    }
        //                }
        //            });

        //        }
        //        catch (HttpUnauthorized)
        //        {
        //            RunOnUiThread(() =>
        //            {
        //                ShowErrorAlert("Sesión expirada.");
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
        //        }
        //    });

        //}

        public void LoadActiveSectionsInView()
        {
            ActiveSectionsList = UserSession.Instance.ActiveSections;

            // After get Sections Active list, load elements in PickerView
            if (ActiveSectionsList != null && ActiveSectionsList.Count > 0)
            {
                spinnerSectors.Enabled = true;

                ArrayAdapter adapter = new ArrayAdapter(this, global::Android.Resource.Layout.SimpleSpinnerItem, ActiveSectionsList.ToArray());
                spinnerSectors.Adapter = adapter;

                if (ActiveSectionsList != null && mFeature != null)
                {
                    for (int i = 0; i < ActiveSectionsList.Count(); i++)
                    {
                        Section type = ActiveSectionsList[i];
                        if (type.Id == mFeature.Sector.Id)
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
                auxSections.Add(mFeature.Sector);
                ArrayAdapter adapter = new ArrayAdapter(this, global::Android.Resource.Layout.SimpleSpinnerItem, auxSections.ToArray());
                spinnerSectors.Adapter = adapter;
            }
        }

        private Feature BuildFeatureWillCreateFromUI()
        {
            // Create Feature
            Feature eventObj = new Feature();
            eventObj.Id = mFeature != null ? mFeature.Id : null;
            eventObj.Date = DateTime.ParseExact(dateEditText.Text, AysaConstants.FormatDate, null);
            eventObj.Detail = detailEditText.Text;

            if (ActiveSectionsList == null || ActiveSectionsList.Count() <= 0)
            {
                eventObj.Sector = Sectors;
            }
            else
            {
                Section auxSector = ActiveSectionsList[spinnerSectors.SelectedItemPosition];
                eventObj.Sector = auxSector;
            }

            eventObj.Usuario = GetUserLogged();


            if (mFeature != null)
            {
                eventObj.Archivos = mFeature.Archivos;
                // Concatenate new files
                if(filesToDeleteInServer.Count() > 0)
                {
                    foreach (AttachmentFile file in filesToDeleteInServer)
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

            return eventObj;
        }

        private void SendFeatureToServer(Feature eventObj)
        {

            Task.Run(async () =>
            {

                try
                {
                    Feature featureCreated;

                    if (mFeature == null)
                    {
                        //Create Feature
                        featureCreated = await AysaClient.Instance.CreateFeature(eventObj);
                    }
                    else
                    {
                        // Update Feature
                        featureCreated = await AysaClient.Instance.UpdateFeature(mFeature.Id, eventObj);
                        FeatureDataHolder.getInstance().setData(featureCreated);
                    }

                    RunOnUiThread(() =>
                    {
                        OnFeatureCreated();
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

        private void OnFeatureCreated()
        {
            global::Android.Support.V7.App.AlertDialog.Builder builder = new global::Android.Support.V7.App.AlertDialog.Builder(this);
            builder.SetTitle("Aviso");
            builder.SetMessage(editMode ? "La novedad ha sido editada con éxito" : "La novedad ha sido creada con éxito");
            builder.SetPositiveButton("OK", OkAction);
            builder.SetCancelable(false);

            Dialog dialog = builder.Create();
            dialog.Show();
        }

        private void OnFeatureModified()
        {
            global::Android.Support.V7.App.AlertDialog.Builder builder = new global::Android.Support.V7.App.AlertDialog.Builder(this);
            builder.SetTitle("Aviso");
            builder.SetMessage("No puedes modificar una novedad de la que no eres autor.");
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