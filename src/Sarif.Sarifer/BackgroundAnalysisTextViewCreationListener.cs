﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;

using Microsoft.Sarif.Viewer.Interop;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// An <see cref="ITextViewCreationListener"/> that triggers background analysis.
    /// </summary>
    [ContentType(AnyContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [Export(typeof(ITextViewCreationListener))]
    public class BackgroundAnalysisTextViewCreationListener : ITextViewCreationListener, IDisposable
    {
        private const string AnyContentType = "any";
        private SarifViewerInterop sarifViewerInterop;
        private bool subscribed;
        private bool disposed;

#pragma warning disable IDE0044, CS0649 // Provided by MEF
        [Import]
        private Lazy<IBackgroundAnalysisService> backgroundAnalysisService;

        [Import]
        private Lazy<IVsEditorAdaptersFactoryService> vsEditorAdaptersFactoryService;

        [Import]
        private ITextBufferViewTracker textBufferViewTracker;
#pragma warning restore IDE0044, CS0649

        /// <inheritdoc/>
        public void TextViewCreated(ITextView textView)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (this.sarifViewerInterop == null)
            {
                if (Package.GetGlobalService(typeof(SVsShell)) is IVsShell vsShell)
                {
                    this.sarifViewerInterop = new SarifViewerInterop(vsShell);
                }
            }

            if (!this.subscribed)
            {
                // ITextViewCreationListener is not IDisposable, so the ITextBufferManager will
                // never be removed from memory. This isn't a problem because the listener will
                // never be removed from memory either; we want it to live as long as the extension
                // is loaded.
                this.textBufferViewTracker.FirstViewAdded += this.TextBufferViewTracker_FirstViewAdded;
                this.textBufferViewTracker.LastViewRemoved += this.TextBufferViewTracker_LastViewRemoved;
                this.subscribed = true;
            }

            textView = textView ?? throw new ArgumentNullException(nameof(textView));

            string text = textView.TextBuffer.CurrentSnapshot.GetText();
            string path = this.GetPathFromTextView(textView);

            textView.Closed += (object sender, EventArgs e) => this.TextView_Closed(textView);

            this.textBufferViewTracker.AddTextView(textView, path, text);
        }

        private void TextBufferViewTracker_FirstViewAdded(object sender, FirstViewAddedEventArgs e)
        {
            this.backgroundAnalysisService.Value.AnalyzeAsync(e.Path, e.Text, e.CancellationToken)
                .FileAndForget(FileAndForgetEventName.BackgroundAnalysisFailure);
        }

        private void TextView_Closed(ITextView textView)
        {
            this.textBufferViewTracker.RemoveTextView(textView);
        }

        // If this is the last view on a buffer, remove any results for this buffer from the
        // error list.
        private void TextBufferViewTracker_LastViewRemoved(object sender, LastViewRemovedEventArgs e)
        {
            this.sarifViewerInterop.CloseSarifLogAsync(new string[] { e.Path })
                .FileAndForget(FileAndForgetEventName.CloseSarifLogsFailure);
        }

        private string GetPathFromTextView(ITextView textView)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            IVsTextView vsTextView = this.vsEditorAdaptersFactoryService.Value.GetViewAdapter(textView);
            if (vsTextView == null)
            {
                return null;
            }

            if (vsTextView.GetBuffer(out IVsTextLines vsTextLines) != VSConstants.S_OK)
            {
                return null;
            }

            if (!(vsTextLines is IPersistFileFormat persistFile))
            {
                return null;
            }

            return persistFile.GetCurFile(out string path, out _) == VSConstants.S_OK
                ? path
                : null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.textBufferViewTracker.Clear();
                }

                this.disposed = true;
            }
        }

        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
