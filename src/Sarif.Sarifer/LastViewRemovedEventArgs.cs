﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// Provides data for the event handler invoked when the last <see cref="ITextView"/> on an
    /// <see cref="ITextBuffer"/> is closed.
    /// </summary>
    public class LastViewRemovedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LastViewRemovedEventArgs"/> class.
        /// </summary>
        /// <param name="logId">
        /// The unique id of the SARIF log associated with this text buffer.
        /// </param>
        public LastViewRemovedEventArgs(string logId)
        {
            this.LogId = logId;
        }

        /// <summary>
        /// Gets the unique id of the SARIF log associated with this text buffer.
        /// </summary>
        public string LogId { get; }
    }
}