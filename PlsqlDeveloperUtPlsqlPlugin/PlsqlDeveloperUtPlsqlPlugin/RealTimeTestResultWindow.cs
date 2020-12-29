﻿using FontAwesome.Sharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using utPLSQL;

namespace PlsqlDeveloperUtPlsqlPlugin
{
    public partial class RealTimeTestResultWindow : Form
    {
        private const int IconSize = 24;

        private readonly RealTimeTestRunner testRunner;
        private BindingList<TestResult> testResults = new BindingList<TestResult>();
        private int totalNumberOfTests;

        public RealTimeTestResultWindow(RealTimeTestRunner testRunner)
        {
            this.testRunner = testRunner;
            InitializeComponent();

            var bindingSource = new BindingSource();
            bindingSource.DataSource = testResults;
            gridResults.DataSource = bindingSource;

            gridResults.Columns[0].HeaderText = "";
            gridResults.Columns[0].MinimumWidth = 30;
            gridResults.Columns[1].MinimumWidth = 835;
            gridResults.Columns[2].MinimumWidth = 100;
            gridResults.Columns[2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }

        internal void RunTests(string type, string owner, string name, string subType)
        {
            ResetComponents();

            testResults.Clear();

            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }

            Show();

            new Thread(() =>
            {
                testRunner.RunTests(type, owner, name, subType);

            }).Start();
            new Thread(() =>
            {
                var completetedTests = 0;

                testRunner.ConsumeResult(@event =>
                {
                    gridResults.BeginInvoke((MethodInvoker)delegate ()
                    {
                        if (@event.type.Equals("pre-run"))
                        {
                            totalNumberOfTests = @event.totalNumberOfTests;

                            progressBar.Minimum = 0;
                            progressBar.Maximum = totalNumberOfTests;

                            CreateTestResults(@event);

                            gridResults.Rows[0].Cells[0].Selected = false;
                        }
                        else if (@event.type.Equals("post-test"))
                        {
                            completetedTests++;
                            txtTests.Text = (completetedTests > totalNumberOfTests ? totalNumberOfTests : completetedTests) + "/" + totalNumberOfTests;
                            progressBar.Value = completetedTests;

                            UpdateTestResult(@event);
                        }
                        else if (@event.type.Equals("post-run"))
                        {
                            txtStatus.Text = "Finished";

                            txtTests.Text = (completetedTests > totalNumberOfTests ? totalNumberOfTests : completetedTests) + "/" + totalNumberOfTests;
                            txtFailures.Text = @event.run.counter.failure + "";
                            txtErrors.Text = @event.run.counter.error + "";
                            txtWarning.Text = @event.run.counter.warning + "";
                            txtDisabled.Text = @event.run.counter.disabled + "";
                            txtTime.Text = @event.run.executionTime + " s";

                            if (@event.run.counter.failure > 0 || @event.run.counter.error > 0)
                            {
                                progressBar.ForeColor = Color.DarkRed;
                            }

                        }
                    });
                });
            }).Start();
        }

        private void ResetComponents()
        {
            txtTests.Text = "";
            txtFailures.Text = "";
            txtErrors.Text = "";
            txtDisabled.Text = "";
            txtStatus.Text = "";
            txtTime.Text = "";
            txtStatus.Text = "Running...";

            txtTestOwner.Text = "";
            txtTestPackage.Text = "";
            txtTestProcuedure.Text = "";
            txtTestName.Text = "";
            txtTestDescription.Text = "";

            txtTestStart.Text = "";
            txtTestEnd.Text = "";

            txtErrorMessage.Text = "";

            var bindingSource = new BindingSource();
            bindingSource.DataSource = new BindingList<Expectation>();
            gridTestFailures.DataSource = bindingSource;

            progressBar.ForeColor = Color.Green;
            progressBar.Minimum = 0;
            progressBar.Maximum = 100;
            progressBar.Value = 0;
        }

