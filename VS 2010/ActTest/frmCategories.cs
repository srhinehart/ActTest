using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TreeLog;

namespace LogViewer
{
    public partial class frmCategories : Form
    {
        public frmCategories()
        {
            InitializeComponent();
        }

        private LogReader myReader = null;
        public LogReader Reader
        {
            get { return myReader; }
            set { myReader = value; InitCategoryList(); }
        }

        private void InitCategoryList()
        {
            if (myReader != null)
            {
                clbCategories.Items.Clear();
                if (myReader.Categories != null)
                {
                    clbCategories.Items.AddRange(myReader.Categories.ToArray());
                    //foreach (int index in myReader.VisibleCategories)
                    //{
                    //    clbCategories.SetItemChecked(index, true);
                    //}
                    int index = 0;
                    while (index < myReader.Categories.Count)
                    {
                        clbCategories.SetItemChecked(index, myReader.CategoryIsVisible[index]);
                        index++;
                    }
                }
            }
        }

        private void frmCategories_Shown(object sender, EventArgs e)
        {
            if (myReader == null || myReader.Categories.Count == 0)
                MessageBox.Show("Must load log file, to define categories.", "No Categories Defined");
        }

        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            int index = 0;
            while (index < clbCategories.Items.Count)
            {
                clbCategories.SetItemChecked(index++, true);
            }
        }

        private void btnSelectNone_Click(object sender, EventArgs e)
        {
            int index = 0;
            while (index < clbCategories.Items.Count)
            {
                clbCategories.SetItemChecked(index++, false);
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (myReader != null && myReader.CategoryIsVisible != null)
            {
                int index = 0;
                while (index < clbCategories.Items.Count)
                {
                    myReader.CategoryIsVisible[index] = clbCategories.GetItemChecked(index);
                    index++;
                }
                this.DialogResult = System.Windows.Forms.DialogResult.OK;
            }
            this.Close();
        }
    }
}
