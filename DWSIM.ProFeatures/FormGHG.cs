﻿
using DWSIM.Interfaces;
using System;
using System.Diagnostics;

namespace DWSIM.ProFeatures
{


    public partial class FormGHG : WeifenLuo.WinFormsUI.Docking.DockContent
    {

        public IFlowsheet CurrentFlowsheet;

        public IAnalyticsProvider AnalyticsProvider;

        public FormGHG()
        {
            InitializeComponent();
            lblFeature = _lblFeature;
            _lblFeature.Name = "lblFeature";
        }

        private void Button2_Click(object sender, EventArgs e)
        {

            Functions.CreateTransitionObject(CurrentFlowsheet, "", "GHG Emissions", "", "", default);

            Functions.DisplayTransitionForm(AnalyticsProvider, CurrentFlowsheet, "GHG Emissions");

        }

        private void FormGHG_Load_1(object sender, EventArgs e)
        {
            ExtensionMethods.FormExtensions.ChangeDefaultFont(this);
        }
    }
}