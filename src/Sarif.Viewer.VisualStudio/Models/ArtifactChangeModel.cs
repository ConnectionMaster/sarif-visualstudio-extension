﻿// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Sarif.Viewer.Models
{
    public class ArtifactChangeModel : NotifyPropertyChangedObject
    {
        private string _filePath;
        private ObservableCollection<ReplacementModel> _replacements;

        public ArtifactChangeModel()
        {
            this._replacements = new ObservableCollection<ReplacementModel>();
        }

        public string  FilePath
        {
            get
            {
                return _filePath;
            }
            set
            {
                if (value != this._filePath)
                {
                    _filePath = value;

                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(this.FileName));
                }
            }
        }

        public string FileName
        {
            get
            {
                return Path.GetFileName(this.FilePath);
            }
        }

        public ObservableCollection<ReplacementModel> Replacements
        {
            get
            {
                return this._replacements;
            }
        }
    }
}
