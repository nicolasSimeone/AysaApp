using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.IO;
using Android.Support.V7.App;
using Aysa.PPEMobile.Model;
using Aysa.PPEMobile.Droid.Utilities;
using Android.Text;
using System.Threading.Tasks;
using Aysa.PPEMobile.Service;
using Aysa.PPEMobile.Service.HttpExceptions;
using Android.Support.V4.Content;
using Android.Support.V4.App;
using Android.Content.PM;
using Android;

namespace Aysa.PPEMobile.Droid.Activities
{
    [Activity(Label = "EventDetailActivity")]
    public class EventDetailActivity : AppCompatActivity
    {
        FrameLayout progressOverlay;
        private Event mEvent;
        private TextView tvEventTitle;
        private TextView tvDetailDate;
        private TextView tvDetailPlace;
        private TextView tvDetailType;
        private TextView tvDetailDetail;
        private TextView tvDetailStatus;
        private TextView tvDetailTags;
        private LinearLayout eventsDocumentList;
        private CheckBox confidentialCheckBox;
        MultilineEditText editTextEvDetailGeneral;
        Spinner spinnerSectors;
        List<Section> ActiveSectionsList;
        readonly int EDIT_EVENT_ACTIVITY_CODE = 90;
        Android.Net.Uri pdfpath;

        bool showEditButton;
        bool clickEditButton = false;

        //used if this event was edited through AddEventActivity
        private bool editedEvent = false;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            this.mEvent = EventDataHolder.getInstance().getData();
            showEditButton = mEvent.CanEdit;

            setUpViews();

            LoadActiveSectionsInView();

            loadFullEvent(this.mEvent);

            SetUpViewAccordingUserPermissions();
        }