        private void UpdateTestResult(@event @event)
        {
            if (@event.test != null)
            {
                foreach (var testResult in testResults)
                {
                    if (testResult.Id.Equals(@event.test.id))
                    {
                        testResult.Start = @event.test.startTime;
                        testResult.End = @event.test.endTime;

                        testResult.Time = @event.test.executionTime;
                        
                        var counter = @event.test.counter;
                        if (counter.disabled > 0)
                        {
                            testResult.Icon = IconChar.Ban.ToBitmap(Color.Gray, IconSize);
                        }
                        else if (counter.success > 0)
                        {
                            testResult.Icon = IconChar.Check.ToBitmap(Color.Green, IconSize);
                        }
                        else if (counter.failure > 0)
                        {
                            testResult.Icon = IconChar.TimesCircle.ToBitmap(IconFont.Solid, IconSize, Color.Orange);
                        }
                        else if (counter.error > 0)
                        {
                            testResult.Icon = IconChar.ExclamationCircle.ToBitmap(Color.Red, IconSize);
                        }
                        else if (counter.warning > 0)
                        {
                            testResult.Icon = IconChar.ExclamationTriangle.ToBitmap(Color.Orange, IconSize);
                        }

                        if (@event.test.errorStack != null)
                        {
                            testResult.Error = @event.test.errorStack;
                        }
                        if (@event.test.failedExpectations != null)
                        {
                            foreach (var expectation in @event.test.failedExpectations)
                            {
                                testResult.failedExpectations.Add(new Expectation(expectation.message, expectation.caller));
                            }
                        }

                        gridResults.Refresh();
                    }
                }
            }
        }

        private void CreateTestResults(@event @event)
        {
            CreateTestResults(@event.items);
            CreateTestResults(@event.suite);
            CreateTestResults(@event.test);
        }

        private void CreateTestResults(items items)
        {
            if (items != null)
            {
                if (items.suite != null)
                {
                    foreach (var itemSuite in items.suite)
                    {
                        CreateTestResults(itemSuite);
                    }
                }
                if (items.test != null)
                {
                    foreach (var test in items.test)
                    {
                        CreateTestResults(test);
                    }
                }
            }
        }

        private void CreateTestResults(suite suite)
        {
            if (suite != null && suite.items != null)
            {
                CreateTestResults(suite.items);
            }
        }

        private void CreateTestResults(test test)
        {
            if (test != null)
            {
                testResults.Add(new TestResult
                {
                    Id = test.id,
                    Owner = test.ownerName,
                    Package = test.objectName,
                    Procedure = test.procedureName,
                    Name = test.name,
                    Description = test.description,
                    Icon = IconChar.None.ToBitmap(Color.Black, IconSize)
                });
            }
        }

        private void btnClose_Click(object sender, System.EventArgs e)
        {
            Hide();
        }

        private void TestResultWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void gridResults_SelectionChanged(object sender, EventArgs e)
        {
            if (gridResults.SelectedRows.Count > 0)
            {
                DataGridViewRow row = gridResults.SelectedRows[0];
                TestResult testResult = (TestResult)row.DataBoundItem;

                txtTestOwner.Text = testResult.Owner;
                txtTestPackage.Text = testResult.Package;
                txtTestProcuedure.Text = testResult.Procedure;
                txtTestName.Text = testResult.Name;
                txtTestDescription.Text = testResult.Description;

                txtTestStart.Text = testResult.Start == null ? "" : testResult.Start.ToString();
                txtTestEnd.Text = testResult.End == null ? "" : testResult.End.ToString();

                txtErrorMessage.Text = testResult.Error;

                var bindingSource = new BindingSource();
                bindingSource.DataSource = testResult.failedExpectations;
                gridTestFailures.DataSource = bindingSource;

                gridTestFailures.Columns[0].MinimumWidth = 480;
                gridTestFailures.Columns[1].MinimumWidth = 480;
            }
        }
    }
}

