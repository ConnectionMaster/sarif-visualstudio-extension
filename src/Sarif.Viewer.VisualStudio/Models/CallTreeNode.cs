﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Converters;
using Microsoft.Sarif.Viewer.Sarif;
using Microsoft.VisualStudio.Text.Adornments;

namespace Microsoft.Sarif.Viewer.Models
{
    internal class CallTreeNode : CodeLocationObject
    {
        private ThreadFlowLocation _location;
        private CallTree _callTree;
        private CallTreeNode _parent;
        private bool _isExpanded;
        private Visibility _visbility;

        public CallTreeNode(int resultId, int runIndex)
            : base(resultId, runIndex)
        {
        }

        [Browsable(false)]
        public ThreadFlowLocation Location
        {
            get
            {
                return this._location;
            }

            set
            {
                this._location = value;

                if (value?.Location?.PhysicalLocation != null)
                {
                    // If the backing ThreadFlowLocation has a PhysicalLocation, set the
                    // Region property. If it has a FileLocation, set the FilePath.
                    // The FilePath and Region properties are used to navigate to the
                    // source location and highlight the line.
                    this.Region = value.Location.PhysicalLocation.Region;

                    if (value.Location.PhysicalLocation.ArtifactLocation?.Uri != null)
                    {
                        this.FilePath = value.Location.PhysicalLocation.ArtifactLocation.Uri.ToPath();
                    }
                }
                else
                {
                    this.FilePath = null;
                    this.Region = null;
                }
            }
        }