        private void setUpViews()
        {
            SetContentView(Resource.Layout.EventDetail);

            progressOverlay = FindViewById<FrameLayout>(Resource.Id.progress_overlay);

            // Set toolbar and title
            global::Android.Support.V7.Widget.Toolbar toolbar = (global::Android.Support.V7.Widget.Toolbar)FindViewById(Resource.Id.toolbar);
            toolbar.Title = "Evento #" + mEvent.NroEvento.ToString();

            SetSupportActionBar(toolbar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            tvEventTitle = FindViewById<TextView>(Resource.Id.textViewEvDetailTitle);
            tvDetailDate = FindViewById<TextView>(Resource.Id.textViewEvDetailDate);
            tvDetailPlace = FindViewById<TextView>(Resource.Id.textViewEvDetailPlace);
            tvDetailType = FindViewById<TextView>(Resource.Id.textViewEvDetailType);
            tvDetailDetail = FindViewById<TextView>(Resource.Id.textViewEvDetailDetalle);
            tvDetailStatus = FindViewById<TextView>(Resource.Id.textViewEvDetailStatus);
            tvDetailTags = FindViewById<TextView>(Resource.Id.textViewEvDetailTags);
            confidentialCheckBox = FindViewById<CheckBox>(Resource.Id.confidentialCheckBox);
            editTextEvDetailGeneral = FindViewById<MultilineEditText>(Resource.Id.editTextEvDetailGeneral);
            eventsDocumentList = FindViewById<LinearLayout>(Resource.Id.eventsDocumentList);

            // Sector field
            spinnerSectors = FindViewById<Spinner>(Resource.Id.spinnerSectors);

            Button btnAddNote = FindViewById<Button>(Resource.Id.btnEventDetailSendGeneral);
            btnAddNote.Click += BtnAddNote_Click;

            confidentialCheckBox.Click += Confidential_Checked_Click;
        }

        void BtnAddNote_Click(object sender, System.EventArgs e)
        {
            Observation observationObj = BuildObservation();

            if (observationObj != null)
            {
                SendObservationToServer(observationObj);
            }
        }

        void Confidential_Checked_Click(object sender, System.EventArgs e)
        {
            Android.App.AlertDialog.Builder dialog = new Android.App.AlertDialog.Builder(this);
            Android.App.AlertDialog alert = dialog.Create();
            alert.SetTitle("Aviso");
            string message = mEvent.Confidencial ? "¿Está seguro que desea desmarcar el evento como confidencial?" : "¿Está seguro que desea marcar el evento como confidencial?";
            alert.SetMessage(message);
            alert.SetButton("Si", (c, ev) =>
            {
                ChangeEventStatusCenfidential();
            });

            alert.SetButton2("Cancelar", (c, ev) => { });

            alert.Show();
        }

        private Observation BuildObservation()
        {
            if (editTextEvDetailGeneral.Text.Length == 0)
            {
                ShowErrorAlert("El detalle de la observación no puede ser vacio");
                return null;
            }

            Observation observationObj = new Observation();
            observationObj.Fecha = DateTime.Now;
            observationObj.Observacion = editTextEvDetailGeneral.Text;
            observationObj.Evento = GetEventAssociated();
            observationObj.Usuario = GetUserLogged();
            if (FindViewById(Resource.Id.spinnerSectorContainer).Visibility == ViewStates.Gone)
            {
                observationObj.Sector = mEvent.Sector;
            }
            else
            {
                observationObj.Sector = ActiveSectionsList[spinnerSectors.SelectedItemPosition];
            }

            return observationObj;
        }

        //SECTOR
        public void LoadActiveSectionsInView()
        {
            ActiveSectionsList = UserSession.Instance.ActiveSections;

            if (ActiveSectionsList != null && ActiveSectionsList.Count > 0)
            {
                ArrayAdapter adapter = new ArrayAdapter(this, global::Android.Resource.Layout.SimpleSpinnerItem, ActiveSectionsList.ToArray());
                spinnerSectors.Adapter = adapter;
            }
            else
            {
                FindViewById(Resource.Id.spinnerSectorContainer).Visibility = ViewStates.Gone;
            }
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
            eventObservation.Id = mEvent.Id;

            return eventObservation;
        }

        private void SendObservationToServer(Observation observationObj)
        {
            ShowProgress(true);

            Task.Run(async () =>
            {
                try
                {
                    Observation observation = await AysaClient.Instance.CreateObservation(observationObj);

                    RunOnUiThread(() =>
                    {
                        ShowProgress(false);
                        if (observation != null)
                        {
                            if (mEvent.Observaciones != null)
                            {
                                editTextEvDetailGeneral.Text = "";

                                // Add observation in fist position to show it in first place
                                mEvent.Observaciones.Insert(0, observation);
                                showEventNotes();
                            }
                        }

                        string successMsj = "La observación ha sido creada con éxito";

                        string toast = string.Format(successMsj);
                        Toast.MakeText(this, toast, ToastLength.Long).Show();
                    });

                }
                catch (HttpUnauthorized)
                {
                    RunOnUiThread(() =>
                    {
                        ShowProgress(false);

                        ShowSessionExpiredError();
                    });
                }
                catch (Exception ex)
                {
                    RunOnUiThread(() =>
                    {
                        ShowProgress(false);

                        ShowErrorAlert(ex.Message);
                    });
                }
            });
        }

        private void ChangeEventStatusCenfidential()
        {
            ShowProgress(true);

            Task.Run(async () =>
            {

                try
                {
                    await AysaClient.Instance.SetEventConfidential(mEvent.Id);

                    RunOnUiThread(() =>
                    {
                        mEvent.Confidencial = !mEvent.Confidencial;
                        confidentialCheckBox.Checked = mEvent.Confidencial;
                    });

                }
                catch (HttpUnauthorized)
                {
                    RunOnUiThread(() =>
                    {
                        ShowSessionExpiredError();
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
                    ShowProgress(false);
                }
            });
        }


        private void ShowSessionExpiredError()
        {
            global::Android.Support.V7.App.AlertDialog.Builder alert = new global::Android.Support.V7.App.AlertDialog.Builder(this);
            alert.SetTitle("Aviso");
            alert.SetMessage("Su sesión ha expirado, por favor ingrese sus credenciales nuevamente");

            Dialog dialog = alert.Create();
            dialog.Show();
        }

        public async void loadFullEvent(Event eventSelected)
        {
            ShowProgress(true);
            // Get complete data of event selected from server
            await Task.Run(async () =>
            {
                try
                {
                    mEvent = await AysaClient.Instance.GetEventById(eventSelected.Id);
                    mEvent.Archivos = await AysaClient.Instance.GetFilesOfEvent(mEvent.Id);   
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
            });
            
            // Go to event detail
            showEventData();

            EventDataHolder.getInstance().setData(mEvent);
            if (mEvent.Archivos.Count > 0)
            {
                showEventDocuments();
            }
            ShowProgress(false);
            clickEditButton = true;
        }

        private void showEventData()
        {
            tvEventTitle.Text = mEvent.Titulo;

            String sourceString1 = "<b>Fecha de ocurrencia:</b> " + mEvent.Fecha;
            tvDetailDate.TextFormatted = Html.FromHtml(sourceString1);

            String sourceString2 = "<b>Lugar:</b> " + mEvent.Lugar;
            tvDetailPlace.TextFormatted = Html.FromHtml(sourceString2);

            String sourceString3 = "<b>Tipo:</b> " + mEvent.Tipo;
            tvDetailType.Visibility = ViewStates.Visible;
            tvDetailType.TextFormatted = Html.FromHtml(sourceString3);

            String sourceString4 = "<b>Detalle:</b> " + mEvent.Detalle;
            tvDetailDetail.Visibility = ViewStates.Visible;
            tvDetailDetail.TextFormatted = Html.FromHtml(sourceString4);
             

            String sourceString5 = "<b>Estado:</b> " + (mEvent.Estado == 1 ? "Abierto" : "Cerrado");
            tvDetailStatus.TextFormatted = Html.FromHtml(sourceString5);

            String sourceString6 = mEvent.Tag;
            if (mEvent.Tag == null || mEvent.Tag.Trim().Equals(""))
            {
                tvDetailTags.Visibility = ViewStates.Gone;
            }
            else
            {
                tvDetailTags.Visibility = ViewStates.Visible;
                tvDetailTags.TextFormatted = Html.FromHtml(sourceString6);
            }


            TextView generalTextView = FindViewById<TextView>(Resource.Id.general_textView);
            generalTextView.TextFormatted = Html.FromHtml("<b>General</b> ");
            TextView sectionTextView = FindViewById<TextView>(Resource.Id.section_textView);
            sectionTextView.TextFormatted = Html.FromHtml("<b>Sector</b> ");

            confidentialCheckBox.Checked = mEvent.Confidencial;

            showEventNotes();
        }

        void OnDocumentClick(object sender, AttachmentFile documentSelected)
        {
            DownloadFileToShowIt(documentSelected);
        }

        private void DownloadFileToShowIt(AttachmentFile documentFile)
        {
            ShowProgress(true);

            Task.Run(async () =>
            {

                try
                {
                    byte[] bytesArray = await AysaClient.Instance.GetFile(documentFile.Id);

                    RunOnUiThread(() =>
                    {
                        SaveDocumentInTemporaryFolder(documentFile.FileName, bytesArray);
                    });

                }
                catch (HttpUnauthorized)
                {
                    RunOnUiThread(() =>
                    {
                        ShowSessionExpiredError();
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
                        ShowProgress(false);
                    });
                }
            });
        }

        private void SaveDocumentInTemporaryFolder(String nameFile, byte[] bytes)
        {
            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage) == (int)Permission.Granted)
            {
                // Save Document
                var directory = global::Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
                directory = Path.Combine(directory, Android.OS.Environment.DirectoryDownloads);
                string filePath = Path.Combine(directory, nameFile);
                File.WriteAllBytes(filePath, bytes);

                // Show document saved
                ShowDocumentSaved(filePath);
            }
            else
            {
                ActivityCompat.RequestPermissions(this, new String[] { Manifest.Permission.WriteExternalStorage }, 1);
            }
        }

        private string GetMimeType(string extension)
        {
            switch (extension.ToLower())
            {
                case ".doc":
                case ".docx":
                    return "application/msword";
                case ".pdf":
                    return "application/pdf";
                case ".xls":
                case ".xlsx":
                    return "application/vnd.ms-excel";
                case ".jpg":
                case ".jpeg":
                case ".png":
                    return "image/jpeg";
                case ".txt":
                    return "text/plain";
                default:
                    return "*/*";
            }
        }

        private void ShowDocumentSaved(string filePath)
        {
            if (Build.VERSION.SdkInt >= Build.VERSION_CODES.N)
            {
                pdfpath = FileProvider.GetUriForFile(this, this.PackageName + ".fileprovider", new Java.IO.File(filePath));
            }
            else
            {
                pdfpath = Android.Net.Uri.FromFile(new Java.IO.File(filePath));
            }
            Intent intent = new Intent(Intent.ActionView);
            string mimeType = GetMimeType(Path.GetExtension(filePath));
            intent.SetDataAndType(pdfpath, mimeType);
            intent.SetFlags(ActivityFlags.GrantReadUriPermission);
            intent.AddFlags(ActivityFlags.NoHistory);
            StartActivity(Intent.CreateChooser(intent, "Eliga una aplicación"));
        }

        private void showEventDocuments()
        {
            if (mEvent.Archivos != null)
            {
                LinearLayout documentsContainer = FindViewById<LinearLayout>(Resource.Id.eventsDocumentList);
                documentsContainer.RemoveAllViews();
                foreach (AttachmentFile obs in mEvent.Archivos)
                {
                    string privatePublicc;
                    if(obs.Private)
                    {
                        privatePublicc = "Privado";
                    }
                    else
                    {
                        privatePublicc = "Publico";
                    }
                    LayoutInflater inflater = (LayoutInflater)GetSystemService(Context.LayoutInflaterService);
                    ViewGroup view = (ViewGroup)inflater.Inflate(Resource.Layout.EventDocumentRow, documentsContainer, false);
                    view.FindViewById<TextView>(Resource.Id.txtDocumentName).Text = obs.FileName + "   " + privatePublicc;
                    view.FindViewById<TextView>(Resource.Id.txtDocumentName).PaintFlags = Android.Graphics.PaintFlags.UnderlineText;

                    documentsContainer.AddView(view);
                    view.Click += (sender, e) => OnDocumentClick(this, obs);

                }
                eventsDocumentList.Parent.RequestLayout();
            }
        }

        private void showEventNotes()
        {
            if (mEvent.Observaciones != null)
            {
                LinearLayout eventNotesContainer = FindViewById<LinearLayout>(Resource.Id.event_notes_container);
                eventNotesContainer.RemoveAllViews();
                foreach (Observation obs in mEvent.Observaciones)
                {
                    LayoutInflater inflater = (LayoutInflater)GetSystemService(Context.LayoutInflaterService);
                    ViewGroup view = (ViewGroup)inflater.Inflate(Resource.Layout.EventNotesRow, eventNotesContainer, false);
                    view.FindViewById<TextView>(Resource.Id.txtObservacionCreador).Text = obs.Usuario == null ? "" : obs.Usuario.NombreApellido;
                    view.FindViewById<TextView>(Resource.Id.txtObservacionFecha).Text = obs.Fecha.ToString(AysaConstants.FormatDate);
                    view.FindViewById<TextView>(Resource.Id.txtNotes).Text = obs.Observacion;

                    eventNotesContainer.AddView(view);
                }
            }
        }

        private void SetUpViewAccordingUserPermissions()
        {

            if (UserSession.Instance.CheckIfUserHasPermission(Permissions.ModificarEvento))
            {
                // Allow to user do everything
                // Check if user can edit confidential field.
                // The user can edit confidential field if he has guard responsable in section 1 or 2
                if (!UserSession.Instance.CheckIfUserIsGuardResponsableOfMainSection())
                {
                    // The user doesn't have guard responsable in section 1 or 2, he can't edit confidential field
                    HiddenConfidentialField();
                }

                return;
            }
            else
            {
                if (UserSession.Instance.CheckIfUserHasPermission(Permissions.ModificarEventoAutorizado))
                {
                    if (UserSession.Instance.CheckIfUserHasActiveSections())
                    {
                        // User has active section so he can add observations

                        // Check if user can edit confidential field.
                        // The user can edit confidential field if he has guard responsable in section 1 or 2
                        if (!UserSession.Instance.CheckIfUserIsGuardResponsableOfMainSection())
                        {
                            // The user doesn't have guard responsable in section 1 or 2, he can't edit confidential field
                            HiddenConfidentialField();
                        }

                        return;
                    }
                    else
                    {
                        // User doesn't have active sections so he can't add observations
                        // The user only can edit the events that they were created by himself
                        
                        HiddenAddObservationContent();
                        HiddenConfidentialField();
                        return;
                    }
                }
            }
        }

        private void HiddenConfidentialField()
        {
            View confidentialContent = FindViewById<View>(Resource.Id.confidentialContent);
            confidentialContent.Visibility = ViewStates.Gone;
        }

        private void HiddenAddObservationContent()
        {
            View addObservationContent = FindViewById<View>(Resource.Id.addObservationContent);
            addObservationContent.Visibility = ViewStates.Gone;
        }

        private void ShowProgress(bool show)
        {
            progressOverlay.Visibility = show ? ViewStates.Visible : ViewStates.Gone;
        }

        private void ShowErrorAlert(string message)
        {
            global::Android.Support.V7.App.AlertDialog.Builder alert = new global::Android.Support.V7.App.AlertDialog.Builder(this);
            alert.SetTitle("Error");
            alert.SetMessage(message);

            if (!IsFinishing)
            {
                Dialog dialog = alert.Create();
                dialog.Show();
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {

            MenuInflater.Inflate(Resource.Menu.evdetails_menu, menu);
            menu.GetItem(0).SetShowAsAction(ShowAsAction.Always);

            // Disable edit button in case that the user doesn't have permissions 
            menu.GetItem(0).SetVisible(showEditButton);


            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
        
            if (item.ItemId == Resource.Id.action_edit_event)
            {
                if (clickEditButton)
                {
                    var editEventActivity = new Intent(this, typeof(AddEventActivity));
                    StartActivityForResult(editEventActivity, EDIT_EVENT_ACTIVITY_CODE);

                    return true;
                }
                
            }
            return base.OnOptionsItemSelected(item);
        }


        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if (requestCode == EDIT_EVENT_ACTIVITY_CODE)
            {
                if (resultCode.Equals(Result.Ok))
                {
                    editedEvent = true;
                    loadFullEvent(this.mEvent);
                }
            }
        }

        public override bool OnSupportNavigateUp()
        {
            OnBackPressed();
            return true;
        }

        public override void OnBackPressed()
        {
            if (editedEvent)
            {
                SetResult(Result.Ok);
                Finish();
            }
            else
            {
                base.OnBackPressed();
            }
        }
    }
}