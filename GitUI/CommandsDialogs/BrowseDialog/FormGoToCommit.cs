﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using GitCommands;
using GitUIPluginInterfaces;
using ResourceManager;

namespace GitUI.CommandsDialogs.BrowseDialog
{
    public sealed partial class FormGoToCommit : GitModuleForm
    {
        /// <summary>
        /// this will be used when Go() is called
        /// </summary>
        private string _selectedRevision;

        // these two are used to prepare for _selectedRevision
        private IGitRef _selectedTag;
        private IGitRef _selectedBranch;

        private readonly AsyncLoader _tagsLoader;
        private readonly AsyncLoader _branchesLoader;

        public FormGoToCommit(GitUICommands commands)
            : base(commands)
        {
            InitializeComponent();
            Translate();
            _tagsLoader = new AsyncLoader();
            _branchesLoader = new AsyncLoader();
        }

        private void FormGoToCommit_Load(object sender, EventArgs e)
        {
            LoadTagsAsync();
            LoadBranchesAsync();
            SetCommitExpressionFromClipboard();
        }

        /// <summary>
        /// returns null if revision does not exist (could not be revparsed)
        /// </summary>
        public string ValidateAndGetSelectedRevision()
        {
            string guid = Module.RevParse(_selectedRevision);
            if (!string.IsNullOrEmpty(guid))
            {
                return guid;
            }

            return null;
        }

        private void commitExpression_TextChanged(object sender, EventArgs e)
        {
            SetSelectedRevisionByFocusedControl();
        }

        private void Go()
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void goButton_Click(object sender, EventArgs e)
        {
            Go();
        }

        private void linkGitRevParse_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(@"https://www.kernel.org/pub/software/scm/git/docs/git-rev-parse.html#_specifying_revisions");
        }

        private void LoadTagsAsync()
        {
            comboBoxTags.Text = Strings.GetLoadingData();
            ThreadHelper.JoinableTaskFactory.RunAsync(() =>
            {
                return _tagsLoader.LoadAsync(
                    () => Module.GetTagRefs(GitModule.GetTagRefsSortOrder.ByCommitDateDescending).ToList(),
                    list =>
                    {
                        comboBoxTags.Text = string.Empty;
                        GitRefsToDataSource(comboBoxTags, list);
                        comboBoxTags.DisplayMember = "LocalName";
                        SetSelectedRevisionByFocusedControl();
                    });
            });
        }

        private void LoadBranchesAsync()
        {
            comboBoxBranches.Text = Strings.GetLoadingData();
            ThreadHelper.JoinableTaskFactory.RunAsync(() =>
            {
                return _branchesLoader.LoadAsync(
                    () => Module.GetRefs(false).ToList(),
                    list =>
                    {
                        comboBoxBranches.Text = string.Empty;
                        GitRefsToDataSource(comboBoxBranches, list);
                        comboBoxBranches.DisplayMember = "LocalName";
                        SetSelectedRevisionByFocusedControl();
                    });
            });
        }

        private static void GitRefsToDataSource(ComboBox cb, IReadOnlyList<IGitRef> refs)
        {
            cb.DataSource = refs;
        }

        private static IReadOnlyList<IGitRef> DataSourceToGitRefs(ComboBox cb)
        {
            return (IReadOnlyList<IGitRef>)cb.DataSource;
        }

        private void comboBoxTags_Enter(object sender, EventArgs e)
        {
            SetSelectedRevisionByFocusedControl();
        }

        private void comboBoxBranches_Enter(object sender, EventArgs e)
        {
            SetSelectedRevisionByFocusedControl();
        }

        private void SetSelectedRevisionByFocusedControl()
        {
            if (textboxCommitExpression.Focused)
            {
                _selectedRevision = textboxCommitExpression.Text.Trim();
            }
            else if (comboBoxTags.Focused)
            {
                _selectedRevision = _selectedTag != null ? _selectedTag.Guid : "";
            }
            else if (comboBoxBranches.Focused)
            {
                _selectedRevision = _selectedBranch != null ? _selectedBranch.Guid : "";
            }
        }

        private void comboBoxTags_TextChanged(object sender, EventArgs e)
        {
            if (comboBoxTags.DataSource == null)
            {
                return;
            }

            _selectedTag = DataSourceToGitRefs(comboBoxTags).FirstOrDefault(a => a.LocalName == comboBoxTags.Text);
            SetSelectedRevisionByFocusedControl();
        }

        private void comboBoxBranches_TextChanged(object sender, EventArgs e)
        {
            if (comboBoxBranches.DataSource == null)
            {
                return;
            }

            _selectedBranch = DataSourceToGitRefs(comboBoxBranches).FirstOrDefault(a => a.LocalName == comboBoxBranches.Text);
            SetSelectedRevisionByFocusedControl();
        }

        private void comboBoxTags_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (comboBoxTags.SelectedValue == null)
            {
                return;
            }

            _selectedTag = (IGitRef)comboBoxTags.SelectedValue;
            SetSelectedRevisionByFocusedControl();
            Go();
        }

        private void comboBoxBranches_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (comboBoxBranches.SelectedValue == null)
            {
                return;
            }

            _selectedBranch = (IGitRef)comboBoxBranches.SelectedValue;
            SetSelectedRevisionByFocusedControl();
            Go();
        }

        private void comboBoxTags_KeyUp(object sender, KeyEventArgs e)
        {
            GoIfEnterKey(sender, e);
        }

        private void comboBoxBranches_KeyUp(object sender, KeyEventArgs e)
        {
            GoIfEnterKey(sender, e);
        }

        private void GoIfEnterKey(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Go();
            }
        }

        private void SetCommitExpressionFromClipboard()
        {
            string text = Clipboard.GetText().Trim();
            if (text.IsNullOrEmpty())
            {
                return;
            }

            string guid = Module.RevParse(text);
            if (!string.IsNullOrEmpty(guid))
            {
                textboxCommitExpression.Text = text;
                textboxCommitExpression.SelectAll();
            }
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _tagsLoader.Dispose();
                _branchesLoader.Dispose();

                components?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
