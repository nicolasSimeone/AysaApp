
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Aysa.PPEMobile.Droid.Fragments;

namespace Aysa.PPEMobile.Droid.Activities
{
    [Activity(Label = "ContainerDocumentsActivity")]
    public class ContainerDocumentsActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.ContainerDocument);

            global::Android.Support.V7.Widget.Toolbar toolbar = (global::Android.Support.V7.Widget.Toolbar)FindViewById(Resource.Id.toolbar);

            toolbar.Title = "Documentos Offline";
            SetSupportActionBar(toolbar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            Android.Support.V4.App.FragmentTransaction fragmentTx = SupportFragmentManager.BeginTransaction();
            DocumentsFragment detailsFrag = new DocumentsFragment();
            fragmentTx.Add(Resource.Id.titles_fragment, detailsFrag);
            fragmentTx.Commit();
        }

        public override bool OnSupportNavigateUp()
        {
            OnBackPressed();
            return true;
        }
    }
}
