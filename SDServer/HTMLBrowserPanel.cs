// HTMLBrowserPanel.cs
// Pete Myers
// CST 415, Fall 2017
//
// Implements a wrapper for a WebBrowser control
// Fires events for recognizing when a user clicks a Hyperlink or a Form button
// Extracts Form variable data and sends it in a dictionary of key/value pairs when users clicks a button in a post method form
//
// This code may be freely used and modified by students and included in their assignment
//

using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SDBrowser
{
    public class HTMLBrowserPanel : Panel
    {
        private string contents;
        private WebBrowser htmlControl;
        private System.ComponentModel.IContainer components = null;
        
        public HTMLBrowserPanel()
        {
            this.htmlControl = null;
            InitializeComponent();
        }

        public class FormClickEventArgs : EventArgs
        {
            public string Method { get; set; }
            public string Target { get; set; }
            public Dictionary<string,string> FormVariables { get; set; }
            public string FormVariablesString
            {
                get
                {
                    string variableString = "";
                    if (FormVariables != null)
                    {
                        foreach (KeyValuePair<string, string> kvp in FormVariables)
                        {
                            variableString += kvp.Key + "=" + kvp.Value + "\r\n";
                        }
                    }
                    return variableString;
                }
            }
        }

        public event EventHandler<string> LinkClicked;
        public event EventHandler<FormClickEventArgs> FormClicked;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
        }

        public void SetContents(string contents)
        {
            this.contents = contents;
            ShowHtmlContent();
        }

        private void ShowHtmlContent()
        {
            if (htmlControl != null)
            {
                this.htmlControl.Hide();
                this.Controls.Remove(this.htmlControl);
                this.htmlControl.Dispose();
            }
            this.SuspendLayout();
            this.htmlControl = new WebBrowser();
            this.htmlControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.htmlControl.Location = new System.Drawing.Point(0, 0);
            this.htmlControl.MinimumSize = new System.Drawing.Size(20, 20);
            this.htmlControl.Name = "htmlControl";
            this.htmlControl.TabIndex = 0;
            this.htmlControl.DocumentCompleted += new System.Windows.Forms.WebBrowserDocumentCompletedEventHandler(this.htmlControl_DocumentCompleted);
            this.htmlControl.AllowNavigation = false;
            this.htmlControl.ScrollBarsEnabled = true;
            this.htmlControl.DocumentText = contents;
            this.htmlControl.Refresh();
            this.Controls.Add(this.htmlControl);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void htmlControl_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            // add event handlers for active elements in the document...

            // add event handler for all form buttons
            foreach (HtmlElement formElement in htmlControl.Document.Forms)
            {
                foreach (HtmlElement controlElement in formElement.All)
                {
                    if ((controlElement.TagName.ToLower() == "input"
                            && (controlElement.GetAttribute("type").ToLower() == "submit"
                                || controlElement.GetAttribute("type").ToLower() == "button"))
                        || (controlElement.TagName.ToLower() == "button"))
                    {
                        controlElement.Click += ControlElement_Click;
                    }
                }
            }

            // add event handler for all links
            foreach (HtmlElement linkElement in htmlControl.Document.Links)
            {
                linkElement.Click += LinkElement_Click;
            }
        }

        private void ControlElement_Click(object sender, HtmlElementEventArgs e)
        {
            // get the current values
            Dictionary<string, string> values = ExtractFormValues(htmlControl.Document);

            // add the clicked control to the values
            HtmlElement controlElement = (sender as HtmlElement);
            values[controlElement.Name] = controlElement.GetAttribute("value");

            // find the form for the clicked control
            HtmlElement formElement = controlElement.Parent;
            while (formElement != null && formElement.TagName.ToLower() != "form")
                formElement = formElement.Parent;

            // extract method and target from form
            string method = formElement.GetAttribute("method");
            string target = formElement.GetAttribute("action");

            // fire event
            FormClickEventArgs fcea = new FormClickEventArgs();
            fcea.Method = method;
            fcea.Target = target;
            fcea.FormVariables = values;
            FormClicked.Invoke(this, fcea);

            /*
            // create a value string
            string valuesString = "";
            foreach (KeyValuePair<string, string> kvp in values)
            {
                valuesString += kvp.Key + "=" + kvp.Value + "\r\n";
            }

            string msg = controlElement.Name + " clicked!";
            if (formElement != null)
            {
                msg += "\r\n";
                msg += method;
                msg += " to ";
                msg += target;
                msg += "\r\n";
                msg += valuesString;
            }

            MessageBox.Show(msg);
            */
        }

        private void LinkElement_Click(object sender, HtmlElementEventArgs e)
        {
            try
            {
                // get the target of the hyperlink, verify it's SD and the same server we're already talking to!
                HtmlElement linkElement = (sender as HtmlElement);
                string target = linkElement.GetAttribute("href");
                LinkClicked.Invoke(this, target);

                //MessageBox.Show("Clicked a link, Target=" + target);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed! " + ex.Message);
            }
        }

        private Dictionary<string, string> ExtractFormValues(HtmlDocument doc)
        {
            Dictionary<string, string> values = new Dictionary<string, string>();

            HtmlElementCollection forms = doc.Forms;
            foreach (HtmlElement formElement in forms)
            {
                foreach (HtmlElement controlElement in formElement.All)
                {
                    if (controlElement.TagName.ToLower() == "input")
                    {
                        string controlType = controlElement.GetAttribute("type");
                        if (controlType != null)
                        {
                            switch (controlType.ToLower())
                            {
                                case "text":
                                    values[controlElement.Name] = controlElement.GetAttribute("value");
                                    break;

                                case "checkbox":
                                case "radio":
                                    if (controlElement.GetAttribute("checked") != null && controlElement.GetAttribute("checked").ToLower() == "true")
                                    {
                                        if (values.ContainsKey(controlElement.Name))
                                            values[controlElement.Name] += ";";
                                        else
                                            values[controlElement.Name] = "";
                                        values[controlElement.Name] += controlElement.GetAttribute("value");
                                    }
                                    break;
                            }
                        }
                    }
                    else if (controlElement.TagName.ToLower() == "textarea")
                    {
                        if (controlElement.InnerText != null)
                            values[controlElement.Name] = controlElement.InnerText;
                    }
                }
            }

            return values;
        }
    }
}