        public bool IsExpanded
        {
            get
            {
                return this._isExpanded;
            }

            set
            {
                if (value != this._isExpanded)
                {
                    this._isExpanded = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public Visibility Visibility
        {
            get
            {
                return this._visbility;
            }

            set
            {
                if (value != this._visbility)
                {
                    this._visbility = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets the location string formatted for Visual Studio.
        /// e.g. myfile.c (24,10).
        /// </summary>
        [Browsable(false)]
        public string LocationDisplayString
        {
            get
            {
                string text = string.Empty;

                if (!string.IsNullOrEmpty(this.FilePath))
                {
                    text = Path.GetFileName(this.FilePath) + " ";
                }

                Region region = this.Location?.Location?.PhysicalLocation?.Region;
                if (region?.StartLine > 0)
                {
                    text += region.FormatForVisualStudio();
                }

                return text;
            }
        }

        internal override ResultTextMarker LineMarker
        {
            get
            {
                // Not all locations have regions. Don't try to mark the locations that don't.
                if (this._lineMarker == null
                    && this.Region != null)
                {
                    this._lineMarker = new ResultTextMarker(
                        runIndex: this.RunIndex,
                        resultId: this.ResultId,
                        uriBaseId: this.UriBaseId,
                        region: this.Region,
                        fullFilePath: this.FilePath,
                        nonHighlightedColor: this.DefaultSourceHighlightColor,
                        highlightedColor: this.SelectedSourceHighlightColor,
                        errorType: PredefinedErrorTypeNames.Suggestion, // Suggestion => no squiggle
                        tooltipContent: CallTreeNodeToTextConverter.MakeDisplayString(this),
                        context: this);
                }

                return this._lineMarker;
            }
        }

        [Browsable(false)]
        public override string DefaultSourceHighlightColor
        {
            get
            {
                if (this.Location.Importance == ThreadFlowLocationImportance.Essential)
                {
                    return ResultTextMarker.KEYEVENT_SELECTION_COLOR;
                }
                else
                {
                    return ResultTextMarker.LINE_TRACE_SELECTION_COLOR;
                }
            }
        }

        [Browsable(false)]
        public override string SelectedSourceHighlightColor
        {
            get
            {
                return ResultTextMarker.HOVER_SELECTION_COLOR;
            }
        }

        [Browsable(false)]
        public List<CallTreeNode> Children { get; set; }

        [Browsable(false)]
        public CallTree CallTree
        {
            get
            {
                return this._callTree;
            }

            set
            {
                this._callTree = value;

                // If there are any children, set their call tree too.
                if (this.Children != null)
                {
                    for (int i = 0; i < this.Children.Count; i++)
                    {
                        this.Children[i].CallTree = this._callTree;
                    }
                }
            }
        }

        [Browsable(false)]
        public CallTreeNode Parent
        {
            get
            {
                return this._parent;
            }

            set
            {
                this._parent = value;

                // Set our call tree to our new parent's call tree.
                if (this._parent != null)
                {
                    this.CallTree = this._parent.CallTree;
                }
            }
        }

        [Category("Location")]
        [DisplayName("Source file")]
        public string SourceFile
        {
            get
            {
                return this.FilePath;
            }
        }

        [Category("Location")]
        [DisplayName("Start line")]
        public int? StartLine
        {
            get
            {
                return this.Location?.Location?.PhysicalLocation?.Region?.StartLine;
            }
        }

        [Category("Location")]
        [DisplayName("End line")]
        public int? EndLine
        {
            get
            {
                return this.Location?.Location?.PhysicalLocation?.Region?.EndLine;
            }
        }

        [Category("Location")]
        [DisplayName("Start column")]
        public int? StartColumn
        {
            get
            {
                return this.Location?.Location?.PhysicalLocation?.Region?.StartColumn;
            }
        }

        [Category("Location")]
        [DisplayName("End column")]
        public int? EndColumn
        {
            get
            {
                return this.Location?.Location?.PhysicalLocation?.Region?.EndColumn;
            }
        }

        public ThreadFlowLocationImportance? Importance
        {
            get
            {
                return this.Location?.Importance;
            }
        }

        public string Message
        {
            get
            {
                return this.Location?.Location?.Message?.Text;
            }
        }

        public string Snippet
        {
            get
            {
                return this.Location?.Location?.PhysicalLocation?.Region?.Snippet?.Text;
            }
        }

        public Dictionary<string, string> Properties
        {
            get
            {
                var properties = new Dictionary<string, string>();

                if (this.Location?.PropertyNames != null)
                {
                    foreach (string key in this.Location.PropertyNames)
                    {
                        properties.Add(key, this.Location.GetProperty<object>(key).ToString());
                    }
                }

                return properties;
            }
        }

        internal void ExpandAll()
        {
            this.IsExpanded = true;

            if (this.Children != null)
            {
                foreach (CallTreeNode child in this.Children)
                {
                    child.ExpandAll();
                }
            }
        }

        internal void CollapseAll()
        {
            this.IsExpanded = false;

            if (this.Children != null)
            {
                foreach (CallTreeNode child in this.Children)
                {
                    child.CollapseAll();
                }
            }
        }

        internal void IntelligentExpand()
        {
            if (this.Location?.Importance == ThreadFlowLocationImportance.Essential)
            {
                CallTreeNode current = this;

                while (current != null)
                {
                    current.IsExpanded = true;
                    current = current.Parent;
                }
            }
            else
            {
                this.IsExpanded = false;
            }

            if (this.Children != null)
            {
                foreach (CallTreeNode child in this.Children)
                {
                    child.IntelligentExpand();
                }
            }
        }

        internal void SetVerbosity(ThreadFlowLocationImportance importance)
        {
            Visibility visibility = Visibility.Visible;
            ThreadFlowLocationImportance myImportance = this.Location?.Importance ?? ThreadFlowLocationImportance.Unimportant;

            switch (importance)
            {
                case ThreadFlowLocationImportance.Essential:
                    if (myImportance != ThreadFlowLocationImportance.Essential)
                    {
                        visibility = Visibility.Collapsed;
                    }

                    break;
                case ThreadFlowLocationImportance.Important:
                    if (myImportance == ThreadFlowLocationImportance.Unimportant)
                    {
                        visibility = Visibility.Collapsed;
                    }

                    break;
                default:
                    visibility = Visibility.Visible;
                    break;
            }

            if (visibility == Visibility.Visible)
            {
                CallTreeNode current = this;

                while (current != null)
                {
                    current.Visibility = Visibility.Visible;
                    current = current.Parent;
                }
            }
            else
            {
                this.Visibility = Visibility.Collapsed;
            }

            if (this.Children != null)
            {
                foreach (CallTreeNode child in this.Children)
                {
                    child.SetVerbosity(importance);
                }
            }
        }
    }
}
