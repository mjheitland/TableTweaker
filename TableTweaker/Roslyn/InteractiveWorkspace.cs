﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace TableTweaker.Roslyn
{
    internal class InteractiveWorkspace : Workspace
    {
        //private SourceTextContainer _openTextContainer;
        private DocumentId _openDocumentId;

        internal InteractiveWorkspace(HostServices host)
            : base(host, "Interactive")
        {
        }

        public new void SetCurrentSolution(Solution solution)
        {
            var oldSolution = CurrentSolution;
            var newSolution = base.SetCurrentSolution(solution);
            RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.SolutionChanged, oldSolution, newSolution);
        }

        public override bool CanOpenDocuments => true;

        public override bool CanApplyChange(ApplyChangesKind feature)
        {
            switch (feature)
            {
                case ApplyChangesKind.ChangeDocument:
                    return true;

                default:
                    return false;
            }
        }

        public void OpenDocument(DocumentId documentId, SourceTextContainer textContainer)
        {
            //_openTextContainer = textContainer;
            _openDocumentId = documentId;
            OnDocumentOpened(documentId, textContainer);
        }

        protected override void ApplyDocumentTextChanged(DocumentId document, SourceText newText)
        {
            if (_openDocumentId != document)
            {
                return;
            }

            //ITextSnapshot appliedText;
            //using (var edit = _openTextContainer.GetTextBuffer().CreateEdit(EditOptions.DefaultMinimalChange, reiteratedVersionNumber: null, editTag: null))
            //{
            //    var oldText = _openTextContainer.CurrentText;
            //    var changes = newText.GetTextChanges(oldText);

            //    foreach (var change in changes)
            //    {
            //        edit.Replace(change.Span.Start, change.Span.Length, change.NewText);
            //    }

            //    appliedText = edit.Apply();
            //}

            OnDocumentTextChanged(document, newText, PreservationMode.PreserveIdentity);
        }

        public new void ClearSolution()
        {
            base.ClearSolution();
        }

        internal void ClearOpenDocument(DocumentId documentId)
        {
            base.ClearOpenDocument(documentId);
        }

        internal new void RegisterText(SourceTextContainer textContainer)
        {
            base.RegisterText(textContainer);
        }

        // ReSharper disable once IdentifierTypo
        internal new void UnregisterText(SourceTextContainer textContainer)
        {
            base.UnregisterText(textContainer);
        }
    }
}
