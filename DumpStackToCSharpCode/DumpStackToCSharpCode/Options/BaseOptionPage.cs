﻿using System;
using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace DumpStackToCSharpCode.Options
{
    /// <summary>
    /// A base class for a DialogPage to show in Tools -> Options.
    /// </summary>
    internal class BaseOptionPage<T> : DialogPage where T : BaseOptionModel<T>, new()
    {
        protected readonly BaseOptionModel<T> _model;

        public EventHandler<bool> OnSettingsPageActivate;
        public BaseOptionPage()
        {
#pragma warning disable VSTHRD104 // Offer async methods
            _model = ThreadHelper.JoinableTaskFactory.Run(BaseOptionModel<T>.CreateAsync);
#pragma warning restore VSTHRD104 // Offer async methods
        }

        public override object AutomationObject => _model;

        public override void LoadSettingsFromStorage()
        {
            _model.Load();
        }

        public override void SaveSettingsToStorage()
        {
            _model.Save();
        }

        public void SubstribeForModelSave(EventHandler<bool> @event)
        {
            _model.OnSettingsSave += @event;
        }

        protected override void OnActivate(CancelEventArgs e)
        {
            OnSettingsPageActivate?.Invoke(this, true);
            LoadSettingsFromStorage();
            base.OnActivate(e);
        }
    }
}
