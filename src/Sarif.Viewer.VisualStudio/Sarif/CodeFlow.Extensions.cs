﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;
using Microsoft.Sarif.Viewer.VisualStudio;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.Sarif.Viewer.Sarif
{
    internal static class CodeFlowExtensions
    {
        public static LocationCollection ToThreadFlowLocationCollection(this CodeFlow codeFlow, Run run, int resultId, int runIndex)
        {
            if (codeFlow == null)
            {
                return null;
            }

            var model = new LocationCollection(codeFlow.Message.Text);

            if (codeFlow.ThreadFlows?[0]?.Locations != null)
            {
                foreach (ThreadFlowLocation location in codeFlow.ThreadFlows[0].Locations)
                {
                    // TODO we are not yet properly hardened against locationless
                    // code locations (and what this means is also in flux as
                    // far as SARIF producers). For now we skip these.
                    if (location.Location?.PhysicalLocation == null) { continue; }

                    model.Add(location.ToLocationModel(run, resultId, runIndex));
                }
            }

            return model;
        }

        public static CallTree ToCallTree(this CodeFlow codeFlow, Run run, int resultId, int runIndex)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (codeFlow.ThreadFlows?[0]?.Locations?.Count == 0)
            {
                return null;
            }

            List<CallTreeNode> topLevelNodes = CodeFlowToTreeConverter.Convert(codeFlow, run, resultId, runIndex);

            return new CallTree(topLevelNodes);
        }
    }
}
